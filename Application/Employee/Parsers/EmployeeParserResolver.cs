namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 등록된 파서 목록 중 파일/내용에 맞는 파서를 선택.
/// </summary>
public class EmployeeParserResolver : IEmployeeParserResolver
{
    private readonly IReadOnlyList<IEmployeeParser> _parsers;

    public EmployeeParserResolver(IEnumerable<IEmployeeParser> parsers)
    {
        _parsers = parsers.ToList();
        if (_parsers.Count == 0)
            throw new InvalidOperationException("IEmployeeParser가 하나 이상 등록되어야 합니다.");
    }

    public IEmployeeParser? ResolveForFile(string? fileName)
    {
        return _parsers.FirstOrDefault(p => p.CanParseFile(fileName));
    }

    public IEmployeeParser? ResolveForContent(string? content)
    {
        return _parsers.FirstOrDefault(p => p.CanParseContent(content));
    }
}
