using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Common;

public static class ApiProblemDetails
{
    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string? code = null,
        string? detail = null,
        IDictionary<string, string[]>? errors = null)
    {
        ProblemDetails problem = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);

        problem.Status = status;
        problem.Title = GetTitle(status);
        problem.Type = $"https://httpstatuses.com/{status}";
        problem.Detail = detail;
        problem.Instance = context.Request.Path;
        problem.Extensions["code"] = code ?? GetCode(status);
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }

    public static async Task WriteAsync(
        HttpContext context,
        int status,
        string? code = null,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            Create(context, status, code, detail),
            cancellationToken);
    }

    public static string GetCode(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "bad_request",
        StatusCodes.Status401Unauthorized => "unauthorized",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not_found",
        StatusCodes.Status409Conflict => "conflict",
        StatusCodes.Status422UnprocessableEntity => "unprocessable_entity",
        _ when status >= 500 => "internal_server_error",
        _ => "request_failed"
    };

    private static string GetTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        _ when status >= 500 => "Internal Server Error",
        _ => "Request Failed"
    };
}
