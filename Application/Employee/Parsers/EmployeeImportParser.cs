using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using task_20260309.Application.Employee.Commands;

namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 형식 자동 감지, CSV 헤더 복구, UTF-8/EUC-KR 인코딩 대응 직원 파서.
/// </summary>
public class EmployeeImportParser : IEmployeeParser
{
    private readonly ILogger<EmployeeImportParser> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EmployeeImportParser(ILogger<EmployeeImportParser> logger)
    {
        _logger = logger;
    }

    public async Task<AddEmployeesCommand> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var text = await ReadWithEncodingAsync(stream, ct);
        return await ParseFromStringAsync(text, ct);
    }

    public Task<AddEmployeesCommand> ParseFromStringAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new AddEmployeesCommand([]));

        var firstChar = text.TrimStart()[0];
        return firstChar == '['
            ? ParseJsonAsync(text, ct)
            : Task.FromResult(ParseCsvAsync(text, ct));
    }

    private static async Task<string> ReadWithEncodingAsync(Stream stream, CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.Length == 0) return string.Empty;

        // UTF-8 시도 후 한글 깨짐(�) 있으면 EUC-KR로 재시도
        var utf8 = Encoding.UTF8.GetString(bytes);
        if (!utf8.Contains('\uFFFD'))
            return utf8;

        try
        {
            var euckr = EncodingHelper.GetEucKr();
            return euckr.GetString(bytes);
        }
        catch
        {
            return utf8;
        }
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
        return await Task.FromResult(new AddEmployeesCommand(records));
    }

    private AddEmployeesCommand ParseCsvAsync(string text, CancellationToken ct)
    {
        using var reader = new StringReader(text);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? ""
        };

        var firstLine = text.Split(['\n', '\r'])[0].Trim();
        var hasHeader = LooksLikeHeader(firstLine);
        config.HasHeaderRecord = hasHeader;

        using var csv = new CsvReader(reader, config);
        string[] headers;
        if (hasHeader)
        {
            if (!csv.Read())
                return new AddEmployeesCommand([]);
            csv.ReadHeader();
            headers = csv.HeaderRecord ?? [];
        }
        else
        {
            headers = ["name", "email", "tel", "joined"];
        }

        var records = new List<EmployeeImportDto>();
        var row = hasHeader ? 1 : 0;
        while (csv.Read())
        {
            row++;
            try
            {
                var name = GetCsvField(csv, headers, "name", hasHeader);
                var email = GetCsvField(csv, headers, "email", hasHeader);
                var tel = GetCsvField(csv, headers, "tel", hasHeader);
                var joinedStr = GetCsvField(csv, headers, "joined", hasHeader);

                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
                    continue;

                if (!DateTime.TryParse(joinedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var joined))
                    joined = DateTime.UtcNow.Date;

                records.Add(new EmployeeImportDto(name?.Trim() ?? "", email?.Trim() ?? "", tel?.Trim() ?? "", joined));
            }
            catch (Exception ex)
            {
                _logger.LogError("N번째 줄 파싱 실패: {Reason}, Row={Row}", ex.Message, row);
            }
        }
        return new AddEmployeesCommand(records);
    }

    private static bool LooksLikeHeader(string line)
    {
        var lower = line.ToLowerInvariant();
        return lower.Contains("name") || lower.Contains("email") || lower.Contains("tel") || lower.Contains("joined");
    }

    private static string? GetCsvField(CsvReader csv, string[] headers, string headerName, bool hasHeader)
    {
        if (hasHeader)
        {
            var idx = Array.FindIndex(headers, h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
            return idx >= 0 ? csv.GetField(idx) : null;
        }
        var pos = headerName switch
        {
            "name" => 0,
            "email" => 1,
            "tel" => 2,
            "joined" => 3,
            _ => -1
        };
        return pos >= 0 ? csv.GetField(pos) : null;
    }

    private sealed class EmployeeImportJsonRecord
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public DateTime? Joined { get; set; }
    }
}
