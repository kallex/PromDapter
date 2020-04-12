using System;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using PromDapterDeclarations;
using PrometheusProcessor;
using SensorMonHTTP;
using Xunit;
using Xunit.Abstractions;

namespace PromDapterTests
{
    public class ServiceProcessorTests
    {
        public ServiceProcessorTests(ITestOutputHelper outputHelper)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(
                message => outputHelper.WriteLine(message));
        }


        [Fact]
        public async Task GetServicesTask()
        {
            var serviceInstances = await ServiceProcessor.GetServices(Assembly.GetExecutingAssembly());
            Assert.True(serviceInstances.Any());
            Assert.True(serviceInstances.All(item => item is IPromDapterService));
        }

        [Fact]
        public async Task DataItemRegexProcessorTest()
        {
            string[] result = null;
            for (int i = 0; i < 1000; i++)
            {
                var serviceProcessor = new ServiceProcessor();
                serviceProcessor.InitializeProcessors();
                var processor = serviceProcessor.DataItemRegexProcessor;
                var service = new HWiNFOProvider();
                result = await processor(service);
            }
            var metricDump = String.Join(Environment.NewLine, result);
        }


        [Fact]
        public async Task ServiceProcessorMemoryLeakTest()
        {
            string[] result = null;
            for (int i = 0; i < 100; i++)
            {
                var serviceProcessor = new ServiceProcessor();
                serviceProcessor.InitializeProcessors();
                var processor = serviceProcessor.DataItemRegexProcessor;
                var service = new HWiNFOProvider();
                result = await processor(service);
            }
            var metricDump = String.Join(Environment.NewLine, result);
            assertMemoryState();
        }


        private void assertMemoryState()
        {
            expectAmount(0, 
                typeof(MemoryMappedViewAccessor),
                typeof(GCHandle));
            
            expectAmount(1, typeof(HWiNFOProvider), typeof(YamlRegexMapData));
            
            void expectAmount(int expectedCount, params Type[] types)
            {
                foreach (var type in types)
                {
                    dotMemory.Check(memory =>
                    {
                        var actualCount = memory.GetObjects(item => item.Type.Is(type)).ObjectsCount;
                        Assert.True(expectedCount == actualCount ,
                            $"{type.Name}: expected {expectedCount} - had {actualCount}");
                    });
                }
            }
        }


    }
}
