namespace task_20260309.Api.Employee;

/// <summary>
/// 이름으로 조회한 결과. 동명이인 포함, items는 Id 오름차순.
/// </summary>
public record GetEmployeeByNameResponse(IReadOnlyList<EmployeeResponse> Items);
