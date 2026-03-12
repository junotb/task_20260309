using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Parsers;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Parsers;

/// <summary>
/// JsonEmployeeParser 단위 테스트.
/// JSON 배열 문자열을 정확히 엔티티 리스트로 변환하는지 검증.
/// </summary>
public class JsonEmployeeParserTests
{
    private readonly JsonEmployeeParser _sut;

    public JsonEmployeeParserTests()
    {
        var logger = new Mock<ILogger<JsonEmployeeParser>>();
        _sut = new JsonEmployeeParser(logger.Object);
    }

    [Fact]
    public async Task ParseFromStringAsync_유효한_JSON_배열_파싱()
    {
        var json = """
            [
                {"name":"홍길동","email":"hong@example.com","tel":"010-1234-5678","joined":"2024-01-15"},
                {"name":"김철수","email":"kim@example.com","tel":"02-123-4567","joined":"2023-06-01"}
            ]
            """;

        var result = await _sut.ParseFromStringAsync(json);

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
    public async Task ParseFromStringAsync_camelCase_속성_인식()
    {
        var json = """[{"name":"테스트","email":"test@example.com","tel":"010-1111-2222","joined":"2024-03-01"}]""";

        var result = await _sut.ParseFromStringAsync(json);

        result.Employees.Should().HaveCount(1);
        result.Employees[0].Name.Should().Be("테스트");
        result.Employees[0].Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task ParseFromStringAsync_빈_배열이면_빈_리스트()
    {
        var result = await _sut.ParseFromStringAsync("[]");
        result.Employees.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseFromStringAsync_빈_문자열이면_빈_리스트()
    {
        var result = await _sut.ParseFromStringAsync("");
        result.Employees.Should().BeEmpty();
    }

    [Fact]
    public void CanParseContent_첫_문자가_대괄호면_true()
    {
        _sut.CanParseContent("[{}]").Should().BeTrue();
        _sut.CanParseContent("  [{\"name\":\"a\"}]").Should().BeTrue();
    }

    [Fact]
    public void CanParseContent_첫_문자가_대괄호가_아니면_false()
    {
        _sut.CanParseContent("name,email,tel").Should().BeFalse();
    }

    [Fact]
    public void CanParseFile_json_확장자면_true()
    {
        _sut.CanParseFile("data.json").Should().BeTrue();
        _sut.CanParseFile("export.JSON").Should().BeTrue();
    }

    [Fact]
    public void CanParseFile_csv_확장자면_false()
    {
        _sut.CanParseFile("data.csv").Should().BeFalse();
    }
}
