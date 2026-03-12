using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using task_20260309.Application.Employee.Parsers;

namespace task_20260309.Api;

/// <summary>
/// 전역 예외 처리기. 엔드포인트/핸들러에서 미처 잡지 못한 예외를 가로채
/// 정제된 JSON 응답과 로그를 남깁니다. 파싱 실패 = 400, 그 외 = 500.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = MapException(exception);
        var traceId = httpContext.TraceIdentifier;

        logger.LogError(exception, "처리되지 않은 예외 발생, Path={Path}, TraceId={TraceId}, StatusCode={StatusCode}",
            httpContext.Request.Path, traceId, (int)statusCode);

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = statusCode == HttpStatusCode.BadRequest ? "Bad Request" : "Internal Server Error",
            status = (int)statusCode,
            detail = message,
            traceId
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonOptions),
            cancellationToken);

        return true; // 예외 처리 완료, 기본 동작 스킵
    }

    private static (HttpStatusCode statusCode, string message) MapException(Exception exception)
    {
        if (exception is ParseException parseEx)
            return (HttpStatusCode.BadRequest, parseEx.Message);

        if (exception is Microsoft.AspNetCore.Http.BadHttpRequestException badHttp)
            return ((HttpStatusCode)badHttp.StatusCode, badHttp.Message);

        if (exception is ArgumentException or ArgumentNullException)
            return (HttpStatusCode.BadRequest, exception.Message);

        if (exception is OperationCanceledException)
            return (HttpStatusCode.BadRequest, "요청이 취소되었습니다.");

        // 500: 상세 메시지는 로그에만, 클라이언트에는 일반화된 메시지
        return (HttpStatusCode.InternalServerError,
            "서버 내부 오류가 발생했습니다. 잠시 후 다시 시도해 주세요.");
    }
}
