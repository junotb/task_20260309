namespace task_20260309.Application.CQRS.Queries;

public record GetEmployeeListResult(
    IReadOnlyList<EmployeeListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record EmployeeListItemDto(int Id, string Name, string Email, string Tel, DateTime Joined);
