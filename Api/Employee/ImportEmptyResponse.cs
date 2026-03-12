namespace task_20260309.Api.Employee;

/// <summary>
/// 빈 입력 또는 파싱 후 유효 직원 0건. hint는 클라이언트 안내용.
/// </summary>
public record ImportEmptyResponse(string Message, string Hint, int Received, int Imported);
