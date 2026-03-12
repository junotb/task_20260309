using FluentAssertions;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Parsers;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Parsers;

/// <summary>
/// ParseException 테스트.
/// 파싱 실패 시 Handler에서 ParseException으로 래핑되어 ParseFailedResponse로 반환됨.
/// ParseException 클래스 생성자 및 속성 검증.
/// </summary>
public class ParseExceptionTests
{
    [Fact]
    public void ParseException_생성_및_속성_검증()
    {
        var inner = new InvalidOperationException("Invalid JSON");
        var ex = new ParseException("파싱 실패", inner, "File", "test.json");

        ex.Message.Should().Be("파싱 실패");
        ex.InnerException.Should().Be(inner);
        ex.SourceType.Should().Be("File");
        ex.FileName.Should().Be("test.json");
    }

    [Fact]
    public void ParseException_FileName_null_가능()
    {
        var ex = new ParseException("rawData 오류", new Exception("inner"), "Textarea", null);

        ex.SourceType.Should().Be("Textarea");
        ex.FileName.Should().BeNull();
    }

    [Fact]
    public void ParseException_Exception_상속()
    {
        var ex = new ParseException("msg", new InvalidOperationException(), "Source", "file.csv");
        ex.Should().BeAssignableTo<Exception>();
    }
}
