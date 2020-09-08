using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using PromDapterDeclarations;
using SharpYaml.Serialization;

namespace PrometheusProcessor
{
    public class YamlRegexMapData
    {
        public ConcurrentDictionary<string, Regex> SensorRegexDict = new ConcurrentDictionary<string, Regex>();
        public ConcurrentDictionary<string, Dictionary<string, string>> SensorMetadataDict = new ConcurrentDictionary<string, Dictionary<string, string>>();

        public Dictionary<string, Regex[]> MetricTypeRegexDict = new Dictionary<string, Regex[]>();

        public Regex[] AllRegexes = new Regex[0];

        public YamlRegexMapData()
        {
            InitializeRegexDict();
        }

        public void InitializeRegexDict()
        {
            //var yamlContent = await File.ReadAllTextAsync();
            var fileName = "Prometheusmapping.yaml";
            var commonAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var commonFilePath = Path.Combine(commonAppDataFolder, "PromDapter", fileName);
            if (File.Exists(commonFilePath) && !Debugger.IsAttached)
            {
                fileName = commonFilePath;
            }
            object obj = null;
            using (var textStream = File.OpenText(fileName))
            {
                var serializer = new Serializer(new SerializerSettings());
                obj = serializer.Deserialize<ExpandoObject>(textStream);
            }

            dynamic dyn = obj;

            if (!PropertyExists(dyn, "mapping"))
            {
                if (PropertyExists(dyn, "mappers"))
                {
                    var combinedMapping = ((List<object>) dyn.mappers)
                        .Where((object item) => ((Dictionary<object, object>) item).ContainsKey("mapping"))
                        .SelectMany((dynamic item) => (List<object>) item["mapping"]).ToList();
                    dyn.mapping = combinedMapping;
                    /*
                    var hwInfoMapper = ((List<object>) dyn.mappers)
                        .FirstOrDefault((dynamic item) => item["name"] == "HWiNFOProvider");
                    if(hwInfoMapper != null)
                        dyn.mapping = hwInfoMapper["mapping"];
                    */
                }
            }

            var metricTypeDict = new Dictionary<string, Regex[]>();
            List<Regex> allRegexes = new List<Regex>();
            foreach (var mapping in dyn.mapping)
            {
                string name = mapping["name"];
                string[] patterns = ((List<object>)mapping["patterns"]).Cast<string>().ToArray();

                var regexes = patterns.Select(item => new Regex(item.StartsWith("^") ? item : "^" + item,
                        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Singleline))
                    .ToArray();
                allRegexes.AddRange(regexes);
                metricTypeDict.Add(name, regexes);
                //Debug.WriteLine($"Name: {name}");
                //Debug.WriteLine($"Pattern(s):");
                //Debug.WriteLine(String.Join(Environment.NewLine, patterns));
            }
            MetricTypeRegexDict = metricTypeDict;
            AllRegexes = allRegexes.ToArray();
        }

        public (Regex MatchingRegex, Dictionary<string, string> MetadataDictionary) GetSensorMetadata(string sensorName)
        {
            (Regex MatchingRegex, Dictionary<string, string> MetadataDictionary) result = (null, null);
            if (SensorRegexDict.TryGetValue(sensorName, out var regex) && SensorMetadataDict.TryGetValue(sensorName, out var metadataDict))
                return (regex, metadataDict);
            regex = AllRegexes.FirstOrDefault(item => item.IsMatch(sensorName));
            SensorRegexDict.TryAdd(sensorName, regex);
            if (regex == null)
                return result;

            var regexMatch = regex.Match(sensorName);
            metadataDict = new Dictionary<string, string>();

            foreach (var groupName in regex.GetGroupNames())
            {
                if (groupName == "0")
                    continue;
                var group = regexMatch.Groups[groupName];
                if (!group.Success)
                    continue;
                var name = groupName;
                var value = group.Value;
                if (name.Contains("_"))
                {
                    if(value != "")
                        throw new InvalidDataException($"Invalid Regex with Entity definition: {name} with non-expected value {value}");
                    var namesplit = name.Split('_');
                    name = namesplit[0];
                    value = namesplit[1];
                }
                metadataDict.Add(name, value);
            }
            SensorMetadataDict.TryAdd(sensorName, metadataDict);
            return (regex, metadataDict);
        }

        public static bool PropertyExists(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object> dict)
                return dict.ContainsKey(name);
            if (obj is Newtonsoft.Json.Linq.JObject)
                return ((Newtonsoft.Json.Linq.JObject)obj).ContainsKey(name);
            return obj.GetType().GetProperty(name) != null;
        }
    }

}