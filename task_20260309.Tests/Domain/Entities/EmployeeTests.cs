using FluentAssertions;
using task_20260309.Domain.Entities;
using task_20260309.Domain.ValueObjects;
using Xunit;

namespace task_20260309.Tests.Domain.Entities;

/// <summary>
/// Employee 엔티티 도메인 단위 테스트.
/// 엔티티 생성 시 기본값 설정 및 비즈니스 규칙 검증.
/// </summary>
public class EmployeeTests
{
    [Fact]
    public void Employee_필수_속성으로_생성_가능()
    {
        var email = Email.Create("hong@example.com");
        var joined = new DateTime(2024, 1, 15);

        var employee = new Employee
        {
            Name = "홍길동",
            Email = email,
            Tel = "010-1234-5678",
            Joined = joined
        };

        employee.Name.Should().Be("홍길동");
        employee.Email.Should().Be(email);
        employee.Email.Normalized.Should().Be("hong@example.com");
        employee.Tel.Should().Be("010-1234-5678");
        employee.Joined.Should().Be(joined);
        employee.Id.Should().Be(0); // 새 엔티티는 기본값 0
    }

    [Fact]
    public void Employee_Id는_기본값_0()
    {
        var employee = new Employee
        {
            Name = "김철수",
            Email = Email.Create("kim@example.com"),
            Tel = "02-123-4567",
            Joined = DateTime.UtcNow.Date
        };

        employee.Id.Should().Be(0);
    }

    [Fact]
    public void Employee_Id_설정_가능()
    {
        var employee = new Employee
        {
            Id = 42,
            Name = "이영희",
            Email = Email.Create("lee@example.com"),
            Tel = "031-987-6543",
            Joined = new DateTime(2023, 6, 1)
        };

        employee.Id.Should().Be(42);
    }

    [Fact]
    public void Employee_이메일_정규화_검증()
    {
        var email = Email.Create("User@EXAMPLE.com");
        var employee = new Employee
        {
            Name = "정규화테스트",
            Email = email,
            Tel = "010-1111-2222",
            Joined = DateTime.UtcNow.Date
        };

        employee.Email.Normalized.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("010-1234-5678")]
    [InlineData("02-123-4567")]
    [InlineData("031 123 4567")]
    [InlineData("+82-10-1234-5678")]
    public void Employee_다양한_전화번호_형식_지원(string tel)
    {
        var employee = new Employee
        {
            Name = "전화테스트",
            Email = Email.Create("tel@example.com"),
            Tel = tel,
            Joined = DateTime.UtcNow.Date
        };

        employee.Tel.Should().Be(tel);
    }
}
