using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using task_20260309.Application.CQRS.Commands;

namespace task_20260309.Application.Parsers;

public static class EmployeeImportParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<AddEmployeesCommand> ParseCsvAsync(Stream stream, CancellationToken ct = default)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant() ?? ""
        };

        using var csv = new CsvReader(reader, config);
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];

        var records = new List<EmployeeImportDto>();
        while (await csv.ReadAsync())
        {
            var name = GetCsvField(csv, headers, "name");
            var email = GetCsvField(csv, headers, "email");
            var tel = GetCsvField(csv, headers, "tel");
            var joinedStr = GetCsvField(csv, headers, "joined");

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
                continue;

            if (!DateTime.TryParse(joinedStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var joined))
                joined = DateTime.UtcNow.Date;

            records.Add(new EmployeeImportDto(name?.Trim() ?? "", email?.Trim() ?? "", tel?.Trim() ?? "", joined));
        }

        return new AddEmployeesCommand(records);
    }

    private static string? GetCsvField(CsvReader csv, string[] headers, string headerName)
    {
        var idx = Array.FindIndex(headers, h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        return idx >= 0 ? csv.GetField(idx) : null;
    }

    public static AddEmployeesCommand ParseCsvFromString(string csvText)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvText));
        return ParseCsvAsync(stream).GetAwaiter().GetResult();
    }

    public static async Task<AddEmployeesCommand> ParseJsonAsync(Stream stream, CancellationToken ct = default)
    {
        var list = await JsonSerializer.DeserializeAsync<List<EmployeeImportJsonRecord>>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException("JSON 파싱 실패");

        var dtos = list
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) || !string.IsNullOrWhiteSpace(r.Email))
            .Select(r =>
            {
                var joined = r.Joined.HasValue ? r.Joined.Value : DateTime.UtcNow.Date;
                return new EmployeeImportDto(
                    r.Name?.Trim() ?? "",
                    r.Email?.Trim() ?? "",
                    r.Tel?.Trim() ?? "",
                    joined);
            })
            .ToList();

        return new AddEmployeesCommand(dtos);
    }

    private sealed class EmployeeImportJsonRecord
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public DateTime? Joined { get; set; }
    }
}
