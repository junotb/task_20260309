using System.Text.RegularExpressions;
using FluentValidation;
using task_20260309.Application.Employee.Commands;
using task_20260309.Domain.Repositories;

namespace task_20260309.Application.Employee.Validators;

public class EmployeeImportDtoValidator : AbstractValidator<EmployeeImportDto>
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex TelRegex = new(
        @"^[\d\-\s+()\.]+$",
        RegexOptions.Compiled);

    public EmployeeImportDtoValidator(IEmployeeRepository repository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("이름은 필수입니다.")
            .MaximumLength(200).WithMessage("이름은 200자를 초과할 수 없습니다.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("이메일은 필수입니다.")
            .Must(BeValidEmail).WithMessage("올바른 이메일 형식이 아닙니다.")
            .MaximumLength(320)
            .MustAsync(async (dto, email, ct) => !await repository.ExistsByEmailAsync((email ?? "").Trim().ToLowerInvariant(), ct))
            .WithMessage("이미 등록된 이메일입니다.");

        RuleFor(x => x.Tel)
            .NotEmpty().WithMessage("전화번호는 필수입니다.")
            .Must(BeValidTel).WithMessage("전화번호 형식이 올바르지 않습니다.")
            .MaximumLength(50);

        RuleFor(x => x.Joined)
            .NotEmpty().WithMessage("입사일은 필수입니다.");
    }

    private static bool BeValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return EmailRegex.IsMatch(email.Trim());
    }

    private static bool BeValidTel(string? tel)
    {
        if (string.IsNullOrWhiteSpace(tel)) return false;
        return TelRegex.IsMatch(tel.Trim());
    }
}
