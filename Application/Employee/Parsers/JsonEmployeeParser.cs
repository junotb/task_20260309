using System.Text.Json;
using Microsoft.Extensions.Logging;
using task_20260309.Application.Common;
using task_20260309.Application.Employee.Commands;
using task_20260309.Infrastructure.Common;

namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// JSON 배열 형식 직원 파서.
/// </summary>
public class JsonEmployeeParser : IEmployeeParser
{
    private readonly ILogger<JsonEmployeeParser> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonEmployeeParser(ILogger<JsonEmployeeParser> logger)
    {
        _logger = logger;
    }

    public bool CanParseFile(string? fileName)
    {
        return FileExtensionHelper.GetNormalizedExtension(fileName) == "json";
    }

    public bool CanParseContent(string? content)
    {
        var trimmed = content?.TrimStart();
        if (string.IsNullOrEmpty(trimmed)) return false;
        return trimmed[0] == '[';
    }

    public async Task<AddEmployeesCommand> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var text = await EncodingHelper.ReadStreamWithEncodingAsync(stream, ct);
        return await ParseFromStringAsync(text, ct);
    }

    public Task<AddEmployeesCommand> ParseFromStringAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new AddEmployeesCommand([]));
        return ParseJsonAsync(text, ct);
    }

    private async Task<AddEmployeesCommand> ParseJsonAsync(string text, CancellationToken ct)
    {
        var records = new List<EmployeeImportDto>();
        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            _logger.LogError("JSON 형식 오류: 배열이 아닙니다");
            return new AddEmployeesCommand(records);
        }
        var row = 0;
        foreach (var el in root.EnumerateArray())
        {
            row++;
            try
            {
                var r = JsonSerializer.Deserialize<EmployeeImportJsonRecord>(el.GetRawText(), JsonOptions);
                if (r is null || (string.IsNullOrWhiteSpace(r.Name) && string.IsNullOrWhiteSpace(r.Email)))
                    continue;
                var joined = r.Joined.HasValue ? r.Joined.Value : DateTime.UtcNow.Date;
                records.Add(new EmployeeImportDto(
                    r.Name?.Trim() ?? "",
                    r.Email?.Trim() ?? "",
                    r.Tel?.Trim() ?? "",
                    joined));
            }
            catch (Exception ex)
            {
                _logger.LogError("N번째 줄 파싱 실패: {Reason}, Row={Row}", ex.Message, row);
            }
        }
        await Task.CompletedTask;
        return new AddEmployeesCommand(records);
    }

    private sealed class EmployeeImportJsonRecord
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public DateTime? Joined { get; set; }
    }
}
