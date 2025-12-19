using System.Net;
using System.Text.Json;
using Serilog;

namespace AuthApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<ExceptionMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault() ?? "N/A";

            _logger.Error(ex,
                "💥 Unhandled exception while processing {Method} {Path} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                success = false,
                message = "An unexpected error occurred.",
                correlationId = correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
