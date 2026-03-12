using FluentAssertions;
using task_20260309.Domain.ValueObjects;
using Xunit;

namespace task_20260309.Tests.Domain.ValueObjects;

/// <summary>
/// Email VO 도메인 단위 테스트.
/// 유효하지 않은 이메일 시 예외, 대문자 정규화 검증.
/// </summary>
public class EmailTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_공백이나_null이면_ArgumentException(string? value)
    {
        var act = () => Email.Create(value!);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*이메일은 필수입니다*")
            .And.ParamName.Should().Be("value");
    }

    [Theory]
    [InlineData("invalid")]           // @ 누락
    [InlineData("invalid@")]          // 도메인 누락
    [InlineData("@domain.com")]       // 로컬 파트 누락
    [InlineData("user@domain")]       // TLD 누락
    [InlineData("user @domain.com")]  // 공백 포함
    public void Create_유효하지_않은_이메일_형식이면_ArgumentException(string value)
    {
        var act = () => Email.Create(value);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*올바른 이메일 형식이 아닙니다*")
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void Create_320자_초과_이메일이면_ArgumentException()
    {
        // 유효한 형식(형식 검사 통과)이면서 320자 초과
        var longEmail = "a@" + new string('b', 316) + ".cc"; // 322 chars
        longEmail.Length.Should().BeGreaterThan(320);

        var act = () => Email.Create(longEmail);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*320자를 초과할 수 없습니다*");
    }

    [Theory]
    [InlineData("User@Example.COM", "user@example.com")]
    [InlineData("TEST@DOMAIN.CO.KR", "test@domain.co.kr")]
    [InlineData("Hong.Gildong@Company.Org", "hong.gildong@company.org")]
    public void Create_대문자가_소문자로_정규화된다(string input, string expectedNormalized)
    {
        var email = Email.Create(input);

        email.Value.Should().Be(input.Trim());
        email.Normalized.Should().Be(expectedNormalized);
    }

    [Theory]
    [InlineData("valid@example.com")]
    [InlineData("user.name@domain.co.kr")]
    [InlineData("user+tag@example.org")]
    public void Create_유효한_이메일이면_성공(string value)
    {
        var email = Email.Create(value);
        email.Value.Should().Be(value);
        email.Normalized.Should().Be(value.ToLowerInvariant());
    }

    [Fact]
    public void IsValidFormat_유효한_형식은_true()
    {
        Email.IsValidFormat("user@example.com").Should().BeTrue();
        Email.IsValidFormat("  user@example.com  ").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("no-at-sign")]
    public void IsValidFormat_무효_형식은_false(string? value)
    {
        Email.IsValidFormat(value).Should().BeFalse();
    }

    [Fact]
    public void Normalize_공백_제거_및_소문자_변환()
    {
        Email.Normalize("  User@Example.COM  ").Should().Be("user@example.com");
        Email.Normalize(null).Should().Be("");
    }

    [Fact]
    public void FromNormalized_이미_정규화된_값으로_생성()
    {
        var email = Email.FromNormalized("user@example.com");
        email.Normalized.Should().Be("user@example.com");
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void TryCreate_유효하면_true와_email_반환()
    {
        var ok = Email.TryCreate("user@example.com", out var email);
        ok.Should().BeTrue();
        email.Should().NotBeNull();
        email!.Normalized.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public void TryCreate_무효하면_false와_null(string? value)
    {
        var ok = Email.TryCreate(value, out var email);
        ok.Should().BeFalse();
        email.Should().BeNull();
    }
}
