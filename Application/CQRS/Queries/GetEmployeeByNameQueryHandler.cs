using Microsoft.EntityFrameworkCore;
using task_20260309.Infrastructure.Data;

namespace task_20260309.Application.CQRS.Queries;

public class GetEmployeeByNameQueryHandler
{
    private readonly AppDbContext _db;

    public GetEmployeeByNameQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetEmployeeByNameResult> HandleAsync(GetEmployeeByNameQuery query, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Name == query.Name)
            .Select(e => new EmployeeDetailDto(e.Id, e.Name, e.Email, e.Tel, e.Joined))
            .FirstOrDefaultAsync(ct);

        return new GetEmployeeByNameResult(employee);
    }
}
