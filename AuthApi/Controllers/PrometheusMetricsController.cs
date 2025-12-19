using System.Globalization;
using System.Text;
using AuthApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("metrics/prometheus")]
    public class PrometheusMetricsController : ControllerBase
    {
        private readonly MetricsService _metrics;

        public PrometheusMetricsController(MetricsService metrics)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        [HttpGet]
        [Produces("text/plain")]
        public IActionResult Get()
        {
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            // Prometheus headers
            sb.AppendLine("# HELP http_request_duration_ms Request duration in milliseconds.");
            sb.AppendLine("# TYPE http_request_duration_ms histogram");

            var snapshots = _metrics.GetAllMetrics();

            foreach (var m in snapshots)
            {
                // Extract method + route from keys like "GET /api/users/{id}"
                var raw = m.RouteTemplate ?? "unknown";
                string method = "UNKNOWN";
                string route  = raw;

                int spaceIndex = raw.IndexOf(' ');
        if (spaceIndex > 0)
        {
          var maybeMethod = raw[..spaceIndex];
          if (maybeMethod is "GET" or "POST" or "PUT" or "DELETE" or "PATCH" or "OPTIONS" or "HEAD")
          {
            method = maybeMethod;
            route = raw[(spaceIndex + 1)..];
          }
        }

                // sanitize labels
                // Normalize and sanitize labels
                route = (route ?? "unknown")
                    .TrimEnd('/')
                    .ToLowerInvariant()
                    .Replace("\"", "\\\"");

                method = (method ?? "UNKNOWN")
                    .ToUpperInvariant()
                    .Replace("\"", "\\\"");


        var hist = m.HistogramSnapshot;

                if (hist is null)
                {
                    sb.AppendLine($"http_request_duration_ms_bucket{{route=\"{route}\",method=\"{method}\",le=\"+Inf\"}} 0");
                    sb.AppendLine($"http_request_duration_ms_sum{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"http_request_duration_ms_count{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"requests_total{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"success_rate{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"avg_response_time_ms{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"p95_response_time_ms{{route=\"{route}\",method=\"{method}\"}} 0");
                    sb.AppendLine($"p99_response_time_ms{{route=\"{route}\",method=\"{method}\"}} 0");
                    continue;
                }

                // cumulative for finite bounds
                var cumulative = hist.ToCumulative();
                for (int i = 0; i < hist.Bounds.Length; i++)
                {
                    var le = hist.Bounds[i].ToString(inv);
                    sb.Append("http_request_duration_ms_bucket{route=\"")
                      .Append(route).Append("\",method=\"").Append(method)
                      .Append("\",le=\"").Append(le).Append("\"} ")
                      .AppendLine(cumulative[i].ToString(inv));
                }

                // +Inf bucket, sum, count
                sb.Append("http_request_duration_ms_bucket{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\",le=\"+Inf\"} ")
                  .AppendLine(hist.Count.ToString(inv));
                sb.Append("http_request_duration_ms_sum{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(hist.SumMs.ToString(inv));
                sb.Append("http_request_duration_ms_count{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(hist.Count.ToString(inv));

                // Convenience gauges
                sb.Append("requests_total{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(m.RequestCount.ToString(inv));
                sb.Append("success_rate{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(m.SuccessRate.ToString(inv));
                sb.Append("avg_response_time_ms{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(m.AvgResponseTimeMs.ToString(inv));
                sb.Append("p95_response_time_ms{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(m.P95ResponseTimeMs.ToString(inv));
                sb.Append("p99_response_time_ms{route=\"").Append(route).Append("\",method=\"").Append(method).Append("\"} ")
                  .AppendLine(m.P99ResponseTimeMs.ToString(inv));
            }

            // normalize CRLF -> LF for Prometheus parser on Windows
            var body = sb.ToString().Replace("\r\n", "\n");
            return Content(body, "text/plain; version=0.0.4");
        }
    }
}
