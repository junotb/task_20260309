namespace task_20260309.Application.Employee.Queries;

/// <summary>
/// 이름 조회 결과. 정확히 일치하는 1건 없으면 null.
/// </summary>
public record GetEmployeeByNameResult(EmployeeDetailDto? Employee);

/// <summary>
/// 직원 상세. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeDetailDto(int Id, string Name, string Email, string Tel, DateTime Joined);
