using Xunit;
using PrometheusProcessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PrometheusProcessor.Tests
{
    public class YamlRegexMapDataTests
    {
        [Fact()]
        public void InitializeRegexDictTest()
        {
            YamlRegexMapData mapData = new YamlRegexMapData();
            mapData.InitializeRegexDict();
            Assert.True(mapData.AllRegexes.Any(), "Need to find some regexp");
        }
    }
}