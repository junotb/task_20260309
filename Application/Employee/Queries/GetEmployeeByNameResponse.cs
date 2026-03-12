namespace task_20260309.Application.Employee.Queries;

/// <summary>
/// 이름 조회 결과. 동명이인 포함하여 0건 이상. 정확히 일치하는 이름의 직원 목록.
/// </summary>
/// <param name="Employees">이름으로 조회된 직원 상세 목록. Id 오름차순.</param>
public record GetEmployeeByNameResponse(IReadOnlyList<EmployeeDetailDto> Employees);

/// <summary>
/// 직원 상세. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeDetailDto(int Id, string Name, string Email, string Tel, DateTime Joined);
