namespace task_20260309.Application.Employee.Commands;

/// <summary>
/// 직원 일괄 추가 요청 명령. 채널을 통해 비동기로 DB에 저장됨.
/// </summary>
/// <param name="Employees">유효성 검사 통과한 직원 목록. 이메일 중복 제거 완료 상태.</param>
public record AddEmployeesCommand(IReadOnlyList<EmployeeImportDto> Employees);

/// <summary>
/// 직원 Import용 DTO. CSV/JSON 파싱 결과. DB 저장 전 검증 대상.
/// </summary>
/// <param name="Name">이름. 필수, 최대 200자. 예: 홍길동</param>
/// <param name="Email">이메일. 필수, DB 내 유일. 예: hong@example.com</param>
/// <param name="Tel">전화번호. 필수, 숫자/하이픈/공백/괄호. 예: 010-1234-5678</param>
/// <param name="Joined">입사일. 필수. 예: 2024-01-15</param>
public record EmployeeImportDto(string Name, string Email, string Tel, DateTime Joined);
