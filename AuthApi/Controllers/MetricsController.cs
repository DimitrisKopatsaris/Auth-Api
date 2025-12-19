using AuthApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly MetricsService _metrics;

        public MetricsController(MetricsService metrics) => _metrics = metrics;

        // GET /api/metrics
        [HttpGet]
        public IActionResult Get() => Ok(_metrics.GetAllMetrics());

        // POST /api/metrics/reset
        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _metrics.Reset();
            return NoContent();
        }
    }
}
