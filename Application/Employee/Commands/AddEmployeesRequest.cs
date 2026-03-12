namespace task_20260309.Application.Employee.Commands;

/// <summary>
/// AddEmployees Handler 입력. 파일 스트림은 Handler가 읽은 후 dispose.
/// </summary>
/// <param name="FileStream">파일 스트림. 있으면 Handler가 읽고 dispose.</param>
/// <param name="FileName">파일명. 확장자 기반 파서 선택용.</param>
/// <param name="RawData">rawData 텍스트.</param>
public record AddEmployeesRequest(
    Stream? FileStream,
    string? FileName,
    string? RawData);
