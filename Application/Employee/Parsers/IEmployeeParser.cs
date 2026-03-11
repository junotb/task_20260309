using task_20260309.Application.Employee.Commands;

namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 직원 Import 파서 계약. 구현체는 다음을 보장해야 함:
/// - 형식 감지: 첫 문자 '[' → JSON, 그 외 → CSV
/// - 인코딩: UTF-8 시도, �(U+FFFD) 발견 시 EUC-KR 재시도
/// - 빈/무효 행은 건너뛰고, 파싱 실패 행은 로그 후 계속. 예외는 치명적 오류(형식 오류 등)에만.
/// </summary>
public interface IEmployeeParser
{
    /// <summary>
    /// 스트림 파싱. stream은 호출자 책임으로 dispose. 구현: 전체를 MemoryStream에 복사 후 문자열 파싱.
    /// 예외: JSON 배열 아님, CSV 구조 오류 등 치명적 형식 오류 시 throw.
    /// </summary>
    Task<AddEmployeesCommand> ParseAsync(Stream stream, CancellationToken ct = default);

    /// <summary>
    /// 문자열 파싱. null/빈 문자열이면 빈 Command 반환. trim 후 첫 문자로 형식 감지.
    /// </summary>
    Task<AddEmployeesCommand> ParseFromStringAsync(string text, CancellationToken ct = default);
}
