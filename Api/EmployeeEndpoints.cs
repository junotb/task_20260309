using FluentValidation;
using FluentValidation.Results;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Queries;
using task_20260309.Application.Employee.Parsers;
using task_20260309.Application.Employee.Services;

namespace task_20260309.Api;

internal static class EmployeeImportHelpers
{
    /// <summary>
    /// 여러 소스의 직원 목록을 이메일 기준으로 병합합니다.
    /// 먼저 나온 항목을 우선하고, 이후 중복은 ImportValidationError로 반환합니다.
    /// </summary>
    public static (List<EmployeeImportDto> Merged, List<ImportValidationError> DuplicateErrors) MergeByEmail(
        IEnumerable<EmployeeImportDto> sources)
    {
        var merged = new List<EmployeeImportDto>();
        var duplicateErrors = new List<ImportValidationError>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var emp in sources)
        {
            index++;
            var key = emp.Email.Trim().ToLowerInvariant();
            if (!seen.Add(key))
            {
                duplicateErrors.Add(new ImportValidationError(
                    index,
                    emp.Email,
                    ["같은 이메일이 이미 포함되어 있습니다."]));
                continue;
            }
            merged.Add(emp);
        }
        return (merged, duplicateErrors);
    }

    /// <summary>
    /// FluentValidation ValidationResult를 ImportValidationError 목록으로 변환합니다.
    /// </summary>
    public static List<ImportValidationError> MapValidationResultToErrors(
        ValidationResult result,
        IReadOnlyList<EmployeeImportDto> employees)
    {
        if (result.IsValid) return [];

        var byIndex = new Dictionary<int, List<string>>();
        foreach (var failure in result.Errors)
        {
            var idx = ExtractIndexFromPropertyName(failure.PropertyName);
            if (idx >= 0 && idx < employees.Count)
            {
                if (!byIndex.TryGetValue(idx, out var list))
                {
                    list = [];
                    byIndex[idx] = list;
                }
                list.Add(failure.ErrorMessage);
            }
        }

        return byIndex
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var emp = employees[kv.Key];
                return new ImportValidationError(kv.Key + 1, emp.Email, kv.Value);
            })
            .ToList();
    }

    private static int ExtractIndexFromPropertyName(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return -1;
        var start = propertyName.IndexOf('[');
        var end = propertyName.IndexOf(']', start + 1);
        if (start < 0 || end <= start) return -1;
        return int.TryParse(propertyName.AsSpan(start + 1, end - start - 1), out var idx) ? idx : -1;
    }
}

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee");

        group.MapGet("/", GetEmployeeList)
            .WithName("GetEmployeeList")
            .WithSummary("직원 목록 조회")
            .WithDescription("""
                페이지네이션된 직원 목록. Page, PageSize로 조회.
                성공 200: items, totalCount, page, pageSize 반환. Page/PageSize는 1~100 자동 보정.
                """)
            .Produces<GetEmployeeListResponse>(200, "application/json");

        group.MapGet("/{name}", GetEmployeeByName)
            .WithName("GetEmployeeByName")
            .WithSummary("이름으로 직원 조회")
            .WithDescription("""
                이름이 정확히 일치하는 직원 1건.
                성공 200: 상세 정보 반환.
                실패 404: 해당 이름의 직원 없음.
                """)
            .Produces<EmployeeDetailResponse>(200, "application/json")
            .Produces(404);

        group.MapPost("/", AddEmployees)
            .WithName("AddEmployees")
            .WithSummary("직원 일괄 추가")
            .WithDescription("""
                multipart/form-data: file(CSV/JSON) 또는 rawData. 둘 다 시 병합·이메일 중복 제거.
                성공 201: 채널 전달 완료. message, imported, skipped, total, errors(있을 때). 실제 DB 저장은 비동기.
                성공 200: file·rawData 둘 다 비었거나, 파싱 결과 유효 직원 0명. received=0 또는 imported=0.
                실패 400: Content-Type 오류 / 파싱 실패(형식 오류) / 검증 실패(전체). errors에 index, email, errors 배열.
                """)
            .Produces(201)
            .Produces(200)
            .Produces(400)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> GetEmployeeList(
        [AsParameters] GetEmployeeListRequest request,
        GetEmployeeListQueryHandler handler,
        ILogger<EmployeeApiLogger> logger,
        CancellationToken ct)
    {
        logger.LogInformation("직원 목록 조회 요청, Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);
        var result = await handler.HandleAsync(new GetEmployeeListQuery(request.Page, request.PageSize), ct);
        logger.LogInformation(
            "직원 목록 조회 완료, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
            result.TotalCount, result.Items.Count);
        var response = new GetEmployeeListResponse(
            result.Items.Select(e => new EmployeeListItemResponse(e.Id, e.Name, e.Email, e.Tel, e.Joined)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetEmployeeByName(
        string name,
        GetEmployeeByNameQueryHandler handler,
        ILogger<EmployeeApiLogger> logger,
        CancellationToken ct)
    {
        logger.LogInformation("직원 이름 조회 요청, Name={Name}", name);
        var result = await handler.HandleAsync(new GetEmployeeByNameQuery(name), ct);
        if (result.Employee is null)
        {
            logger.LogWarning("직원을 찾을 수 없음, Name={Name}", name);
            return Results.NotFound();
        }
        logger.LogInformation("직원 조회 완료, Id={EmployeeId}, Name={Name}", result.Employee.Id, name);
        var dto = result.Employee;
        return Results.Ok(new EmployeeDetailResponse(dto.Id, dto.Name, dto.Email, dto.Tel, dto.Joined));
    }

    private static async Task<IResult> AddEmployees(
        HttpContext context,
        EmployeeImportChannel channel,
        IValidator<AddEmployeesCommand> commandValidator,
        IEmployeeParser parser,
        ILogger<EmployeeApiLogger> logger,
        CancellationToken ct)
    {
        if (!context.Request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Content-Type: multipart/form-data가 필요합니다. file 또는 rawData를 전달해 주세요." });
        }

        var (file, rawData) = await ReadImportFormAsync(context, ct);

        if ((file is null || file.Length == 0) && string.IsNullOrWhiteSpace(rawData))
        {
            logger.LogWarning("수신된 데이터가 비어있음, SourceType=None");
            return Results.Ok(new ImportEmptyResponse(
                "수신된 데이터가 비어있습니다.",
                "파일(file)을 업로드하거나 rawData에 CSV/JSON 텍스트를 입력해 주세요.",
                0, 0));
        }

        List<EmployeeImportDto> mergedEmployees;
        int totalParsedCount;
        List<ImportValidationError> mergeDuplicateErrors;

        try
        {
            (mergedEmployees, totalParsedCount, mergeDuplicateErrors) = await ParseAndMergeAsync(parser, file, rawData, logger, ct);
        }
        catch (ParseException ex)
        {
            logger.LogError(ex.InnerException, "파싱 실패, SourceType={SourceType}, FileName={FileName}", ex.SourceType, ex.FileName ?? "-");
            return Results.BadRequest(new { error = ex.Message });
        }

        if (mergedEmployees.Count == 0)
        {
            var sourceType = (file is not null && file.Length > 0, !string.IsNullOrWhiteSpace(rawData)) switch
            {
                (true, true) => "File+Textarea",
                (true, false) => "File",
                _ => "Textarea"
            };
            logger.LogWarning("수신된 데이터에서 유효한 직원을 찾지 못함, SourceType={SourceType}", sourceType);
            return Results.Ok(new ImportEmptyResponse(
                "수신된 데이터에서 유효한 직원 정보를 찾지 못했습니다.",
                "",
                (file is not null && file.Length > 0) || !string.IsNullOrWhiteSpace(rawData) ? 1 : 0,
                0));
        }

        var command = new AddEmployeesCommand(mergedEmployees);
        var validationResult = await commandValidator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            var validationErrors = EmployeeImportHelpers.MapValidationResultToErrors(validationResult, mergedEmployees);
            var allErrors = mergeDuplicateErrors.Concat(validationErrors).ToList();
            logger.LogWarning("유효한 직원이 없음, TotalCount={TotalCount}, ErrorCount={ErrorCount}",
                mergedEmployees.Count + mergeDuplicateErrors.Count, allErrors.Count);
            return Results.BadRequest(new
            {
                message = "유효성 검사를 통과한 직원이 없습니다.",
                total = totalParsedCount,
                imported = 0,
                errors = allErrors
            });
        }

        await channel.WriteAsync(command, ct);
        var skippedCount = totalParsedCount - mergedEmployees.Count;
        logger.LogInformation("직원 Import 채널에 전달 완료, ImportedCount={ImportedCount}, SkippedCount={SkippedCount}",
            mergedEmployees.Count, skippedCount);

        var allResponseErrors = mergeDuplicateErrors.Count > 0 ? mergeDuplicateErrors : null;
        return Results.Created("/api/employee", BuildImportSuccessResponse(
            mergedEmployees.Count, skippedCount, totalParsedCount, allResponseErrors));
    }

    private static async Task<(IFormFile? file, string? rawData)> ReadImportFormAsync(HttpContext context, CancellationToken ct)
    {
        var form = await context.Request.ReadFormAsync(ct);
        var file = form.Files["file"];
        var rawData = form["rawData"].FirstOrDefault();
        return (file, rawData);
    }

    private static async Task<(List<EmployeeImportDto> Merged, int TotalParsedCount, List<ImportValidationError> MergeDuplicateErrors)> ParseAndMergeAsync(
        IEmployeeParser parser,
        IFormFile? file,
        string? rawData,
        ILogger logger,
        CancellationToken ct)
    {
        var fromFile = new List<EmployeeImportDto>();
        var fromRaw = new List<EmployeeImportDto>();
        var totalParsed = 0;

        if (file is not null && file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            try
            {
                var parsed = await parser.ParseAsync(stream, ct);
                fromFile.AddRange(parsed.Employees);
                totalParsed += parsed.Employees.Count;
                logger.LogInformation("직원 추가 요청 파싱 완료, SourceType={SourceType}, ParsedCount={ParsedCount}", "File", parsed.Employees.Count);
            }
            catch (Exception ex)
            {
                throw new ParseException("파일 형식이 올바르지 않습니다. CSV 또는 JSON을 확인해 주세요.", ex, "File", file.FileName);
            }
        }

        if (!string.IsNullOrWhiteSpace(rawData))
        {
            try
            {
                var parsed = await parser.ParseFromStringAsync(rawData!, ct);
                fromRaw.AddRange(parsed.Employees);
                totalParsed += parsed.Employees.Count;
                logger.LogInformation("직원 추가 요청 파싱 완료, SourceType={SourceType}, ParsedCount={ParsedCount}, MergedTotal={MergedTotal}",
                    "Textarea", parsed.Employees.Count, fromFile.Count + fromRaw.Count);
            }
            catch (Exception ex)
            {
                throw new ParseException("rawData 형식이 올바르지 않습니다. CSV 또는 JSON을 확인해 주세요.", ex, "Textarea", null);
            }
        }

        var (merged, duplicateErrors) = EmployeeImportHelpers.MergeByEmail(fromFile.Concat(fromRaw));
        return (merged, totalParsed, duplicateErrors);
    }

    private static ImportSuccessResponse BuildImportSuccessResponse(
        int imported, int skipped, int total, IReadOnlyList<ImportValidationError>? errors)
    {
        return new ImportSuccessResponse(
            "직원 등록이 완료되었습니다.",
            imported,
            skipped,
            total,
            errors);
    }
}

/// <summary>
/// 직원 목록 조회 쿼리. Page &lt; 1이면 1, PageSize는 1~100 클램프.
/// </summary>
/// <param name="Page">페이지 번호. 기본 1. 예: 1</param>
/// <param name="PageSize">페이지당 건수. 기본 10. 예: 10</param>
public record GetEmployeeListRequest(int Page = 1, int PageSize = 10);

/// <summary>
/// 직원 목록 응답. Id 오름차순.
/// </summary>
public record GetEmployeeListResponse(
    IReadOnlyList<EmployeeListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// 목록 1건. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeListItemResponse(int Id, string Name, string Email, string Tel, DateTime Joined);

/// <summary>
/// 상세 1건. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeDetailResponse(int Id, string Name, string Email, string Tel, DateTime Joined);

/// <summary>
/// Import 성공. imported=실제 등록 대상, skipped=중복·검증 실패로 제외된 건수.
/// </summary>
public record ImportSuccessResponse(
    string Message,
    int Imported,
    int Skipped,
    int Total,
    IReadOnlyList<ImportValidationError>? Errors);

/// <summary>
/// 빈 입력 또는 파싱 후 유효 직원 0건. hint는 클라이언트 안내용.
/// </summary>
public record ImportEmptyResponse(string Message, string Hint, int Received, int Imported);

/// <summary>
/// 검증 오류 1건. index=1-based 행 번호, errors=오류 메시지 배열. 예: index=2, email=dup@x.com, errors=["이미 등록된 이메일입니다."]
/// </summary>
public record ImportValidationError(int Index, string Email, IReadOnlyList<string> Errors);

/// <summary>
/// 파싱 실패. SourceType, FileName으로 원인 추적.
/// </summary>
internal sealed class ParseException(string message, Exception inner, string sourceType, string? fileName) : Exception(message, inner)
{
    public string SourceType { get; } = sourceType;
    public string? FileName { get; } = fileName;
}

internal sealed class EmployeeApiLogger { }
