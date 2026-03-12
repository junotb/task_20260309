namespace task_20260309.Application.Employee;

/// <summary>
/// 검증 오류 1건. index=1-based 행 번호, errors=오류 메시지 배열.
/// </summary>
public record ImportValidationError(int Index, string Email, IReadOnlyList<string> Errors);
