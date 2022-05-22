using Xunit;

namespace PrometheusProcessor.Tests
{
    public class YamlWMIProviderDataTests
    {
        [Fact()]
        public void InitializeDataTest()
        {
            var configFile = Tests.GetConfigFilename();
            var providerData = YamlWMIProviderData.InitializeFromFile(configFile);
            Assert.Equal(2, providerData.WMIServiceData.Sources.Count);
        }
    }
}