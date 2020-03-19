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
            var result = await processor(service);
        }
    }
}
