using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PrometheusProcessor;
using SensorMonHTTP;

namespace PromDapterSvc.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetricsController : ControllerBase
    {

        private readonly ILogger<MetricsController> _logger;
        private readonly IConfiguration Configuration;

        public MetricsController(ILogger<MetricsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        [HttpGet]
        [Route("")]
        [Route("{filter}")]
        public async Task<ContentResult> Get(string filter = null)
        {

            var prefix = Configuration["PrometheusMetricPrefix"];
            var serviceProcessor = new ServiceProcessor();
            serviceProcessor.InitializeProcessors(prefix);
            var processor = serviceProcessor.DataItemRegexProcessor;
            var service = new HWiNFOProvider();
            IEnumerable<string> processingResult = await processor(service);
            if (filter != null)
            {
                processingResult = processingResult.Where(item => item.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }
            var textContent = String.Join("\n", processingResult);
            var content = Content(textContent);
            return content;
        }
    }
}
