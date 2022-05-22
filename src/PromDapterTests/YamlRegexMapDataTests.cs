using Xunit;
using PrometheusProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PrometheusProcessor.Tests
{
    public class YamlRegexMapDataTests
    {
        [Fact()]
        public void InitializeRegexDictTest()
        {
            var configFile = Tests.GetConfigFilename();
            var mapData = YamlRegexMapData.InitializeFromFile(configFile);
            Assert.True(mapData.AllRegexes.Any(), "Need to find some regexp");
        }


    }
}