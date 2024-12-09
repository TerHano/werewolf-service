using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WerewolfParty_Server.DTO;

namespace WerewolfParty_Server.Exceptions;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);


        var response = new APIResponse()
        {
            Success = false,
            ErrorMessages = new List<string>([exception.Message])
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response
            .WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}