using Microsoft.AspNetCore.Diagnostics;
using TechnicalTest.API.DTOs;

namespace TechnicalTest.API;

public class ExceptionHandlerMiddleware : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.Clear();
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(new ApiResponse<object?>(false, null, "An unknown error has occurred"), cancellationToken: cancellationToken);

        return true;
    }
}