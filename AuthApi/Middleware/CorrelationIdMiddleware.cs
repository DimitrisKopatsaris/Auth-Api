using System.Diagnostics;
using Serilog;
using Serilog.Context;

namespace AuthApi.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string CorrelationHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var isMetrics = context.Request.Path.StartsWithSegments("/metrics");

            var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
                                ?? Guid.NewGuid().ToString();

            context.Response.Headers[CorrelationHeader] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("TraceId", Activity.Current?.Id ?? context.TraceIdentifier))
            {
                try
                {
                    // 👇 Only log for non-/metrics requests
                    if (!isMetrics)
                    {
                        Log.Information("➡️ Incoming request: {Method} {Path} | CorrelationId: {CorrelationId} | TraceId: {TraceId}",
                            context.Request.Method, context.Request.Path, correlationId,
                            Activity.Current?.Id ?? context.TraceIdentifier);
                    }

                    await _next(context);

                    if (!isMetrics)
                    {
                        Log.Information("⬅️ Completed request: {Method} {Path} | CorrelationId: {CorrelationId} | StatusCode: {StatusCode}",
                            context.Request.Method, context.Request.Path, correlationId, context.Response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    // Still log errors (even for /metrics) so we don’t hide real problems
                    Log.Error(ex, "❌ Error handling request {Method} {Path} | CorrelationId: {CorrelationId}",
                        context.Request.Method, context.Request.Path, correlationId);
                    throw;
                }
            }
        }
    }
}
