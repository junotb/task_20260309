namespace task_20260309.Application.CQRS.Queries;

public record GetEmployeeByNameResult(EmployeeDetailDto? Employee);

public record EmployeeDetailDto(int Id, string Name, string Email, string Tel, DateTime Joined);
