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
        }
    }
}