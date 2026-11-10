using System.Data.Common;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected. Nobody is listening for a response.
            context.Response.StatusCode = 499;
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, MapStatus(ex), ex.Code, ex.Message);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Database error while handling {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE",
                "The service is temporarily unavailable. Please try again in a moment.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error while handling {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR",
                "Something went wrong on our side. Please try again.");
        }
    }

    private static int MapStatus(DomainException ex) => ex switch
    {
        ValidationException => StatusCodes.Status400BadRequest,
        NotHoldOwnerException => StatusCodes.Status403Forbidden,
        ClassNotFoundException or HoldNotFoundException => StatusCodes.Status404NotFound,
        NoSeatsAvailableException
            or DuplicateActiveHoldException
            or HoldExpiredException
            or InvalidHoldStateException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };

    private async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning("Cannot write error response, it has already started.");
            return;
        }

        // Deliberately no Response.Clear(): that would also remove the CORS headers.
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ErrorResponse(code, message));
    }
}