using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace EAPlaymateGroup.Common;

public sealed class ApiProblemDetailsResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(
        ResultExecutingContext context,
        ResultExecutionDelegate next)
    {
        var status = (context.Result as IStatusCodeActionResult)?.StatusCode;
        if (status is >= 400 && context.Result is not FileResult)
        {
            if (context.Result is ObjectResult { Value: ProblemDetails })
            {
                await next();
                return;
            }

            string? code = null;
            string? detail = null;
            IDictionary<string, string[]>? errors = null;

            if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.Value is ApiErrorDto apiError)
                {
                    code = apiError.Code;
                    detail = apiError.Message;
                    errors = apiError.Errors;
                }
                else if (objectResult.Value is not null)
                {
                    detail = objectResult.Value.GetType()
                        .GetProperty("message", System.Reflection.BindingFlags.IgnoreCase |
                                                System.Reflection.BindingFlags.Public |
                                                System.Reflection.BindingFlags.Instance)
                        ?.GetValue(objectResult.Value)?.ToString();
                }
            }

            context.Result = new ObjectResult(ApiProblemDetails.Create(
                context.HttpContext,
                status.Value,
                code,
                detail,
                errors))
            {
                StatusCode = status,
                ContentTypes = { "application/problem+json" }
            };
        }

        await next();
    }
}
