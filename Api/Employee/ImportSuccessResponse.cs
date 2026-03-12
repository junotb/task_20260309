using task_20260309.Application.Employee;

namespace task_20260309.Api.Employee;

/// <summary>
/// Import 성공. imported=실제 등록 대상, skipped=중복·검증 실패로 제외된 건수.
/// </summary>
public record ImportSuccessResponse(
    string Message,
    int Imported,
    int Skipped,
    int Total,
    IReadOnlyList<ImportValidationError>? Errors);
