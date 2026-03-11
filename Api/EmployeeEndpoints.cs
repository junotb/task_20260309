using FluentValidation;
using task_20260309.Application.CQRS.Commands;
using task_20260309.Application.CQRS.Queries;
using task_20260309.Application.Parsers;
using task_20260309.Application.Services;

namespace task_20260309.Api;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee");

        group.MapGet("/", GetEmployeeList)
            .WithName("GetEmployeeList")
            .Produces<GetEmployeeListResponse>(200, "application/json");

        group.MapGet("/{name}", GetEmployeeByName)
            .WithName("GetEmployeeByName")
            .Produces<EmployeeDetailResponse>(200, "application/json")
            .Produces(404);

        group.MapPost("/", AddEmployees)
            .WithName("AddEmployees")
            .Produces(201)
            .Produces(400)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> GetEmployeeList(
        [AsParameters] GetEmployeeListRequest request,
        GetEmployeeListQueryHandler handler,
        CancellationToken ct)
    {
        var query = new GetEmployeeListQuery(request.Page, request.PageSize);
        var result = await handler.HandleAsync(query, ct);
        var response = new GetEmployeeListResponse(
            result.Items.Select(e => new EmployeeListItemResponse(e.Id, e.Name, e.Email, e.Tel, e.Joined)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetEmployeeByName(
        string name,
        GetEmployeeByNameQueryHandler handler,
        CancellationToken ct)
    {
        var query = new GetEmployeeByNameQuery(name);
        var result = await handler.HandleAsync(query, ct);
        if (result.Employee is null)
            return Results.NotFound();
        var dto = result.Employee;
        return Results.Ok(new EmployeeDetailResponse(dto.Id, dto.Name, dto.Email, dto.Tel, dto.Joined));
    }

    private static async Task<IResult> AddEmployees(
        HttpContext context,
        EmployeeImportChannel channel,
        IValidator<AddEmployeesCommand> validator,
        CancellationToken ct)
    {
        AddEmployeesCommand command;
        var contentType = context.Request.ContentType ?? "";

        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            var csvText = form["csvText"].FirstOrDefault();

            if (file is not null)
            {
                await using var stream = file.OpenReadStream();
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                command = ext == ".json"
                    ? await EmployeeImportParser.ParseJsonAsync(stream, ct)
                    : await EmployeeImportParser.ParseCsvAsync(stream, ct);
            }
            else if (!string.IsNullOrWhiteSpace(csvText))
            {
                command = EmployeeImportParser.ParseCsvFromString(csvText);
            }
            else
            {
                return Results.BadRequest(new { error = "file 또는 csvText가 필요합니다." });
            }
        }
        else if (contentType.Contains("application/json"))
        {
            await using var stream = context.Request.Body;
            command = await EmployeeImportParser.ParseJsonAsync(stream, ct);
        }
        else if (contentType.Contains("text/csv"))
        {
            await using var stream = context.Request.Body;
            command = await EmployeeImportParser.ParseCsvAsync(stream, ct);
        }
        else
        {
            return Results.BadRequest(new { error = "Content-Type: application/json, text/csv 또는 multipart/form-data (file, csvText) 필요" });
        }

        var validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return Results.BadRequest(new { errors });
        }

        await channel.WriteAsync(command, ct);
        return Results.Created("/api/employee", (object?)null);
    }
}

public record GetEmployeeListRequest(int Page = 1, int PageSize = 10);

public record GetEmployeeListResponse(
    IReadOnlyList<EmployeeListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record EmployeeListItemResponse(int Id, string Name, string Email, string Tel, DateTime Joined);

public record EmployeeDetailResponse(int Id, string Name, string Email, string Tel, DateTime Joined);
