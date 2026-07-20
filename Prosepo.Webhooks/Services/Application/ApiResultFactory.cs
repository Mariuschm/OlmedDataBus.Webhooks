using Microsoft.AspNetCore.Mvc;
using Prosepo.Webhooks.Application.Common;

namespace Prosepo.Webhooks.Services.Application;

public static class ApiResultFactory
{
    public static IActionResult Error(int statusCode, string error, string message)
    {
        return new ObjectResult(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            StatusCode = statusCode
        })
        {
            StatusCode = statusCode
        };
    }

    public static IActionResult ValidationError(string error, string message)
    {
        return new BadRequestObjectResult(new ApiErrorResponse
        {
            Error = error,
            Message = message,
            StatusCode = StatusCodes.Status400BadRequest
        });
    }
}