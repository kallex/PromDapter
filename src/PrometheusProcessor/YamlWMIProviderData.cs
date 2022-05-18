using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using CamelCaseNamingConvention = YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention;
using Serializer = YamlDotNet.Serialization.Serializer;

namespace PrometheusProcessor
{
    public class YamlWMIProviderData
    {
        private YamlWMIProviderData()
        {
        }

        public static YamlWMIProviderData InitializeFromFile(string fileName)
        {

            using var textStream = File.OpenText(fileName);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            var rootObject = deserializer.Deserialize<RootYaml>(textStream);

            var result = new YamlWMIProviderData();
            return result;
        }

        /*
         
wmiservice:
  - source: Win32_Process
    ids:
    - Name
    - ProcessId
  - source: Win32_LogicalDisk
    name: Disk
    ids:
    - DeviceID
    values:
    - name: Size
      unit: Byte
    - name: FreeSpace
      unit: Byte

         
         */

        public static bool PropertyExists(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object> strDict)
                return strDict.ContainsKey(name);
            if (obj is IDictionary<object, object> objDict)
                return objDict.ContainsKey(name);
            if (obj is Newtonsoft.Json.Linq.JObject)
                return ((Newtonsoft.Json.Linq.JObject)obj).ContainsKey(name);
            return obj.GetType().GetProperty(name) != null;
        }

        public class RootYaml
        {
            [YamlMember(typeof(WMIService), Alias = "wmiService")]
            public WMIService WMIService { get; set; }
        }

        [DebuggerDisplay("{Sources}")]
        public class WMIService
        {
            public List<WMISource> Sources { get; set; }
        }

        [DebuggerDisplay("{Source} ({Name})")]
        public class WMISource
        {
            public string Source { get; set; }
            public string Name { get; set; }
            [YamlMember(typeof(List<string>), Alias = "ids")]
            public List<string> IDs { get; set; }

            public List<ValueItem> Values { get; set; }
        }

        [DebuggerDisplay("{Name} ({Unit})")]
        public class ValueItem
        {
            public string Name { get; set; }
            public string Unit { get; set; }
        }
    }
}