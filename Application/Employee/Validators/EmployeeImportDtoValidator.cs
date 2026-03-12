using FluentValidation;
using task_20260309.Application.Common;
using task_20260309.Application.Employee.Commands;
using task_20260309.Domain.Repositories;
using task_20260309.Domain.ValueObjects;

namespace task_20260309.Application.Employee.Validators;

public class EmployeeImportDtoValidator : AbstractValidator<EmployeeImportDto>
{
    public EmployeeImportDtoValidator(IEmployeeRepository repository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("이름은 필수입니다.")
            .MaximumLength(200).WithMessage("이름은 200자를 초과할 수 없습니다.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("이메일은 필수입니다.")
            .Must(Email.IsValidFormat).WithMessage("올바른 이메일 형식이 아닙니다.")
            .MaximumLength(320)
            .MustAsync(async (_, email, ct) =>
            {
                if (!Email.TryCreate(email, out var emailVo)) return true; // 형식 오류는 위 규칙에서 처리
                return !await repository.ExistsByEmailAsync(emailVo, ct);
            })
            .WithMessage("이미 등록된 이메일입니다.");

        RuleFor(x => x.Tel)
            .NotEmpty().WithMessage("전화번호는 필수입니다.")
            .Must(ValidationPatterns.IsValidTel).WithMessage("전화번호 형식이 올바르지 않습니다.")
            .MaximumLength(50);

        RuleFor(x => x.Joined)
            .NotEmpty().WithMessage("입사일은 필수입니다.");
    }
}
