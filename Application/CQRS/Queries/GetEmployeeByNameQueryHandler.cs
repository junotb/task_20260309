using Microsoft.EntityFrameworkCore;
using task_20260309.Infrastructure.Data;

namespace task_20260309.Application.CQRS.Queries;

public class GetEmployeeByNameQueryHandler
{
    private readonly AppDbContext _db;
    private readonly ILogger<GetEmployeeByNameQueryHandler> _logger;

    public GetEmployeeByNameQueryHandler(AppDbContext db, ILogger<GetEmployeeByNameQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GetEmployeeByNameResult> HandleAsync(GetEmployeeByNameQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Query 시작, QueryName={QueryName}, Name={Name}",
            nameof(GetEmployeeByNameQuery), query.Name);
        var employee = await _db.Employees
            .AsNoTracking()
            .Where(e => e.Name == query.Name)
            .Select(e => new EmployeeDetailDto(e.Id, e.Name, e.Email, e.Tel, e.Joined))
            .FirstOrDefaultAsync(ct);

        _logger.LogInformation(
            "Query 완료, QueryName={QueryName}, Name={Name}, Found={Found}, EmployeeId={EmployeeId}",
            nameof(GetEmployeeByNameQuery), query.Name, employee is not null, employee?.Id);
        return new GetEmployeeByNameResult(employee);
    }
}
