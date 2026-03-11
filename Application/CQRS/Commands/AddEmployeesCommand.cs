namespace task_20260309.Application.CQRS.Commands;

public record AddEmployeesCommand(IReadOnlyList<EmployeeImportDto> Employees);

public record EmployeeImportDto(string Name, string Email, string Tel, DateTime Joined);
