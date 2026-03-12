namespace task_20260309.Api.Employee;

/// <summary>
/// 직원 1건. 목록/상세 공통. Swagger 예: Id=1, Name=홍길동, Email=hong@example.com, Tel=010-1234-5678, Joined=2024-01-15
/// </summary>
public record EmployeeResponse(int Id, string Name, string Email, string Tel, DateTime Joined);
