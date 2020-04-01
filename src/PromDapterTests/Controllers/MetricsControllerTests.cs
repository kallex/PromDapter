using Xunit;
using PromDapterSvc.Controllers;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.dotMemoryUnit;
using PrometheusProcessor;
using SensorMonHTTP;
using Xunit.Abstractions;

namespace PromDapterSvc.Controllers.Tests
{
    public class MetricsControllerTests
    {
        public MetricsControllerTests(ITestOutputHelper outputHelper)
        {
            DotMemoryUnitTestOutput.SetOutputMethod(
                message => outputHelper.WriteLine(message));
        }


        [Fact]
        public async Task GetMemoryLeakTest()
        {
            for (int i = 0; i < 100; i++)
            {
                var controller = new MetricsController(null, null);
                var result = await controller.Get();
            }
            assertMemoryState();
        }

        [Fact]
        public async Task GetPerformanceProfileTest()
        {
            for (int i = 0; i < 10000; i++)
            {
                var controller = new MetricsController(null, null);
                var result = await controller.Get();
            }
        }



        private void assertMemoryState()
        {
            expectAmount(0,
                typeof(MemoryMappedViewAccessor),
                typeof(GCHandle),
                typeof(HWiNFOProvider));

            expectAmount(1, typeof(YamlRegexMapData));

            void expectAmount(int expectedCount, params Type[] types)
            {
                foreach (var type in types)
                {
                    dotMemory.Check(memory =>
                    {
                        var actualCount = memory.GetObjects(item => item.Type.Is(type)).ObjectsCount;
                        Assert.True(expectedCount == actualCount,
                            $"{type.Name}: expected {expectedCount} - had {actualCount}");
                    });
                }
            }
        }

    }
}