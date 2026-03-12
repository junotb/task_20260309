using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Validators;
using task_20260309.Domain.Repositories;
using task_20260309.Domain.ValueObjects;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Validators;

/// <summary>
/// AddEmployeesCommandValidator 단위 테스트.
/// FluentValidation 규칙이 이메일 형식, 필수값 누락 등을 잘 잡아내는지 확인.
/// </summary>
public class AddEmployeesCommandValidatorTests
{
    private readonly AddEmployeesCommandValidator _sut;

    public AddEmployeesCommandValidatorTests()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var itemValidator = new EmployeeImportDtoValidator(repo.Object);
        _sut = new AddEmployeesCommandValidator(itemValidator);
    }

    [Fact]
    public async Task Validate_빈_직원_목록이면_NotEmpty_실패()
    {
        var command = new AddEmployeesCommand([]);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "직원 데이터가 비어있습니다.");
    }

    [Fact]
    public async Task Validate_이메일_형식_오류_시_실패()
    {
        var employees = new List<EmployeeImportDto>
        {
            new("홍길동", "invalid-email", "010-1234-5678", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "올바른 이메일 형식이 아닙니다.");
    }

    [Fact]
    public async Task Validate_이름_필수_누락_시_실패()
    {
        var employees = new List<EmployeeImportDto>
        {
            new("", "hong@example.com", "010-1234-5678", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "이름은 필수입니다.");
    }

    [Fact]
    public async Task Validate_전화번호_형식_오류_시_실패()
    {
        var employees = new List<EmployeeImportDto>
        {
            new("홍길동", "hong@example.com", "invalid-tel!!!", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "전화번호 형식이 올바르지 않습니다.");
    }

    [Fact]
    public async Task Validate_이메일_필수_누락_시_실패()
    {
        var employees = new List<EmployeeImportDto>
        {
            new("홍길동", "", "010-1234-5678", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "이메일은 필수입니다.");
    }

    [Fact]
    public async Task Validate_이름_200자_초과_시_실패()
    {
        var longName = new string('가', 201);
        var employees = new List<EmployeeImportDto>
        {
            new(longName, "hong@example.com", "010-1234-5678", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "이름은 200자를 초과할 수 없습니다.");
    }

    [Fact]
    public async Task Validate_유효한_데이터_시_성공()
    {
        var employees = new List<EmployeeImportDto>
        {
            new("홍길동", "hong@example.com", "010-1234-5678", new DateTime(2024, 1, 15))
        };
        var command = new AddEmployeesCommand(employees);

        var result = await _sut.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_이미_등록된_이메일_시_실패()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(r => r.ExistsByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var itemValidator = new EmployeeImportDtoValidator(repo.Object);
        var validator = new AddEmployeesCommandValidator(itemValidator);

        var employees = new List<EmployeeImportDto>
        {
            new("홍길동", "existing@example.com", "010-1234-5678", DateTime.UtcNow.Date)
        };
        var command = new AddEmployeesCommand(employees);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "이미 등록된 이메일입니다.");
    }
}
