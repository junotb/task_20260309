using System.Threading.Channels;
using task_20260309.Application.CQRS.Commands;

namespace task_20260309.Application.Services;

/// <summary>
/// Producer-Consumer 패턴용 채널.
/// API에서 직원 데이터를 채널에 쓰고, BackgroundService에서 읽어 DB에 저장.
/// </summary>
public class EmployeeImportChannel
{
    private readonly Channel<AddEmployeesCommand> _channel = Channel.CreateBounded<AddEmployeesCommand>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    private readonly ILogger<EmployeeImportChannel> _logger;

    public EmployeeImportChannel(ILogger<EmployeeImportChannel> logger)
    {
        _logger = logger;
    }

    public ValueTask WriteAsync(AddEmployeesCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("Import 채널에 쓰기, EmployeeCount={EmployeeCount}", command.Employees.Count);
        return _channel.Writer.WriteAsync(command, ct);
    }

    public IAsyncEnumerable<AddEmployeesCommand> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
