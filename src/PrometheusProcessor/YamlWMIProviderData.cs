using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SharpYaml.Serialization;

namespace PrometheusProcessor
{
    public class YamlWMIProviderData
    {
        private YamlWMIProviderData()
        {
        }

        public static YamlWMIProviderData InitializeFromFile(string fileName)
        {
            object obj = null;
            using (var textStream = File.OpenText(fileName))
            {
                var serializer = new Serializer(new SerializerSettings());
                obj = serializer.Deserialize<ExpandoObject>(textStream);
            }

            dynamic dyn = obj;

            var metricTypeDict = new Dictionary<string, Regex[]>();
            List<Regex> allRegexes = new List<Regex>();
            foreach (var wmiSource in dyn.wmiservice)
            {
                string source = wmiSource["source"];
                string[] ids = ((List<object>)wmiSource["ids"]).Cast<string>().ToArray();
                //string name = wmiSource["name"];
                string name = PropertyExists(wmiSource, "name") ? wmiSource["name"] : null;

                var values = PropertyExists(wmiSource, "values") ? ((List<object>)wmiSource["values"]) : null;

                //(string name, string unit)[] values = ((List<object>)wmiSource["values"])

                //Debug.WriteLine($"Name: {name}");
                //Debug.WriteLine($"Pattern(s):");
                //Debug.WriteLine(String.Join(Environment.NewLine, patterns));
            }

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

    }
}