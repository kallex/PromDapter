using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
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

        private static ServiceProcessor ServiceProcessor = null;
        private static string Prefix = null;

        private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        [HttpGet]
        [Route("")]
        [Route("{filter}")]
        public async Task<ContentResult> Get(string filter = null)
        {
            /*
            var gcSettings = new
            {
                GCSettings.IsServerGC,
                GCSettings.LargeObjectHeapCompactionMode,
                GCSettings.LatencyMode
            };
            */
            const string DebugFilterName = "debugerror";
            const string ResetFilterName = "reset";
            bool allowedAccess = await Semaphore.WaitAsync(3000);
            if (!allowedAccess)
                return Content("Semaphore failed");
            try
            {
                var prefix = Prefix;
                var serviceProcessor = ServiceProcessor;
                bool isResetFilter = filter == ResetFilterName;
                if (isResetFilter)
                {
                    Prefix = null;
                    ServiceProcessor = null;
                    return Content("Resetted");
                }
                if (serviceProcessor == null)
                {
                    prefix = Configuration?["PrometheusMetricPrefix"] ?? "hw";
                    serviceProcessor = new ServiceProcessor();
                    serviceProcessor.InitializeProcessors(prefix);
                    Prefix = prefix;
                    ServiceProcessor = serviceProcessor;
                } 
                var processor = serviceProcessor.DataItemRegexProcessor;
                var service = new HWiNFOProvider();
                IEnumerable<string> processingResult = await processor(service);
                if (filter != null && filter.ToLower() != DebugFilterName)
                {
                    processingResult = processingResult.Where(item =>
                        item.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
                }

                var textContent = String.Join("\n", processingResult);
                var content = Content(textContent);
                return content;
            }
            catch (Exception ex)
            {
                ServiceProcessor = null;
                Prefix = null;
                if (filter == DebugFilterName)
                {
                    var content = Content(ex.ToString());
                    return content;
                }

                throw;
            }
            finally
            {
                //GC.WaitForFullGCComplete();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
                //GC.WaitForFullGCComplete();
                Semaphore.Release();
                //GC.Collect();
            }
        }
    }
}
