using System;
using System.Threading.Tasks;
using PrometheusProcessor;
using SensorMonHTTP;
using Xunit;

namespace PromDapterTests
{
    public class ServiceProcessorTests
    {
        [Fact]
        public async Task DataItemRegexProcessorTest()
        {
            var serviceProcessor = new ServiceProcessor();
            serviceProcessor.InitializeProcessors();
            var processor = serviceProcessor.DataItemRegexProcessor;
            var service = new HWiNFOProvider();
            string[] result;
            //for (int i = 0; i < 10000; i++)
            {
                result = await processor(service);
            }
            var metricDump = String.Join(Environment.NewLine, result);
        }
    }
}
