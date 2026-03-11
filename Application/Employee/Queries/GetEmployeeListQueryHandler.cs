using task_20260309.Domain.Repositories;

namespace task_20260309.Application.Employee.Queries;

/// <summary>
/// 직원 목록 조회. page/pageSize는 1~100으로 자동 보정.
/// 실패 시: Repository 예외가 그대로 전파됨 (DB 장애 등).
/// </summary>
public class GetEmployeeListQueryHandler
{
    private readonly IEmployeeRepository _repository;
    private readonly ILogger<GetEmployeeListQueryHandler> _logger;

    public GetEmployeeListQueryHandler(IEmployeeRepository repository, ILogger<GetEmployeeListQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GetEmployeeListResult> HandleAsync(GetEmployeeListQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        _logger.LogInformation(
            "Query 시작, QueryName={QueryName}, Page={Page}, PageSize={PageSize}",
            nameof(GetEmployeeListQuery), page, pageSize);

        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, ct);

        var dtos = items.Select(e => new EmployeeListItemDto(e.Id, e.Name, e.Email, e.Tel, e.Joined)).ToList();

        _logger.LogInformation(
            "Query 완료, QueryName={QueryName}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
            nameof(GetEmployeeListQuery), page, pageSize, totalCount, dtos.Count);
        return new GetEmployeeListResult(dtos, totalCount, page, pageSize);
    }
}
