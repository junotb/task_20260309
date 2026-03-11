using FluentValidation;
using Microsoft.EntityFrameworkCore;
using task_20260309.Api;
using task_20260309.Application.Services;
using task_20260309.Application.Validators;
using task_20260309.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

// CQRS Handlers
builder.Services.AddScoped<task_20260309.Application.CQRS.Queries.GetEmployeeListQueryHandler>();
builder.Services.AddScoped<task_20260309.Application.CQRS.Queries.GetEmployeeByNameQueryHandler>();

// Producer-Consumer Channel
builder.Services.AddSingleton<EmployeeImportChannel>();
builder.Services.AddHostedService<EmployeeImportBackgroundService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<EmployeeImportDtoValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB 마이그레이션 (개발용)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapEmployeeEndpoints();

app.Run();
