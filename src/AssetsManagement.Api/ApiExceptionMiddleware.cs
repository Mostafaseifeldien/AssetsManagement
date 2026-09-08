using AssetsManagement.Application;
using AssetsManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace AssetsManagement.Api;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, message, code) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, exception.Message, "not_found"),
                ConflictException => (StatusCodes.Status409Conflict, exception.Message, "conflict"),
                DomainRuleException => (StatusCodes.Status422UnprocessableEntity, exception.Message, "business_rule"),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "The record was changed by another user.", "concurrency"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", "unexpected")
            };
            if (status >= 500)
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Request rejected with {Status} ({Code}) for {Method} {Path}: {Message}",
                    status, code, context.Request.Method, context.Request.Path, message);
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                new ApiResponse<object>(false, message, null, [new ApiError(null, message, code)]),
                context.RequestAborted);
        }
    }
}
