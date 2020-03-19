using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PromDapterSvc.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AboutController : ControllerBase
    {
        private readonly ILogger<MetricsController> _logger;

        public AboutController(ILogger<MetricsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ContentResult Version()
        {
            return Content("1.2.3");
        }
    }
}