using task_20260309.Application.Employee.Commands;
using task_20260309.Domain.Repositories;
using EmployeeEntity = task_20260309.Domain.Entities.Employee;

namespace task_20260309.Application.Employee.Services;

/// <summary>
/// 채널 Consumer. API가 채널에 쓴 Command를 비동기로 DB에 저장.
/// 부수 효과: 201 반환 후 실제 저장은 비동기. 저장 실패해도 클라이언트에게 재통보 안 함.
/// 실패 시: 예외 로깅 후 해당 배치만 스킵, 채널은 계속 수신. 재시도/보상 트랜잭션 없음.
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
        var repository = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var addedCount = 0;
        var skippedCount = 0;

        foreach (var dto in command.Employees)
        {
            // DB에는 소문자로 저장. 동시 요청 시 race로 중복 삽입 가능성 있음 → UNIQUE 제약으로 방지.
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await repository.ExistsByEmailAsync(email, ct))
            {
                _logger.LogWarning(
                    "DB 저장 건너뜀(이메일 중복), BatchId={BatchId}, EmployeeEmail={EmployeeEmail}",
                    batchId, email);
                skippedCount++;
                continue;
            }

            var entity = new EmployeeEntity
            {
                Name = dto.Name.Trim(),
                Email = email,
                Tel = dto.Tel.Trim(),
                Joined = dto.Joined
            };
            repository.Add(entity);
            _logger.LogDebug(
                "DB 저장 대기, BatchId={BatchId}, EmployeeEmail={EmployeeEmail}",
                batchId, email);
            addedCount++;
        }

        await repository.SaveChangesAsync(ct);
        _logger.LogInformation(
            "DB 저장 완료, BatchId={BatchId}, AddedCount={AddedCount}, SkippedCount={SkippedCount}, TotalCount={TotalCount}",
            batchId, addedCount, skippedCount, command.Employees.Count);
    }
}
