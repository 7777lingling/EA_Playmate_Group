using Microsoft.AspNetCore.Mvc;

namespace EAPlaymateGroup.Common;

public sealed class ApiErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class ApiErrors
{
    public static BadRequestObjectResult BadRequest(string code, string message)
    {
        return new BadRequestObjectResult(new ApiErrorDto
        {
            Code = code,
            Message = message
        });
    }

    public static BadRequestObjectResult Validation(Dictionary<string, string[]> errors)
    {
        return new BadRequestObjectResult(new ApiErrorDto
        {
            Code = "validation_error",
            Message = "Validation failed.",
            Errors = errors
        });
    }
}
