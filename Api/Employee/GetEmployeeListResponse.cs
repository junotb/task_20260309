namespace task_20260309.Api.Employee;

/// <summary>
/// 직원 목록 응답. Id 오름차순.
/// </summary>
public record GetEmployeeListResponse(
    IReadOnlyList<EmployeeResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);
