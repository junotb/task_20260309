using FluentValidation;
using task_20260309.Application.CQRS.Commands;

namespace task_20260309.Application.Validators;

public class AddEmployeesCommandValidator : AbstractValidator<AddEmployeesCommand>
{
    public AddEmployeesCommandValidator(EmployeeImportDtoValidator itemValidator)
    {
        RuleFor(x => x.Employees)
            .NotEmpty().WithMessage("직원 데이터가 비어있습니다.");

        RuleForEach(x => x.Employees).SetValidator(itemValidator);

        RuleFor(x => x.Employees)
            .Must(HaveNoDuplicateEmails).WithMessage("같은 이메일이 여러 번 포함되어 있습니다.");
    }

    private static bool HaveNoDuplicateEmails(IReadOnlyList<EmployeeImportDto> list)
    {
        var emails = list.Select(e => e.Email.Trim().ToLowerInvariant()).ToList();
        return emails.Distinct().Count() == emails.Count;
    }
}
