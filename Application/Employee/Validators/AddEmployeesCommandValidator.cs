using FluentValidation;
using task_20260309.Application.Employee.Commands;

namespace task_20260309.Application.Employee.Validators;

/// <summary>
/// 직원 일괄 추가 Command 검증. 병합 단계에서 이미 이메일 중복이 제거되므로
/// 여기서는 NotEmpty 및 항목별 검증만 수행합니다.
/// </summary>
public class AddEmployeesCommandValidator : AbstractValidator<AddEmployeesCommand>
{
    public AddEmployeesCommandValidator(EmployeeImportDtoValidator itemValidator)
    {
        RuleFor(x => x.Employees)
            .NotEmpty().WithMessage("직원 데이터가 비어있습니다.");

        RuleForEach(x => x.Employees).SetValidator(itemValidator);
    }
}
