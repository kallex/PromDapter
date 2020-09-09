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
        private static IPromDapterService[] Services = null;
        private static string Prefix = null;

        private static SemaphoreSlim Semaphore = new SemaphoreSlim(1);

        public async Task initServiceProcessor()
        {
            var configuredPrefix = Configuration?["PrometheusMetricPrefix"];
            const string defaultPrefix = "hwi_";
            string prefix;
            if (String.IsNullOrEmpty(configuredPrefix))
            {
                _logger.LogWarning($"No prefix defined in configuration; defaulting to {defaultPrefix}");
                prefix = defaultPrefix;
            }
            else
                prefix = configuredPrefix;
            var serviceProcessor = new ServiceProcessor();
            serviceProcessor.InitializeProcessors(prefix);
            Prefix = prefix;


            ServiceProcessor = serviceProcessor;
            var services = await ServiceProcessor.GetServices(Assembly.GetExecutingAssembly());
            Services = services;
            await serviceProcessor.InitializeServices(services);
            //serviceProcessor.CurrentProcessor = ServiceProcessor.DataItemRegexProcessor;
            _logger?.Log(LogLevel.Information, "Cache Initialized");
        }

        const string DebugFilterName = "debugerror";
        const string ResetFilterName = "reset";
        const string JsonFilterName = "json";


        private static Dictionary<string, object> ServiceParamDictionary = new Dictionary<string, object>()
        {
            {nameof(WMIProvider), new object[] {"Win32_LogicalDisk"}}
        };

        [HttpGet]
        [Route("")]
        [Route("{filter}")]
        public async Task<ContentResult> Get(string filter = null, string option = null)
        {
            bool allowedAccess = await Semaphore.WaitAsync(3000);
            if (!allowedAccess)
                return Content("Semaphore failed");
            try
            {
                var prefix = Prefix;
                var serviceProcessor = ServiceProcessor;
                IPromDapterService[] services = Services;
                bool isResetFilter = filter == ResetFilterName;
                bool isJsonFilter = filter == JsonFilterName;
                if (isResetFilter)
                {
                    Prefix = null;
                    ServiceProcessor = null;
                    _logger?.Log(LogLevel.Information, "Cache Reset");
                    return Content("Resetted");
                }

                if (ServiceProcessor == null)
                    await initServiceProcessor();
                

                /*
                var processor = serviceProcessor.DataItemRegexProcessor;
                {
                    var dump = new HWiNFOProvider();
                }
                */
                //var processingTasks = services.Select(item => processor(item));
                //await Task.WhenAll(processingTasks);
                //IEnumerable<string> processingResult = processingTasks.SelectMany(item => item.Result);
                //IEnumerable<string> processingResult = await serviceProcessor
                //    .GetPrometheusData();
                /*
                if (filter != null && filter.ToLower() != DebugFilterName)
                {
                    processingResult = processingResult.Where(item =>
                        item.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
                }
                */
                if (ServiceProcessor == null)
                {
                    await initServiceCache();
                }

                ContentResult content = null;
                if (isJsonFilter)
                {
                    content = await getJsonContent(option);
                }
                else
                {
                    content = await getPrometheusContent(filter, ServiceParamDictionary);
                }

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

        private async Task initServiceCache()
        {
            string prefix;
            var configuredPrefix = Configuration?["PrometheusMetricPrefix"];
            const string defaultPrefix = "hwi_";
            if (String.IsNullOrEmpty(configuredPrefix))
            {
                _logger.LogWarning($"No prefix defined in configuration; defaulting to {defaultPrefix}");
                prefix = defaultPrefix;
            }
            else
                prefix = configuredPrefix;

            var serviceProcessor = new ServiceProcessor();
            serviceProcessor.InitializeProcessors(prefix);
            Prefix = prefix;

            ServiceProcessor = serviceProcessor;
            var services = await ServiceProcessor.GetServices(Assembly.GetExecutingAssembly());
            Services = services;
            _logger?.Log(LogLevel.Information, "Cache Initialized");
        }

        private async Task<ContentResult> getPrometheusContent(string filter, Dictionary<string, object> paramDictionary)
        {
            ContentResult content;
            var processor = ServiceProcessor.DataItemRegexPrometheusProcessor;
            {
                var dump = new HWiNFOProvider();
                var dump2 = new WMIProvider();
            }
            var processingTasks = Services.Select(item =>
            {
                string typeName = item.GetType().Name;
                object parameters = null;
                paramDictionary.TryGetValue(typeName, out parameters);
                return processor(item, parameters);
            });
            await Task.WhenAll(processingTasks);
            var processingResults = processingTasks.Select(item => item.Result).ToArray();

            var validMimeType = System.Net.Mime.MediaTypeNames.Text.Plain;
            var invalidMimes = processingResults.Where(item => item.mimeType != validMimeType).Select(item => item.mimeType)
                .Distinct()
                .OrderBy(item => item)
                .ToArray();
            if (invalidMimes.Any())
            {
                string errorList = String.Join(", ", invalidMimes);
                throw new NotSupportedException($"Not supported mime type(s): {errorList}");
            }

            IEnumerable<string> combinedResults = processingResults
                .Select(item => item.data as string)
                .Where(item => item != null)
                .SelectMany(item => item.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();


            if (filter != null && filter.ToLower() != DebugFilterName)
            {
                combinedResults = combinedResults.Where(item =>
                    item.Contains(filter, StringComparison.InvariantCultureIgnoreCase));
            }

            var textContent = String.Join("\n", combinedResults);
            content = Content(textContent);
            return content;
        }

        private async Task<ContentResult> getJsonContent(string option)
        {
            ContentResult content;
            var processor = ServiceProcessor.DataItemRegexJsonProcessor;
            var processingTasks = Services.Select(item => processor(item, option));
            await Task.WhenAll(processingTasks);
            var processingResults = processingTasks.Select(item => item.Result).ToArray();

            var validMimeType = System.Net.Mime.MediaTypeNames.Application.Json;
            var invalidMimes = processingResults.Where(item => item.mimeType != validMimeType).Select(item => item.mimeType)
                .Distinct()
                .OrderBy(item => item)
                .ToArray();
            if (invalidMimes.Any())
            {
                string errorList = String.Join(", ", invalidMimes);
                throw new NotSupportedException($"Not supported mime type(s): {errorList}");
            }

            IEnumerable<string> combinedResults = processingResults
                .Select(item => item.data as string)
                .Where(item => item != null)
                .SelectMany(item => item.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                .ToArray();


            var textContent = String.Join("\n", combinedResults);
            content = Content(textContent);
            return content;
        }

    }
}
