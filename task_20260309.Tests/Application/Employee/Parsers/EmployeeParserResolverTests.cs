using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using task_20260309.Application.Employee.Parsers;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Parsers;

/// <summary>
/// EmployeeParserResolver 단위 테스트.
/// 내용물(첫 글자가 [ 인지 등)을 보고 적절한 파서(JSON/CSV)를 찾아오는지 확인.
/// </summary>
public class EmployeeParserResolverTests
{
    [Fact]
    public void ResolveForContent_첫_문자가_대괄호면_JsonParser_선택()
    {
        var (resolver, csvParser, jsonParser) = CreateResolver();
        var content = "[{\"name\":\"a\",\"email\":\"a@b.com\"}]";

        var result = resolver.ResolveForContent(content);

        result.Should().Be(jsonParser);
    }

    [Fact]
    public void ResolveForContent_첫_문자가_대괄호_아니면_CsvParser_선택()
    {
        var (resolver, csvParser, jsonParser) = CreateResolver();
        var content = "name,email,tel,joined\n홍길동,hong@example.com,010-1234-5678,2024-01-15";

        var result = resolver.ResolveForContent(content);

        result.Should().Be(csvParser);
    }

    [Fact]
    public void ResolveForContent_공백_후_대괄호면_JsonParser()
    {
        var (resolver, csvParser, jsonParser) = CreateResolver();
        var content = "  [{\"name\":\"x\"}]";

        var result = resolver.ResolveForContent(content);

        result.Should().Be(jsonParser);
    }

    [Fact]
    public void ResolveForFile_csv_확장자면_CsvParser()
    {
        var (resolver, csvParser, jsonParser) = CreateResolver();

        var result = resolver.ResolveForFile("data.csv");

        result.Should().Be(csvParser);
    }

    [Fact]
    public void ResolveForFile_json_확장자면_JsonParser()
    {
        var (resolver, csvParser, jsonParser) = CreateResolver();

        var result = resolver.ResolveForFile("employees.json");

        result.Should().Be(jsonParser);
    }

    [Fact]
    public void ResolveForFile_지원하지_않는_확장자면_null()
    {
        var (resolver, _, _) = CreateResolver();

        var result = resolver.ResolveForFile("data.txt");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveForFile_xml_확장자면_null()
    {
        var (resolver, _, _) = CreateResolver();

        var result = resolver.ResolveForFile("data.xml");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveForContent_null이면_null()
    {
        var (resolver, _, _) = CreateResolver();

        var result = resolver.ResolveForContent(null);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveForContent_빈_문자열이면_null()
    {
        var (resolver, _, _) = CreateResolver();
        var result = resolver.ResolveForContent("");

        result.Should().BeNull();
    }

    [Fact]
    public void 생성자_파서_없으면_InvalidOperationException()
    {
        var act = () => new EmployeeParserResolver(Array.Empty<IEmployeeParser>());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*IEmployeeParser가 하나 이상 등록*");
    }

    private static (EmployeeParserResolver Resolver, CsvEmployeeParser CsvParser, JsonEmployeeParser JsonParser) CreateResolver()
    {
        var csvParser = new CsvEmployeeParser(Mock.Of<ILogger<CsvEmployeeParser>>());
        var jsonParser = new JsonEmployeeParser(Mock.Of<ILogger<JsonEmployeeParser>>());
        var resolver = new EmployeeParserResolver(new IEmployeeParser[] { csvParser, jsonParser });
        return (resolver, csvParser, jsonParser);
    }
}
