using System.Text;
using Microsoft.OpenApi.Models;
using task_20260309.Application.Employee;
using task_20260309.Application.Employee.Commands;
using task_20260309.Application.Employee.Queries;
using task_20260309.Application.Employee.Services;
using GetEmployeeListRequest = task_20260309.Api.Employee.GetEmployeeListRequest;
using GetEmployeeListResponse = task_20260309.Api.Employee.GetEmployeeListResponse;
using GetEmployeeByNameResponse = task_20260309.Api.Employee.GetEmployeeByNameResponse;
using EmployeeResponse = task_20260309.Api.Employee.EmployeeResponse;
using ImportEmptyResponse = task_20260309.Api.Employee.ImportEmptyResponse;
using ImportSuccessResponse = task_20260309.Api.Employee.ImportSuccessResponse;

namespace task_20260309.Api;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/employee");

        group.MapGet("/", ListEmployees)
            .WithName("ListEmployees")
            .WithSummary("직원 목록 조회")
            .WithDescription("""
                페이지네이션된 직원 목록. Page, PageSize로 조회.
                성공 200: items, totalCount, page, pageSize 반환. Page/PageSize는 1~100 자동 보정.
                """)
            .Produces<GetEmployeeListResponse>(200, "application/json");

        group.MapGet("/{name}", FindEmployeesByName)
            .WithName("FindEmployeesByName")
            .WithSummary("이름으로 직원 조회")
            .WithDescription("""
                이름이 정확히 일치하는 직원 전부. 동명이인 포함, Id 오름차순.
                성공 200: items 배열 반환(0건 이상).
                실패 404: 해당 이름의 직원 없음(items 빈 배열).
                """)
            .Produces<GetEmployeeByNameResponse>(200, "application/json")
            .Produces(404);

        group.MapPost("/", ImportEmployees)
            .WithName("ImportEmployees")
            .WithSummary("직원 일괄 추가")
            .WithDescription("""
                multipart/form-data: file(CSV/JSON) 또는 rawData. 또는 body 직접: text/plain, text/csv, application/json. 둘 다 시 병합·이메일 중복 제거.
                성공 201: 채널 전달 완료. message, imported, skipped, total, errors(있을 때). 실제 DB 저장은 비동기.
                성공 200: file·rawData 둘 다 비었거나, 파싱 결과 유효 직원 0명. received=0 또는 imported=0.
                실패 400: Content-Type 오류 / 파싱 실패(형식 오류) / 검증 실패(전체). errors에 index, email, errors 배열.
                """)
            .WithOpenApi(operation =>
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Description = "파일 업로드 또는 rawData 입력. 둘 다 입력 시 병합·이메일 중복 제거.",
                    Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Properties =
                                {
                                    ["file"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = "CSV 또는 JSON 파일 (.csv, .json)"
                                    },
                                    ["rawData"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "CSV 또는 JSON 텍스트 (직접 입력)"
                                    }
                                }
                            }
                        },
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema
                                {
                                    Type = "object",
                                    Properties =
                                    {
                                        ["name"] = new OpenApiSchema { Type = "string" },
                                        ["email"] = new OpenApiSchema { Type = "string" },
                                        ["tel"] = new OpenApiSchema { Type = "string" },
                                        ["joined"] = new OpenApiSchema { Type = "string", Description = "yyyy-MM-dd" }
                                    }
                                }
                            }
                        },
                        ["text/plain"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "CSV 또는 JSON 텍스트"
                            }
                        },
                        ["text/csv"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "CSV 텍스트 (헤더 포함)"
                            }
                        }
                    }
                };
                return operation;
            })
            .Produces(201)
            .Produces(200)
            .Produces(400)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> ListEmployees(
        [AsParameters] GetEmployeeListRequest request,
        GetEmployeeListQueryHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Api.Employee");
        logger.LogInformation("직원 목록 조회 요청, Page={Page}, PageSize={PageSize}", request.Page, request.PageSize);
        var result = await handler.HandleAsync(new GetEmployeeListQuery(request.Page, request.PageSize), ct);
        logger.LogInformation(
            "직원 목록 조회 완료, TotalCount={TotalCount}, ReturnedCount={ReturnedCount}",
            result.TotalCount, result.Items.Count);
        var response = new GetEmployeeListResponse(
            result.Items.Select(e => new EmployeeResponse(e.Id, e.Name, e.Email, e.Tel, e.Joined)).ToList(),
            result.TotalCount,
            result.Page,
            result.PageSize);
        return Results.Ok(response);
    }

    private static async Task<IResult> FindEmployeesByName(
        string name,
        GetEmployeeByNameQueryHandler handler,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Api.Employee");
        logger.LogInformation("직원 이름 조회 요청, Name={Name}", name);
        var result = await handler.HandleAsync(new GetEmployeeByNameQuery(name), ct);
        if (result.Employees.Count == 0)
        {
            logger.LogWarning("직원을 찾을 수 없음, Name={Name}", name);
            return Results.NotFound();
        }
        logger.LogInformation("직원 조회 완료, Count={Count}, Name={Name}", result.Employees.Count, name);
        var items = result.Employees
            .Select(e => new EmployeeResponse(e.Id, e.Name, e.Email, e.Tel, e.Joined))
            .ToList();
        return Results.Ok(new GetEmployeeByNameResponse(items));
    }

    private const int MaxRawBodyBytes = 1024 * 1024; // 1MB (rawData form 필드와 동일)

    private static readonly HashSet<string> RawBodyContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/csv",
        "application/json"
    };

    private static async Task<IResult> ImportEmployees(
        HttpContext context,
        AddEmployeesCommandHandler handler,
        CancellationToken ct)
    {
        AddEmployeesRequest request;
        var contentType = context.Request.ContentType?.Split(';')[0].Trim();

        if (context.Request.HasFormContentType)
        {
            var (file, rawData) = await ReadImportFormAsync(context, ct);
            Stream? fileStream = null;
            if (file is not null && file.Length > 0)
                fileStream = file.OpenReadStream();
            request = new AddEmployeesRequest(fileStream, file?.FileName, rawData);
        }
        else if (contentType is not null && RawBodyContentTypes.Contains(contentType))
        {
            var rawData = await ReadRawBodyAsync(context, ct);
            if (rawData is null)
                return Results.BadRequest(new { error = "요청 본문이 너무 큽니다. (최대 1MB)" });
            request = new AddEmployeesRequest(null, null, rawData);
        }
        else
        {
            return Results.BadRequest(new { error = "Content-Type: multipart/form-data 또는 text/plain, text/csv, application/json이 필요합니다." });
        }

        var result = await handler.HandleAsync(request, ct);

        return result switch
        {
            AddEmployeesEmptyResponse empty => Results.Ok(new ImportEmptyResponse(
                empty.Message, empty.Hint, empty.Received, empty.Imported)),
            AddEmployeesParseFailedResponse failed => Results.BadRequest(new { error = failed.Message }),
            AddEmployeesValidationFailedResponse validation => Results.BadRequest(new
            {
                message = validation.Message,
                total = validation.Total,
                imported = 0,
                errors = validation.Errors
            }),
            AddEmployeesSuccessResponse success => Results.Created("/api/employee", new ImportSuccessResponse(
                "직원 등록이 완료되었습니다.",
                success.Imported,
                success.Skipped,
                success.Total,
                success.Errors)),
            _ => Results.BadRequest(new { error = "처리할 수 없습니다." })
        };
    }

    private static async Task<(IFormFile? file, string? rawData)> ReadImportFormAsync(HttpContext context, CancellationToken ct)
    {
        var form = await context.Request.ReadFormAsync(ct);
        var file = form.Files["file"];
        var rawData = form["rawData"].FirstOrDefault();
        return (file, rawData);
    }

    private static async Task<string?> ReadRawBodyAsync(HttpContext context, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();
        int total = 0;
        int read;
        while ((read = await context.Request.Body.ReadAsync(buffer, ct)) > 0)
        {
            total += read;
            if (total > MaxRawBodyBytes)
                return null;
            ms.Write(buffer, 0, read);
        }
        ms.Position = 0;
        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
