namespace Prosepo.Webhooks.Application.Common;

public sealed class ApiSuccessResponse<T>
{
    public bool Success { get; init; } = true;
    public T? Data { get; init; }
}

public sealed class ApiErrorResponse
{
    public bool Success { get; init; } = false;
    public string Error { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int StatusCode { get; init; }
}