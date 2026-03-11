using Microsoft.EntityFrameworkCore;
using task_20260309.Domain.Entities;
using task_20260309.Domain.Repositories;

namespace task_20260309.Infrastructure.Data;

/// <summary>
/// IEmployeeRepository의 EF Core 구현체.
/// </summary>
public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Employee> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var queryable = _context.Employees.AsNoTracking().OrderBy(e => e.Id);
        var totalCount = await queryable.CountAsync(ct);
        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Employee?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Name == name, ct);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        var key = email.Trim().ToLowerInvariant();
        return await _context.Employees.AnyAsync(e => e.Email == key, ct);
    }

    public void Add(Employee employee)
    {
        _context.Employees.Add(employee);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
