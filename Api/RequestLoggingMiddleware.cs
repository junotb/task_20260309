namespace task_20260309.Api;

/// <summary>
/// 모든 HTTP 요청의 진입점과 종료 시점에 구조적 로그를 남깁니다.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        _logger.LogInformation(
            "HTTP 요청 진입, Method={Method}, Path={Path}",
            method, path);

        try
        {
            await _next(context);
            _logger.LogInformation(
                "HTTP 요청 종료, Method={Method}, Path={Path}, StatusCode={StatusCode}",
                method, path, context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HTTP 요청 처리 중 예외 발생, Method={Method}, Path={Path}",
                method, path);
            throw;
        }
    }
}
