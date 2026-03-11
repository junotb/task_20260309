using task_20260309.Domain.Entities;

namespace task_20260309.Domain.Repositories;

/// <summary>
/// Employee 영속성 계약. 구현체는 DbContext 스코프 내에서 동작해야 하며,
/// Add 후 SaveChangesAsync 호출 전까지 트랜잭션에 포함됨. Get/Exists는 반드시 AsNoTracking.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// 페이지네이션된 직원 목록과 총 건수. page·pageSize는 호출자가 1 이상으로 보정한 상태로 전달됨.
    /// 구현: Id 오름차순, AsNoTracking.
    /// </summary>
    Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// 이름이 완전 일치하는 직원 1건. 대소문자 구분. 없으면 null.
    /// 구현: AsNoTracking.
    /// </summary>
    Task<Employee?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// 이메일 존재 여부. email은 Trim·ToLowerInvariant 적용된 상태로 전달됨.
    /// 구현: 대소문자 무시 비교.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// 직원 추가. SaveChangesAsync 전까지 메모리에만 반영. 동일 DbContext 스코프 내에서 호출 필수.
    /// </summary>
    void Add(Employee employee);

    /// <summary>
    /// Add된 변경 사항을 DB에 반영. 실패 시 예외. 트랜잭션 커밋.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
