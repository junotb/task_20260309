using FluentValidation;
using task_20260309.Application.Employee;
using task_20260309.Application.Employee.Parsers;
using task_20260309.Application.Employee.Services;

namespace task_20260309.Application.Employee.Commands;

/// <summary>
/// AddEmployees Command Handler. 파싱, 검증, 채널 전달까지 오케스트레이션.
/// </summary>
public class AddEmployeesCommandHandler
{
    private readonly IEmployeeParserResolver _parserResolver;
    private readonly IValidator<AddEmployeesCommand> _commandValidator;
    private readonly EmployeeImportChannel _channel;
    private readonly ILogger<AddEmployeesCommandHandler> _logger;

    public AddEmployeesCommandHandler(
        IEmployeeParserResolver parserResolver,
        IValidator<AddEmployeesCommand> commandValidator,
        EmployeeImportChannel channel,
        ILogger<AddEmployeesCommandHandler> logger)
    {
        _parserResolver = parserResolver;
        _commandValidator = commandValidator;
        _channel = channel;
        _logger = logger;
    }

    /// <summary>
    /// AddEmployees 흐름 실행. 입력이 비었으면 Empty, 파싱 실패 시 ParseFailed, 검증 실패 시 ValidationFailed, 성공 시 Success 반환.
    /// </summary>
    public async Task<AddEmployeesResponse> HandleAsync(AddEmployeesRequest request, CancellationToken ct = default)
    {
        if (IsEmpty(request))
        {
            _logger.LogWarning("수신된 데이터가 비어있음, SourceType=None");
            return new AddEmployeesEmptyResponse(
                "수신된 데이터가 비어있습니다.",
                "파일(file)을 업로드하거나 rawData에 CSV/JSON 텍스트를 입력해 주세요.",
                0, 0);
        }

        List<EmployeeImportDto> mergedEmployees;
        int totalParsedCount;
        List<ImportValidationError> mergeDuplicateErrors;

        try
        {
            (mergedEmployees, totalParsedCount, mergeDuplicateErrors) = await ParseAndMergeAsync(request, ct);
        }
        catch (ParseException ex)
        {
            _logger.LogError(ex.InnerException, "파싱 실패, SourceType={SourceType}, FileName={FileName}", ex.SourceType, ex.FileName ?? "-");
            return new AddEmployeesParseFailedResponse(ex.Message);
        }

        if (mergedEmployees.Count == 0)
        {
            var hasAnyInput = (request.FileStream is not null, !string.IsNullOrWhiteSpace(request.RawData)) switch
            {
                (true, true) => "File+Textarea",
                (true, false) => "File",
                _ => "Textarea"
            };
            _logger.LogWarning("수신된 데이터에서 유효한 직원을 찾지 못함, SourceType={SourceType}", hasAnyInput);
            return new AddEmployeesEmptyResponse(
                "수신된 데이터에서 유효한 직원 정보를 찾지 못했습니다.",
                "",
                (request.FileStream is not null || !string.IsNullOrWhiteSpace(request.RawData)) ? 1 : 0,
                0);
        }

        var command = new AddEmployeesCommand(mergedEmployees);
        var validationResult = await _commandValidator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            var validationErrors = EmployeeImportHelpers.MapValidationResultToErrors(validationResult, mergedEmployees);
            var allErrors = mergeDuplicateErrors.Concat(validationErrors).ToList();
            _logger.LogWarning("유효한 직원이 없음, TotalCount={TotalCount}, ErrorCount={ErrorCount}",
                mergedEmployees.Count + mergeDuplicateErrors.Count, allErrors.Count);
            return new AddEmployeesValidationFailedResponse(
                "유효성 검사를 통과한 직원이 없습니다.",
                totalParsedCount,
                allErrors);
        }

        await _channel.WriteAsync(command, ct);
        var skippedCount = totalParsedCount - mergedEmployees.Count;
        _logger.LogInformation("직원 Import 채널에 전달 완료, ImportedCount={ImportedCount}, SkippedCount={SkippedCount}",
            mergedEmployees.Count, skippedCount);

        var allResponseErrors = mergeDuplicateErrors.Count > 0 ? mergeDuplicateErrors : null;
        return new AddEmployeesSuccessResponse(mergedEmployees.Count, skippedCount, totalParsedCount, allResponseErrors);
    }

    private static bool IsEmpty(AddEmployeesRequest request)
    {
        var hasFile = request.FileStream is not null && request.FileStream.Length > 0;
        var hasRaw = !string.IsNullOrWhiteSpace(request.RawData);
        return !hasFile && !hasRaw;
    }

    private async Task<(List<EmployeeImportDto> Merged, int TotalParsedCount, List<ImportValidationError> MergeDuplicateErrors)> ParseAndMergeAsync(
        AddEmployeesRequest request,
        CancellationToken ct)
    {
        var fromFile = new List<EmployeeImportDto>();
        var fromRaw = new List<EmployeeImportDto>();
        var totalParsed = 0;

        try
        {
            if (request.FileStream is not null && request.FileStream.Length > 0)
            {
                var parser = _parserResolver.ResolveForFile(request.FileName);
                if (parser is null)
                {
                    throw new ParseException(
                        $"지원하지 않는 파일 형식입니다. CSV 또는 JSON을 사용해 주세요. (확장자: {Path.GetExtension(request.FileName ?? "")})",
                        new InvalidOperationException("Unsupported format"), "File", request.FileName);
                }
                try
                {
                    var parsed = await parser.ParseAsync(request.FileStream, ct);
                    fromFile.AddRange(parsed.Employees);
                    totalParsed += parsed.Employees.Count;
                    _logger.LogInformation("직원 추가 요청 파싱 완료, SourceType={SourceType}, ParsedCount={ParsedCount}", "File", parsed.Employees.Count);
                }
                catch (Exception ex)
                {
                    throw new ParseException("파일 형식이 올바르지 않습니다. CSV 또는 JSON을 확인해 주세요.", ex, "File", request.FileName);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.RawData))
            {
                var parser = _parserResolver.ResolveForContent(request.RawData);
                if (parser is null)
                {
                    throw new ParseException("rawData 형식을 인식할 수 없습니다. CSV 또는 JSON 배열을 사용해 주세요.", new InvalidOperationException("Unsupported format"), "Textarea", null);
                }
                var parsed = await parser.ParseFromStringAsync(request.RawData!, ct);
                fromRaw.AddRange(parsed.Employees);
                totalParsed += parsed.Employees.Count;
                _logger.LogInformation("직원 추가 요청 파싱 완료, SourceType={SourceType}, ParsedCount={ParsedCount}, MergedTotal={MergedTotal}",
                    "Textarea", parsed.Employees.Count, fromFile.Count + fromRaw.Count);
            }

            var (merged, duplicateErrors) = EmployeeImportHelpers.MergeByEmail(fromFile.Concat(fromRaw));
            return (merged, totalParsed, duplicateErrors);
        }
        finally
        {
            if (request.FileStream is not null)
                await request.FileStream.DisposeAsync();
        }
    }
}
