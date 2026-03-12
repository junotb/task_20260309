namespace task_20260309.Application.Employee.Parsers;

/// <summary>
/// 파싱 실패. SourceType, FileName으로 원인 추적.
/// </summary>
public sealed class ParseException(string message, Exception inner, string sourceType, string? fileName) : Exception(message, inner)
{
    public string SourceType { get; } = sourceType;
    public string? FileName { get; } = fileName;
}
