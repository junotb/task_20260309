using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using task_20260309.Api;
using task_20260309.Application.Employee.Parsers;
using task_20260309.Application.Employee.Services;
using task_20260309.Application.Employee.Validators;
using task_20260309.Domain.Repositories;
using task_20260309.Infrastructure.Data;

// logs 폴더 자동 생성 (실행 디렉터리 기준)
var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logsDir);

var builder = WebApplication.CreateBuilder(args);

// Serilog: 콘솔 출력 + CompactJsonFormatter (Seq 등 분석기 업로드용), 매일 Rolling
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        new CompactJsonFormatter(),
        path: Path.Combine(logsDir, "log-.json"),
        rollingInterval: RollingInterval.Day));

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// Repository
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// CQRS Handlers
builder.Services.AddScoped<task_20260309.Application.Employee.Queries.GetEmployeeListQueryHandler>();
builder.Services.AddScoped<task_20260309.Application.Employee.Queries.GetEmployeeByNameQueryHandler>();

// Producer-Consumer Channel
builder.Services.AddSingleton<EmployeeImportChannel>();
builder.Services.AddHostedService<EmployeeImportBackgroundService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<EmployeeImportDtoValidator>();
builder.Services.AddScoped<IEmployeeParser, EmployeeImportParser>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "task_20260309.xml");
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// DB 마이그레이션 (개발용)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapEmployeeEndpoints();
app.MapFallbackToFile("index.html");

try
{
    Log.Information("애플리케이션 시작");
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
