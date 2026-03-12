using task_20260309.Application.Employee;

namespace task_20260309.Application.Employee.Commands;

/// <summary>
/// AddEmployees Handler 결과. 판별 유니온 스타일.
/// </summary>
public abstract record AddEmployeesResponse;

/// <summary>
/// 입력 없음 또는 파싱 후 유효 직원 0건.
/// </summary>
public record AddEmployeesEmptyResponse(string Message, string Hint, int Received, int Imported) : AddEmployeesResponse;

/// <summary>
/// 파싱 실패. 형식 오류 등.
/// </summary>
public record AddEmployeesParseFailedResponse(string Message) : AddEmployeesResponse;

/// <summary>
/// 검증 실패. FluentValidation 또는 병합 중복.
/// </summary>
public record AddEmployeesValidationFailedResponse(string Message, int Total, IReadOnlyList<ImportValidationError> Errors) : AddEmployeesResponse;

/// <summary>
/// 채널 전달 성공.
/// </summary>
public record AddEmployeesSuccessResponse(int Imported, int Skipped, int Total, IReadOnlyList<ImportValidationError>? Errors) : AddEmployeesResponse;
