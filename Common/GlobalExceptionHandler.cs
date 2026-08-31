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
        if (exception is BadHttpRequestException badRequest &&
            badRequest.StatusCode < StatusCodes.Status500InternalServerError)
        {
            logger.LogWarning(
                exception,
                "Bad request rejected. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            if (!httpContext.Response.HasStarted)
            {
                await ApiProblemDetails.WriteAsync(
                    httpContext,
                    badRequest.StatusCode,
                    detail: badRequest.Message,
                    cancellationToken: cancellationToken);
            }

            return true;
        }

        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(
                "Request aborted by client. TraceId: {TraceId}",
                httpContext.TraceIdentifier);
            return true;
        }

        var status = exception is BadHttpRequestException badRequestException
            ? badRequestException.StatusCode
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
