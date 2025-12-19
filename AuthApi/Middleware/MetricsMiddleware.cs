using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AuthApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AuthApi.Middleware
{
    public sealed class MetricsMiddleware
    {
        private readonly RequestDelegate _next;
        public MetricsMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context, MetricsService metrics)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // 1️⃣ Skip metrics endpoints (avoid self-scraping noise)
            if (path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // 2️⃣ Prefer route template for consistent grouping
                var endpoint = context.GetEndpoint() as RouteEndpoint;
                var routeTemplate = endpoint?.RoutePattern?.RawText ?? path;

                // 3️⃣ Normalize (lowercase, trim)
                var normalizedRoute = routeTemplate.TrimEnd('/').ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalizedRoute))
                    normalizedRoute = "/";

                // 4️⃣ Separate labels: method + route
                var method = context.Request.Method.ToUpperInvariant();

                // Your in-memory key keeps method+route combined (as before)
                var routeKey = $"{method} {normalizedRoute}";

                // 5️⃣ Success = HTTP 2xx or 3xx
                var statusCode = context.Response.StatusCode;
                var isSuccess = statusCode >= 200 && statusCode < 400;

                // 6️⃣ Record your existing in-memory metrics
                metrics.RecordRequest(routeKey, stopwatch.Elapsed.TotalMilliseconds, isSuccess);

                // 7️⃣ NEW: increment Prometheus counter for Grafana success/error/RPS
                metrics.IncRequestTotal(method, normalizedRoute, statusCode);
            }
        }
    }
}
