namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 파일 확장자/내용에 맞는 IEmployeeParser 선택.
/// </summary>
public interface IEmployeeParserResolver
{
    /// <summary>
    /// 파일 확장자로 파서 선택. 없으면 null.
    /// </summary>
    IEmployeeParser? ResolveForFile(string? fileName);

    /// <summary>
    /// rawData 내용으로 파서 선택. JSON 우선, 없으면 CSV 등. 없으면 null.
    /// </summary>
    IEmployeeParser? ResolveForContent(string? content);
}
