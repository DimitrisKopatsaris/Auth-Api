using Serilog.Core;
using Serilog.Events;

namespace AuthApi.Logging
{
    public class CorrelationIdEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier 
                                ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault();

            if (!string.IsNullOrEmpty(correlationId))
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("CorrelationId", correlationId));
            }
        }
    }
}
