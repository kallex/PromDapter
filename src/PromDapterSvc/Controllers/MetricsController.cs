using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PromDapterDeclarations;
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
        private IPromDapterService[] Services = null;
        private static string Prefix = null;

        private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        [HttpGet]
        [Route("")]
        [Route("{filter}")]
        public async Task<ContentResult> Get(string filter = null)
        {
            const string DebugFilterName = "debugerror";
            const string ResetFilterName = "reset";
            bool allowedAccess = await Semaphore.WaitAsync(3000);
            if (!allowedAccess)
                return Content("Semaphore failed");
            try
            {
                var prefix = Prefix;
                var serviceProcessor = ServiceProcessor;
                IPromDapterService[] services = Services;
                bool isResetFilter = filter == ResetFilterName;
                if (isResetFilter)
                {
                    Prefix = null;
                    ServiceProcessor = null;
                    _logger?.Log(LogLevel.Information, "Cache Reset");
                    return Content("Resetted");
                }

                if (serviceProcessor == null)
                {
                    
                    var configuredPrefix = Configuration?["PrometheusMetricPrefix"];
                    const string defaultPrefix = "hwi_";
                    if (String.IsNullOrEmpty(configuredPrefix))
                    {
                        _logger.LogWarning($"No prefix defined in configuration; defaulting to {defaultPrefix}");
                        prefix = defaultPrefix;
                    }
                    else
                        prefix = configuredPrefix;
                    serviceProcessor = new ServiceProcessor();
                    serviceProcessor.InitializeProcessors(prefix);
                    Prefix = prefix;
                    
                    ServiceProcessor = serviceProcessor;
                    services = await ServiceProcessor.GetServices(Assembly.GetExecutingAssembly());
                    Services = services;
                    _logger?.Log(LogLevel.Information, "Cache Initialized");
                }
                var processor = serviceProcessor.DataItemRegexProcessor;
                {
                    var dump = new HWiNFOProvider();
                }
                var processingTasks = services.Select(item => processor(item));
                await Task.WhenAll(processingTasks);
                IEnumerable<string> processingResult = processingTasks.SelectMany(item => item.Result);
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
                _logger.LogCritical(ex, "Unhandled error");
                if (filter == DebugFilterName)
                {
                    var content = Content(ex.ToString());
                    return content;
                }
                throw;
            }
            finally
            {
                Semaphore.Release();
            }
        }

    }
}
