using Microsoft.AspNetCore.Http;

namespace Prosepo.Webhooks.Application.Common;

public sealed class ApplicationResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public ErrorDetails? Error { get; init; }

    public static ApplicationResult<T> Ok(T data, int statusCode = StatusCodes.Status200OK)
    {
        return new ApplicationResult<T>
        {
            Success = true,
            StatusCode = statusCode,
            Data = data
        };
    }

    public static ApplicationResult<T> Fail(int statusCode, string error, string message)
    {
        return new ApplicationResult<T>
        {
            Success = false,
            StatusCode = statusCode,
            Error = new ErrorDetails
            {
                Error = error,
                Message = message
            }
        };
    }
}

public sealed class ErrorDetails
{
    public string Error { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}