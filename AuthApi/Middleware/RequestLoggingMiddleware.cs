using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace AuthApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<RequestLoggingMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 🧠 Step 1: Skip Prometheus /metrics requests to avoid log spam
            if (context.Request.Path.StartsWithSegments("/metrics"))
            {
                await _next(context); // still let Prometheus scrape metrics
                return;               // exit early — no logging
            }

            var sw = Stopwatch.StartNew();
            int statusCode = 200;

            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                statusCode = 500;
                _logger.Error(ex, "Unhandled exception during request");
                throw; // rethrow so ExceptionMiddleware can handle it
            }
            finally
            {
                sw.Stop();

                var durationMs = sw.Elapsed.TotalMilliseconds;
                var method = context.Request.Method;
                var path = context.Request.Path.Value ?? "unknown";
                var endpoint = $"{method} {path}".TrimEnd('/');

                // Build structured log object
                var logData = new
                {
                    Endpoint = endpoint.ToLowerInvariant(),
                    StatusCode = statusCode,
                    DurationMs = Math.Round(durationMs, 2),
                    Success = statusCode < 400,
                    User = context.User?.Identity?.Name ?? "anonymous"
                };

                // Log at appropriate level
                if (statusCode >= 500)
                    _logger.Error("Request failed {@RequestInfo}", logData);
                else if (statusCode >= 400)
                    _logger.Warning("Client error {@RequestInfo}", logData);
                else
                    _logger.Information("Request completed {@RequestInfo}", logData);
            }
        }
    }
}
