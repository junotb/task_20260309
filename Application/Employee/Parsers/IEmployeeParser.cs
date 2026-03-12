using task_20260309.Application.Employee.Commands;

namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 직원 Import 파서 계약. 확장자/내용 기반 선택을 위해 CanParse* 메서드 제공.
/// </summary>
public interface IEmployeeParser
{
    /// <summary>
    /// 파일 확장자로 처리 가능 여부. .csv, .json 등.
    /// </summary>
    bool CanParseFile(string? fileName);

    /// <summary>
    /// rawData 텍스트로 처리 가능 여부. 첫 문자 등으로 판단.
    /// </summary>
    bool CanParseContent(string? content);

    /// <summary>
    /// 스트림 파싱. stream은 호출자 책임으로 dispose.
    /// </summary>
    Task<AddEmployeesCommand> ParseAsync(Stream stream, CancellationToken ct = default);

    /// <summary>
    /// 문자열 파싱. null/빈 문자열이면 빈 Command 반환.
    /// </summary>
    Task<AddEmployeesCommand> ParseFromStringAsync(string text, CancellationToken ct = default);
}
