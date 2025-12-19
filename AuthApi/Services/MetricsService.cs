using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Prometheus;

namespace AuthApi.Services
{
    /// <summary>
    /// Aggregates per-route metrics for your custom snapshots AND
    /// exposes a Prometheus counter for success/error/RPS panels.
    /// </summary>
    public sealed class MetricsService
    {
        // ---------- In-memory (your existing per-route metrics) ----------
        private readonly ConcurrentDictionary<string, EndpointMetrics> _byRoute = new();

        public void RecordRequest(string routeTemplate, double durationMs, bool isSuccess)
        {
            var key = NormalizeRoute(routeTemplate);
            var m = _byRoute.GetOrAdd(key, static rt => new EndpointMetrics(rt));
            m.Record(durationMs, isSuccess);
        }

        // Compat: if your middleware calls Record(...)
        public void Record(string routeTemplate, double durationMs, bool isSuccess)
            => RecordRequest(routeTemplate, durationMs, isSuccess);

        public IEnumerable<EndpointMetricsSnapshot> GetAllMetrics()
            => _byRoute.Values.Select(v => v.Snapshot());

        public void Reset()
        {
            _byRoute.Clear();
            // or: foreach (var kv in _byRoute) kv.Value.Clear();
        }

        private static string NormalizeRoute(string? routeTemplate)
            => string.IsNullOrWhiteSpace(routeTemplate) ? "unknown" : routeTemplate;


        // ---------- Prometheus (for Grafana success/error/RPS panels) ----------

        // Exposes: http_requests_total{method,route,status_code}
        private static readonly Counter HttpRequestsTotal = Metrics.CreateCounter(
            "http_requests_total",
            "Total HTTP requests processed",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "route", "status_code" }
            });

        public void IncRequestTotal(string method, string routeTemplate, int statusCode)
        {
            var route = NormalizeRoute(routeTemplate);
            HttpRequestsTotal.WithLabels(method, route, statusCode.ToString()).Inc();
        }

        public void RecordRequestWithStatus(string method, string routeTemplate, int statusCode, double durationMs, bool isSuccess)
        {
            RecordRequest(routeTemplate, durationMs, isSuccess);
            IncRequestTotal(method, routeTemplate, statusCode);
        }
    }
}
