using Microsoft.AspNetCore.Diagnostics;

namespace EAPlaymateGroup.Common;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception is BadHttpRequestException badRequest
            ? badRequest.StatusCode
            : StatusCodes.Status500InternalServerError;

        logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var detail = status >= 500 && !environment.IsDevelopment()
            ? "伺服器發生未預期錯誤。"
            : exception.Message;

        await ApiProblemDetails.WriteAsync(
            httpContext,
            status,
            detail: detail,
            cancellationToken: cancellationToken);
        return true;
    }
}
