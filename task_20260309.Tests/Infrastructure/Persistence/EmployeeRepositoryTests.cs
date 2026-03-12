using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using task_20260309.Domain.Entities;
using task_20260309.Domain.Repositories;
using task_20260309.Domain.ValueObjects;
using task_20260309.Infrastructure.Persistence;
using Xunit;

namespace task_20260309.Tests.Infrastructure.Persistence;

/// <summary>
/// EmployeeRepository 통합 테스트.
/// 실제 AppDbContext(In-Memory)를 사용하여 유니크 인덱스(Email) 작동, 데이터 저장 검증.
/// </summary>
public class EmployeeRepositoryTests
{
    private static (AppDbContext Context, IEmployeeRepository Repository) CreateFresh()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, new EmployeeRepository(context));
    }

    [Fact]
    public async Task Add_및_SaveChanges_데이터_저장_성공()
    {
        var (context, repository) = CreateFresh();
        var employee = new Employee
        {
            Name = "홍길동",
            Email = Email.Create("hong@example.com"),
            Tel = "010-1234-5678",
            Joined = new DateTime(2024, 1, 15)
        };

        repository.Add(employee);
        await repository.SaveChangesAsync();

        employee.Id.Should().BeGreaterThan(0);
        var (items, total) = await repository.GetPagedAsync(1, 10);
        items.Should().HaveCount(1);
        items[0].Name.Should().Be("홍길동");
        items[0].Email.Normalized.Should().Be("hong@example.com");
    }

    [Fact]
    public async Task GetPagedAsync_페이징_정상()
    {
        var (_, repository) = CreateFresh();
        for (int i = 0; i < 5; i++)
        {
            repository.Add(new Employee
            {
                Name = $"직원{i}",
                Email = Email.Create($"user{i}@example.com"),
                Tel = "010-1234-5678",
                Joined = DateTime.UtcNow.Date
            });
        }
        await repository.SaveChangesAsync();

        var (page1, total) = await repository.GetPagedAsync(1, 2);
        page1.Should().HaveCount(2);
        total.Should().Be(5);

        var (page2, _) = await repository.GetPagedAsync(2, 2);
        page2.Should().HaveCount(2);

        var (page3, _) = await repository.GetPagedAsync(3, 2);
        page3.Should().HaveCount(1);
    }

    [Fact]
    public async Task 동일_이메일_저장_시_유니크_인덱스_위반_예외()
    {
        // In-Memory DB는 유니크 제약을 적용하지 않음. SQLite In-Memory 사용.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new EmployeeRepository(context);

        var email = Email.Create("duplicate@example.com");
        repository.Add(new Employee
        {
            Name = "First",
            Email = email,
            Tel = "010-1111",
            Joined = DateTime.UtcNow.Date
        });
        await repository.SaveChangesAsync();

        repository.Add(new Employee
        {
            Name = "Second",
            Email = email,
            Tel = "010-2222",
            Joined = DateTime.UtcNow.Date
        });

        var act = async () => await repository.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ExistsByEmailAsync_존재하면_true()
    {
        var (_, repository) = CreateFresh();
        var email = Email.Create("exists@example.com");
        repository.Add(new Employee
        {
            Name = "Exists",
            Email = email,
            Tel = "010-1234",
            Joined = DateTime.UtcNow.Date
        });
        await repository.SaveChangesAsync();

        var exists = await repository.ExistsByEmailAsync(email);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByEmailAsync_없으면_false()
    {
        var (_, repository) = CreateFresh();
        var email = Email.Create("nonexistent@example.com");
        var exists = await repository.ExistsByEmailAsync(email);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetByNameAsync_이름으로_조회()
    {
        var (_, repository) = CreateFresh();
        repository.Add(new Employee
        {
            Name = "홍길동",
            Email = Email.Create("hong@example.com"),
            Tel = "010-1234",
            Joined = DateTime.UtcNow.Date
        });
        await repository.SaveChangesAsync();

        var found = await repository.GetByNameAsync("홍길동");
        found.Should().NotBeNull();
        found!.Email.Normalized.Should().Be("hong@example.com");
    }
}
