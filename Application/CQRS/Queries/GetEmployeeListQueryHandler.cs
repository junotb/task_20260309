using Microsoft.EntityFrameworkCore;
using task_20260309.Infrastructure.Data;

namespace task_20260309.Application.CQRS.Queries;

public class GetEmployeeListQueryHandler
{
    private readonly AppDbContext _db;

    public GetEmployeeListQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetEmployeeListResult> HandleAsync(GetEmployeeListQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var queryable = _db.Employees.AsNoTracking().OrderBy(e => e.Id);

        var totalCount = await queryable.CountAsync(ct);

        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeListItemDto(e.Id, e.Name, e.Email, e.Tel, e.Joined))
            .ToListAsync(ct);

        return new GetEmployeeListResult(items, totalCount, page, pageSize);
    }
}
