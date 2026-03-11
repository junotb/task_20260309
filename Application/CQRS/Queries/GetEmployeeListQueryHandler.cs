using Microsoft.EntityFrameworkCore;
using task_20260309.Infrastructure.Data;

namespace task_20260309.Application.CQRS.Queries;

public class GetEmployeeListQueryHandler
{
    private readonly AppDbContext _db;
    private readonly ILogger<GetEmployeeListQueryHandler> _logger;

    public GetEmployeeListQueryHandler(AppDbContext db, ILogger<GetEmployeeListQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GetEmployeeListResult> HandleAsync(GetEmployeeListQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        _logger.LogInformation(
            "Query 시작, QueryName={QueryName}, Page={Page}, PageSize={PageSize}",
            nameof(GetEmployeeListQuery), page, pageSize);

        var queryable = _db.Employees.AsNoTracking().OrderBy(e => e.Id);

        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(e.Id, e.Name, e.Email, e.Tel, e.Joined))
            .ToListAsync(ct);

        _logger.LogInformation(
            "Query 완료, QueryName={QueryName}, Page={Page}, PageSize={PageSize}, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
            nameof(GetEmployeeListQuery), page, pageSize, totalCount, items.Count);
        return new GetEmployeeListResult(items, totalCount, page, pageSize);
    }
}
