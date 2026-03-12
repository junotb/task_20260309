using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Services;
using Xunit;

namespace task_20260309.Tests.Application.Employee.Services;

/// <summary>
/// EmployeeImportChannel 통합 테스트.
/// Channel에 데이터를 썼을 때 유실 없이 읽히는지 (Producer-Consumer) 확인.
/// </summary>
public class EmployeeImportChannelTests
{
    [Fact]
    public async Task WriteAsync_후_ReadAllAsync_데이터_유실_없음()
    {
        var logger = new Mock<ILogger<EmployeeImportChannel>>();
        var channel = new EmployeeImportChannel(logger.Object);

        var cmd1 = new AddEmployeesCommand([
            new EmployeeImportDto("홍길동", "hong@example.com", "010-1111", DateTime.UtcNow.Date)
        ]);
        var cmd2 = new AddEmployeesCommand([
            new EmployeeImportDto("김철수", "kim@example.com", "010-2222", DateTime.UtcNow.Date)
        ]);

        var list = new List<AddEmployeesCommand>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var readTask = Task.Run(async () =>
        {
            await foreach (var cmd in channel.ReadAllAsync(cts.Token))
            {
                list.Add(cmd);
                if (list.Count >= 2) { cts.Cancel(); break; }
            }
        });

        await channel.WriteAsync(cmd1);
        await channel.WriteAsync(cmd2);

        try { await readTask; } catch (OperationCanceledException) { }

        list.Should().HaveCount(2);
        list[0].Employees[0].Name.Should().Be("홍길동");
        list[1].Employees[0].Name.Should().Be("김철수");
    }

    [Fact]
    public async Task WriteAsync_한_건_쓰고_읽기()
    {
        var logger = new Mock<ILogger<EmployeeImportChannel>>();
        var channel = new EmployeeImportChannel(logger.Object);

        var cmd = new AddEmployeesCommand([
            new EmployeeImportDto("Single", "single@example.com", "010-9999", DateTime.UtcNow.Date)
        ]);

        AddEmployeesCommand? read = null;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var readTask = Task.Run(async () =>
        {
            await foreach (var c in channel.ReadAllAsync(cts.Token))
            {
                read = c;
                break;
            }
        });

        await channel.WriteAsync(cmd);
        try { await readTask; } catch (OperationCanceledException) { }

        read.Should().NotBeNull();
        read!.Employees.Should().HaveCount(1);
        read.Employees[0].Email.Should().Be("single@example.com");
    }
}
