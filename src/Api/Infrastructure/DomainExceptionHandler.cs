using Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Infrastructure;

/// <summary>
/// Порушення бізнес-правила (DomainException) перетворюється на 409 Conflict у форматі ProblemDetails.
/// Усі інші винятки залишаються 500 без деталей.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Порушено бізнес-правило.",
                Detail = exception.Message
            }
        });
    }
}
