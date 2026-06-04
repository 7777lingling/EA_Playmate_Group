namespace EAPlaymateGroup.Services;

public sealed class ServiceResult
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static ServiceResult Success()
    {
        return new ServiceResult { Succeeded = true };
    }

    public static ServiceResult Missing()
    {
        return new ServiceResult { NotFound = true };
    }

    public static ServiceResult Failure(string code, string message)
    {
        return new ServiceResult
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    public static ServiceResult Validation(Dictionary<string, string[]> errors)
    {
        return new ServiceResult
        {
            ValidationErrors = errors
        };
    }
}

public sealed class ServiceResult<T>
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string[]>? ValidationErrors { get; init; }

    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>
        {
            Succeeded = true,
            Value = value
        };
    }

    public static ServiceResult<T> Missing()
    {
        return new ServiceResult<T> { NotFound = true };
    }

    public static ServiceResult<T> Failure(string code, string message)
    {
        return new ServiceResult<T>
        {
            ErrorCode = code,
            ErrorMessage = message
        };
    }

    public static ServiceResult<T> Validation(Dictionary<string, string[]> errors)
    {
        return new ServiceResult<T>
        {
            ValidationErrors = errors
        };
    }
}
