using Microsoft.EntityFrameworkCore;
using task_20260309.Application.CQRS.Commands;
using task_20260309.Domain.Entities;
using task_20260309.Infrastructure.Data;

namespace task_20260309.Application.Services;

/// <summary>
/// 채널에서 직원 추가 요청을 읽어 DB에 비동기 저장.
/// Producer-Consumer 패턴의 Consumer 역할.
/// </summary>
public class EmployeeImportBackgroundService : BackgroundService
{
    private readonly EmployeeImportChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmployeeImportBackgroundService> _logger;

    public EmployeeImportBackgroundService(
        EmployeeImportChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<EmployeeImportBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var command in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessCommandAsync(command, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee import 처리 중 오류 발생");
            }
        }
    }

    private async Task ProcessCommandAsync(AddEmployeesCommand command, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var entities = command.Employees
            .Select(e => new Employee
            {
                Name = e.Name.Trim(),
                Email = e.Email.Trim().ToLowerInvariant(),
                Tel = e.Tel.Trim(),
                Joined = e.Joined
            })
            .ToList();

        foreach (var emp in entities)
        {
            var exists = await db.Employees.AnyAsync(x => x.Email == emp.Email, ct);
            if (exists)
            {
                _logger.LogWarning("이메일 중복 건너뜀: {Email}", emp.Email);
                continue;
            }
            db.Employees.Add(emp);
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("직원 {Count}명 추가 완료", entities.Count);
    }
}
