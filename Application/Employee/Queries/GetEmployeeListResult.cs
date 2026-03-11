namespace task_20260309.Application.Employee.Queries;

/// <summary>
/// 직원 목록 조회 결과. Id 오름차순 페이지네이션.
/// </summary>
public record GetEmployeeListResult(
    IReadOnlyList<EmployeeListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

/// <summary>
/// 직원 목록 1건. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeListItemDto(int Id, string Name, string Email, string Tel, DateTime Joined);
