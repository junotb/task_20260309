namespace task_20260309.Api.Employee;

/// <summary>
/// 직원 목록 조회 쿼리. Page &lt; 1이면 1, PageSize는 1~100 클램프.
/// </summary>
/// <param name="Page">페이지 번호. 기본 1. 예: 1</param>
/// <param name="PageSize">페이지당 건수. 기본 10. 예: 10</param>
public record GetEmployeeListRequest(int Page = 1, int PageSize = 10);
