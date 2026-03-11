using System.Threading.Channels;
using task_20260309.Application.Employee.Commands;

namespace task_20260309.Application.Employee.Services;

/// <summary>
/// Producer-Consumer 채널. Bounded(100), FullMode=Wait.
/// 부수 효과: 채널 가득 찼을 때 WriteAsync는 Consumer가 읽을 때까지 대기. 요청 블로킹됨.
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
