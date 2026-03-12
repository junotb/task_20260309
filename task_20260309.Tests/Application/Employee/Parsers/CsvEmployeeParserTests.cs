using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Parsers;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Parsers;

/// <summary>
/// CsvEmployeeParser 단위 테스트.
/// CSV 문자열을 정확히 엔티티(EmployeeImportDto) 리스트로 변환하는지 검증.
/// </summary>
public class CsvEmployeeParserTests
{
    private readonly CsvEmployeeParser _sut;

    public CsvEmployeeParserTests()
    {
        var logger = new Mock<ILogger<CsvEmployeeParser>>();
        _sut = new CsvEmployeeParser(logger.Object);
    }

    [Fact]
    public async Task ParseFromStringAsync_헤더_있는_CSV_파싱()
    {
        var csv = """
            name,email,tel,joined
            홍길동,hong@example.com,010-1234-5678,2024-01-15
            김철수,kim@example.com,02-123-4567,2023-06-01
            """;

        var result = await _sut.ParseFromStringAsync(csv);

        result.Employees.Should().HaveCount(2);
        result.Employees[0].Name.Should().Be("홍길동");
        result.Employees[0].Email.Should().Be("hong@example.com");
        result.Employees[0].Tel.Should().Be("010-1234-5678");
        result.Employees[0].Joined.Should().Be(new DateTime(2024, 1, 15));

        result.Employees[1].Name.Should().Be("김철수");
        result.Employees[1].Email.Should().Be("kim@example.com");
        result.Employees[1].Tel.Should().Be("02-123-4567");
        result.Employees[1].Joined.Should().Be(new DateTime(2023, 6, 1));
    }

    [Fact]
    public async Task ParseFromStringAsync_헤더_없는_CSV_파싱()
    {
        var csv = """
            이영희,lee@example.com,031-987-6543,2022-12-01
            """;

        var result = await _sut.ParseFromStringAsync(csv);

        result.Employees.Should().HaveCount(1);
        result.Employees[0].Name.Should().Be("이영희");
        result.Employees[0].Email.Should().Be("lee@example.com");
        result.Employees[0].Tel.Should().Be("031-987-6543");
        result.Employees[0].Joined.Should().Be(new DateTime(2022, 12, 1));
    }

    [Fact]
    public async Task ParseFromStringAsync_빈_문자열이면_빈_리스트()
    {
        var result = await _sut.ParseFromStringAsync("");
        result.Employees.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseFromStringAsync_공백만_있으면_빈_리스트()
    {
        var result = await _sut.ParseFromStringAsync("   ");
        result.Employees.Should().BeEmpty();
    }

    [Fact]
    public void CanParseContent_첫_문자가_대괄호가_아니면_true()
    {
        _sut.CanParseContent("name,email,tel,joined").Should().BeTrue();
        _sut.CanParseContent("a,b,c,d").Should().BeTrue();
    }

    [Fact]
    public void CanParseContent_첫_문자가_대괄호면_false()
    {
        _sut.CanParseContent("[{\"name\":\"a\"}]").Should().BeFalse();
    }

    [Fact]
    public void CanParseFile_csv_확장자면_true()
    {
        _sut.CanParseFile("data.csv").Should().BeTrue();
        _sut.CanParseFile("export.CSV").Should().BeTrue();
    }

    [Fact]
    public void CanParseFile_json_확장자면_false()
    {
        _sut.CanParseFile("data.json").Should().BeFalse();
    }
}
