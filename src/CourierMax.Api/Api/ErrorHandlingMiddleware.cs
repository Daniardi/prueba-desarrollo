using System.Text.Json;
using CourierMax.Api.Services;

namespace CourierMax.Api.Api;

public sealed class ErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Handled business error: {Message}", ex.Message);
            await WriteProblem(context, ex.StatusCode, ex.Message, ex.Detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error");
            await WriteProblem(
                context,
                StatusCodes.Status500InternalServerError,
                "Ocurrio un error inesperado.");
        }
    }

    private static async Task WriteProblem(HttpContext context, int statusCode, string error, string? detail = null)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ProblemResponse(error, detail),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
