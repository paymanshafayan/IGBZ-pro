using IGBZ.Domain.Shared;
using MongoDB.Driver;

namespace IGBZ.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Domain validation failed", ex.Message);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, "Duplicate key", "A document with the same unique key already exists.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while processing request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://igbz.local/problems/request-failed",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        });
    }
}
