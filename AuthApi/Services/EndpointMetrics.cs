using System;

namespace AuthApi.Services
{
    public sealed class EndpointMetrics
    {
        private readonly object _lock = new();

        private long _requestCount;
        private long _errorCount;

        public string RouteTemplate { get; }
        public LatencyHistogram Histogram { get; } = new();

        public EndpointMetrics(string routeTemplate) => RouteTemplate = routeTemplate;

        public void Record(double durationMs, bool isSuccess)
        {
            Histogram.Observe(durationMs);
            lock (_lock)
            {
                _requestCount++;
                if (!isSuccess) _errorCount++;
            }
        }

        public EndpointMetricsSnapshot Snapshot()
        {
            long req, err;
            lock (_lock)
            {
                req = _requestCount;
                err = _errorCount;
            }

            var hist = Histogram.Snapshot();

            return new EndpointMetricsSnapshot
            {
                RouteTemplate      = RouteTemplate,
                RequestCount       = req,
                ErrorCount         = err,
                AvgResponseTimeMs  = hist.AvgMs,
                P95ResponseTimeMs  = hist.Percentile(0.95),
                P99ResponseTimeMs  = hist.Percentile(0.99),
                SuccessRate        = req == 0 ? 0 : 1.0 - (double)err / req,
                HistogramSnapshot  = hist
            };
        }

        public void Clear()
        {
            lock (_lock)
            {
                _requestCount = 0;
                _errorCount = 0;
            }
            Histogram.Reset();
        }
    }

    public sealed class EndpointMetricsSnapshot
    {
        public string RouteTemplate { get; set; } = "";
        public long   RequestCount { get; set; }
        public long   ErrorCount { get; set; }
        public double AvgResponseTimeMs { get; set; }
        public double P95ResponseTimeMs { get; set; }
        public double P99ResponseTimeMs { get; set; }
        public double SuccessRate { get; set; }

        // Included for Prometheus exposition.
        public HistogramSnapshot HistogramSnapshot { get; set; } = default!;
    }
}
