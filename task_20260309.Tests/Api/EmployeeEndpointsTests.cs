using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using task_20260309;
using task_20260309.Infrastructure.Persistence;
using Xunit;

namespace task_20260309.Tests.Api;

/// <summary>
/// EmployeeEndpoints API 기능 테스트.
/// WebApplicationFactory로 실제 HTTP 요청 ~ 응답 파이프라인 검증.
/// </summary>
public class EmployeeEndpointsTests : IClassFixture<EmployeeWebApplicationFactory>
{
    private readonly EmployeeWebApplicationFactory _factory;

    public EmployeeEndpointsTests(EmployeeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_employee_page1_페이징된_JSON_응답()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/employee?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<EmployeeListJson>();
        json.Should().NotBeNull();
        json!.Items.Should().NotBeNull();
        json.TotalCount.Should().BeGreaterThanOrEqualTo(0);
        json.Page.Should().Be(1);
        json.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Post_employee_유효한_데이터_시_201_Created()
    {
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("name,email,tel,joined\n테스트,test@example.com,010-1234-5678,2024-03-01"), "rawData");

        var response = await client.PostAsync("/api/employee", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Post_employee_ContentType_없으면_400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/employee", new StringContent("{}"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_employee_페이징_파라미터_기본값_적용()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/employee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<EmployeeListJson>();
        json.Should().NotBeNull();
        json!.Page.Should().Be(1);
        json.PageSize.Should().Be(10);
    }

    private sealed class EmployeeListJson
    {
        public List<EmployeeItemJson>? Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class EmployeeItemJson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public DateTime Joined { get; set; }
    }
}

/// <summary>
/// In-Memory DB를 사용하는 테스트용 WebApplicationFactory.
/// </summary>
public class EmployeeWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
        });

        builder.UseEnvironment("Development");
    }
}
