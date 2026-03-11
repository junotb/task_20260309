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
        _logger.LogInformation("직원 Import Consumer 시작, 채널 대기 중");
        await foreach (var command in _channel.ReadAllAsync(stoppingToken))
        {
            var batchId = Guid.NewGuid();
            try
            {
                _logger.LogInformation(
                    "Command 시작, CommandName={CommandName}, BatchId={BatchId}, EmployeeCount={EmployeeCount}",
                    nameof(AddEmployeesCommand), batchId, command.Employees.Count);
                _logger.LogInformation(
                    "파싱 성공(배치 수신), BatchId={BatchId}, EmployeeCount={EmployeeCount}",
                    batchId, command.Employees.Count);
                await ProcessCommandAsync(command, batchId, stoppingToken);
                _logger.LogInformation(
                    "Command 완료, CommandName={CommandName}, BatchId={BatchId}",
                    nameof(AddEmployeesCommand), batchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Command 처리 실패, CommandName={CommandName}, BatchId={BatchId}, EmployeeCount={EmployeeCount}",
                    nameof(AddEmployeesCommand), batchId, command.Employees.Count);
            }
        }
    }

    private async Task ProcessCommandAsync(AddEmployeesCommand command, Guid batchId, CancellationToken ct)
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

        var addedCount = 0;
        var skippedCount = 0;
        foreach (var emp in entities)
        {
            var exists = await db.Employees.AnyAsync(x => x.Email == emp.Email, ct);
            if (exists)
            {
                _logger.LogWarning(
                    "DB 저장 건너뜀(이메일 중복), BatchId={BatchId}, EmployeeEmail={EmployeeEmail}",
                    batchId, emp.Email);
                skippedCount++;
                continue;
            }
            db.Employees.Add(emp);
            _logger.LogDebug(
                "DB 저장 대기, BatchId={BatchId}, EmployeeEmail={EmployeeEmail}",
                batchId, emp.Email);
            addedCount++;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation(
            "DB 저장 완료, BatchId={BatchId}, AddedCount={AddedCount}, SkippedCount={SkippedCount}, TotalCount={TotalCount}",
            batchId, addedCount, skippedCount, entities.Count);
    }
}
