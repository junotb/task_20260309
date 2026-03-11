using FluentValidation;
using task_20260309.Application.CQRS.Commands;
using task_20260309.Application.CQRS.Queries;
using task_20260309.Application.Parsers;
using task_20260309.Application.Services;

namespace task_20260309.Api;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee");

        group.MapGet("/", GetEmployeeList)
            .WithName("GetEmployeeList")
            .Produces<GetEmployeeListResponse>(200, "application/json");

        group.MapGet("/{name}", GetEmployeeByName)
            .WithName("GetEmployeeByName")
            .Produces<EmployeeDetailResponse>(200, "application/json")
            .Produces(404);

        group.MapPost("/", AddEmployees)
            .WithName("AddEmployees")
            .Produces(201)
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
        var query = new GetEmployeeListQuery(request.Page, request.PageSize);
        var result = await handler.HandleAsync(query, ct);
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
        var query = new GetEmployeeByNameQuery(name);
        var result = await handler.HandleAsync(query, ct);
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
        IValidator<AddEmployeesCommand> validator,
        ILogger<EmployeeApiLogger> logger,
        CancellationToken ct)
    {
        AddEmployeesCommand command;
        var contentType = context.Request.ContentType ?? "";
        logger.LogInformation("직원 추가 요청 수신, ContentType={ContentType}", contentType);

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            var csvText = form["csvText"].FirstOrDefault();

            if (file is not null)
            {
                var fileName = file.FileName;
                var fileSize = file.Length;
                logger.LogInformation(
                    "파일 업로드 수신, FileName={FileName}, FileSize={FileSize}",
                    fileName, fileSize);

                await using var stream = file.OpenReadStream();
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                try
                {
                    command = ext == ".json"
                        ? await EmployeeImportParser.ParseJsonAsync(stream, ct)
                        : await EmployeeImportParser.ParseCsvAsync(stream, ct);
                    logger.LogInformation(
                        "파일 파싱 성공, FileName={FileName}, EmployeeCount={EmployeeCount}",
                        fileName, command.Employees.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "파일 파싱 실패, FileName={FileName}, FileSize={FileSize}",
                        fileName, fileSize);
                    throw;
                }
            }
            else if (!string.IsNullOrWhiteSpace(csvText))
            {
                try
                {
                    command = EmployeeImportParser.ParseCsvFromString(csvText);
                    logger.LogInformation("csvText 파싱 성공, EmployeeCount={EmployeeCount}", command.Employees.Count);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "csvText 파싱 실패");
                    throw;
                }
            }
            else
            {
                return Results.BadRequest(new { error = "file 또는 csvText가 필요합니다." });
            }
        }
        else if (contentType.Contains("application/json"))
        {
            await using var stream = context.Request.Body;
            try
            {
                command = await EmployeeImportParser.ParseJsonAsync(stream, ct);
                logger.LogInformation("JSON 바디 파싱 성공, EmployeeCount={EmployeeCount}", command.Employees.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JSON 바디 파싱 실패");
                throw;
            }
        }
        else if (contentType.Contains("text/csv"))
        {
            await using var stream = context.Request.Body;
            try
            {
                command = await EmployeeImportParser.ParseCsvAsync(stream, ct);
                logger.LogInformation("CSV 바디 파싱 성공, EmployeeCount={EmployeeCount}", command.Employees.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CSV 바디 파싱 실패");
                throw;
            }
        }
        else
        {
            return Results.BadRequest(new { error = "Content-Type: application/json, text/csv 또는 multipart/form-data (file, csvText) 필요" });
        }

        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            logger.LogWarning("직원 추가 유효성 검사 실패, ErrorCount={ErrorCount}, Errors={Errors}",
                validationResult.Errors.Count, string.Join("; ", validationResult.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return Results.BadRequest(new { errors });
        }

        await channel.WriteAsync(command, ct);
        logger.LogInformation("직원 Import 채널에 전달 완료, EmployeeCount={EmployeeCount}", command.Employees.Count);
        return Results.Created("/api/employee", (object?)null);
    }
}

public record GetEmployeeListRequest(int Page = 1, int PageSize = 10);

public record GetEmployeeListResponse(
    IReadOnlyList<EmployeeListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record EmployeeListItemResponse(int Id, string Name, string Email, string Tel, DateTime Joined);

public record EmployeeDetailResponse(int Id, string Name, string Email, string Tel, DateTime Joined);

/// <summary>
/// Employee API 엔드포인트 로깅 카테고리용 (ILogger&lt;T&gt; 타입 인자)
/// </summary>
internal sealed class EmployeeApiLogger { }
