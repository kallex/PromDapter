using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        public MetricsController(ILogger<MetricsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<ContentResult> Get()
        {

            var serviceProcessor = new ServiceProcessor();
            serviceProcessor.InitializeProcessors();
            var processor = serviceProcessor.DataItemRegexProcessor;
            var service = new HWiNFOProvider();
            var processingResult = await processor(service);
            var textContent = String.Join(Environment.NewLine, processingResult);
            var content = Content(textContent);
            return content;
        }
    }
}
