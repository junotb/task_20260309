using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using task_20260309.Application.Common;
using task_20260309.Application.Employee.Commands;
using task_20260309.Infrastructure.Common;

namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// CSV 형식 직원 파서. 헤더 복구, UTF-8/EUC-KR 인코딩 대응.
/// </summary>
public class CsvEmployeeParser : IEmployeeParser
{
    private readonly ILogger<CsvEmployeeParser> _logger;

    public CsvEmployeeParser(ILogger<CsvEmployeeParser> logger)
    {
        _logger = logger;
    }

    public bool CanParseFile(string? fileName)
    {
        return FileExtensionHelper.GetNormalizedExtension(fileName) == "csv";
    }

    public bool CanParseContent(string? content)
    {
        var trimmed = content?.TrimStart();
        if (string.IsNullOrEmpty(trimmed)) return false;
        return trimmed[0] != '[';
    }

    public async Task<AddEmployeesCommand> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        var text = await EncodingHelper.ReadStreamWithEncodingAsync(stream, ct);
        return ParseCsv(text);
    }

    public Task<AddEmployeesCommand> ParseFromStringAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(new AddEmployeesCommand([]));
        return Task.FromResult(ParseCsv(text));
    }

    private AddEmployeesCommand ParseCsv(string text)
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
}
