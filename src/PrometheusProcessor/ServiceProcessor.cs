using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PromDapterDeclarations;
using SensorMonHTTP;
using SharpYaml.Model;

namespace PrometheusProcessor
{
    public class ServiceProcessor
    {
        public delegate ((string helpKey, string helpLineSingleSource, string helpLineMultipleSource) helpItems, string typeLine, string prometheusLine) PrometheusOutputFunc(DataItem dataItem);
        public delegate (string sourceName, string sensorName, Dictionary<string, object> objectDict) JsonOutputFunc(DataItem dataItem);

        public delegate Task<(string mimeType, object data)> ProcessorPrometheusOutput(IPromDapterService service, object parameters);
        public delegate Task<(string mimeType, object data)> ProcessorJsonOutput(IPromDapterService service, object parameters);


        private string CurrentHostName = Environment.MachineName;
        public ServiceProcessor()
        {

        }

        public async Task InitializeServices(IPromDapterService[] services, string configFile)
        {
            Services = services;
            RegexMapData = YamlRegexMapData.InitializeFromFile(configFile);
            InitializeProcessors();
        }


        //public GetPrometheusOutputFunc WMIProviderPrometheusFunc { get; set; } 

        //public GetJsonOutputFunc WMIPRoviderJsonFunc { get; set; } 

        //public GetPrometheusOutputFunc HWiNFOProviderPrometheusFunc { get; set; }

        //public GetJsonOutputFunc HWiNFOProviderJsonFunc { get; set; } 


        public IPromDapterService[] Services { get; set; }

        //public Processor CurrentProcessor { get; set; }

        public YamlRegexMapData RegexMapData { get; set; }

        public ProcessorPrometheusOutput DataItemRegexPrometheusProcessor { get; set; }

        public ProcessorJsonOutput DataItemRegexJsonProcessor { get; set; }

        public void InitializeProcessors(string providerMetrixPrefix = null)
        {
            if (!String.IsNullOrEmpty(providerMetrixPrefix) && !providerMetrixPrefix.EndsWith("_"))
                providerMetrixPrefix += "_";
            var dataItemRegexProcessorNameMap = new Dictionary<string, string>()
            {
                { "MetricCategory", "Category" },
                { "MetricName", null },
                { "Entity", null },
            };

        PrometheusOutputFunc hwiPromFunc = item =>
        {
            var metadata = RegexMapData.GetSensorMetadata(item.Name);
            bool hasMetaData = metadata != (null, null);
            bool hasCategoryData = item.CategoryValues?.Any() == true;
            if (!hasMetaData && !hasCategoryData)
                return default;
            var metadataDict = hasMetaData
                ? new Dictionary<string, string>(metadata.MetadataDictionary)
                : item.CategoryValues.ToDictionary(kv => kv.Key,
                    kv => Convert.ToString(kv.Value.Cast<DataValue>().FirstOrDefault()?.Object, CultureInfo.InvariantCulture));
            if (!metadataDict.TryGetValue("MetricName", out var metricName))
            {
                //Debug.WriteLine($"Warning: No MetricName retrieved - {item.Name}");
                metricName = item.Name;
                //return;
            }

            var unit = item.Unit;
            var sensorName = item.Name;
            var sensorType = item.Category;
            var sourceName = item.Source.SourceName;
            metadataDict.Add("unit", unit);
            metadataDict.Add("sensor_type", sensorType);
            metadataDict.Add("sensor", sensorName);
            metadataDict.Add("source", sourceName);
            metadataDict.Add("host", CurrentHostName);

            // Move Entity name as prefix for MetricName
            if (metadataDict.ContainsKey("Entity"))
            {
                var entity = metadataDict["Entity"];
                metricName = entity + "_" + metricName;
            }

            if (!String.IsNullOrEmpty(unit))
            {
                metricName += "_" + unit;
            }

            var finalMetricName = $"{providerMetrixPrefix}{cleanupMetricName(metricName)}";
            var helpKey = $"# HELP {finalMetricName}";
            var helpString = $"{helpKey} {metricName.Replace("_", " ")} - {sourceName}";
            var multipleSourceHelpString = $"{helpKey} {metricName.Replace("_", " ")} - (multiple sources)";

            var metricType = getMetrictType(unit);
            string typeString = null;
            if (metricType != null)
            {
                typeString = $"# TYPE {finalMetricName} {metricType}";
            }
            var categoryParts = metadataDict.Keys
                .Select(mapKeyName)
                .Where(keyItem => keyItem.keyName != null)
                .Select(keyItem => $"{cleanupKeyName(keyItem.keyName)}=\"{metadataDict[keyItem.key]}\"").ToArray();
            var categoryString = String.Join(",", categoryParts);
            var promStr =
                $"{finalMetricName}{{{categoryString}}} {Convert.ToString(item.Value.Object, CultureInfo.InvariantCulture)}";

            return ((helpKey, helpString, multipleSourceHelpString), typeString, promStr);
        };

        JsonOutputFunc hwiJsonFunc = null;

        PrometheusOutputFunc wmiPromFunc = null;
        JsonOutputFunc wmiJsonFunc = null;

        var itemProcessorDict =
                new Dictionary<string, (PrometheusOutputFunc prometheusOutputFunc, JsonOutputFunc jsonOutputFunc)>();

        itemProcessorDict.Add("SensorMonHTTP.WMIProvider", (wmiPromFunc, wmiJsonFunc));
        itemProcessorDict.Add("SensorMonHTTP.HWiNFOProvider",
            (hwiPromFunc, hwiJsonFunc));




            DataItemRegexPrometheusProcessor = async (service, parameters) =>
            {
                var serviceTypeName = service.GetType().FullName;
                var outputFunc = itemProcessorDict[serviceTypeName].prometheusOutputFunc;

                await service.Open();
                var dataItems = await service.GetDataItems((object[]) parameters) ;
                await service.Close(true);

                if (dataItems == null)
                    return (null, null);

                const double MinimumAmountPerCore = 100.0;
                var itemCount = dataItems.Length;
                // Above 4 we're starting to get diminishing returns; tested on 16 Core Ryzen 3950X with 370 metrics
                // thus we estimate around 100 per core is sensible metric worthwhile the scheduling
                var degreeOfParallelism = (int) Math.Ceiling(itemCount / MinimumAmountPerCore);
                if (Debugger.IsAttached)
                    degreeOfParallelism = 1;
                var parallelBag = new ConcurrentBag<(string helpKey, string typeString, string[] metricLines)>();
                // Dicting helpDict to avoid errors on multiple helps
                var helpDict = new ConcurrentDictionary<string, string>();

                Parallel.ForEach(dataItems, new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = degreeOfParallelism
                    },
                    item =>
                    {
                        var itemOutput = outputFunc(item);

                        var helpItems = itemOutput.helpItems;
                        var helpKey = helpItems.helpKey;
                        var helpString = helpItems.helpLineSingleSource;
                        var multipleSourceHelpString = helpItems.helpLineMultipleSource;

                        string typeLine = itemOutput.typeLine;
                        var prometheusLine = itemOutput.prometheusLine;
                        
                        // If already exists with different name, add multiple sources name
                        var helpLine = helpDict.AddOrUpdate(helpKey, helpString, (key, oldValue) => helpString == oldValue ? oldValue : multipleSourceHelpString);

                        parallelBag.Add((helpKey, typeLine, new[] {prometheusLine}));
                    });
                var grouped = parallelBag
                    .OrderBy(item => helpDict[item.helpKey]).ThenBy(item => item.typeString)
                    .GroupBy(item => (helpDict[item.helpKey], item.typeString));
                var lineResult = grouped
                    .SelectMany(grp => new[] {grp.Key.Item1, grp.Key.typeString}
                        .Concat(grp.SelectMany(item => item.metricLines).OrderBy(item => item))
                        .Where(item => !String.IsNullOrEmpty(item))).ToArray();
                var result = String.Join(Environment.NewLine, lineResult);
                var mimeType = System.Net.Mime.MediaTypeNames.Text.Plain;
                return (mimeType, result);
            };

            DataItemRegexJsonProcessor = async (service, parameters) =>
            {
                var serviceTypeName = service.GetType().FullName;
                var outputFunc = itemProcessorDict[serviceTypeName].prometheusOutputFunc;

                bool flattenMeta = false;
                const string FlattenMetaName = nameof(flattenMeta);
                string[] groupBy;
                const string GroupByName = nameof(groupBy);

                await service.Open();
                object[] serviceParams = null;
                if (service is WMIProvider)
                    serviceParams = new object[] { "Win32_LogicalDisk" };
                var dataItems = await service.GetDataItems(serviceParams);
                await service.Close(true);

                if (dataItems == null)
                    return (null, null);

                var paramArray = (parameters as string)?.Split(new [] {","}, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                flattenMeta = paramArray.Contains(FlattenMetaName);
                groupBy = paramArray.FirstOrDefault(item => item.StartsWith(GroupByName))?.Split('-').Skip(1)
                    .ToArray();


                const double MinimumAmountPerCore = 100.0;
                var itemCount = dataItems.Length;
                // Above 4 we're starting to get diminishing returns; tested on 16 Core Ryzen 3950X with 370 metrics
                // thus we estimate around 100 per core is sensible metric worthwhile the scheduling
                var degreeOfParallelism = (int)Math.Ceiling(itemCount / MinimumAmountPerCore);
                if (Debugger.IsAttached)
                    degreeOfParallelism = 1;
                var parallelBag = new ConcurrentBag<(string sourceName, string sensorName, Dictionary<string, object> dataObject)>();

                Parallel.ForEach(dataItems, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = degreeOfParallelism
                },
                    item =>
                    {
                        var metadata = RegexMapData.GetSensorMetadata(item.Name);
                        bool hasMetaData = metadata != (null, null);
                        bool hasCategoryData = item.CategoryValues?.Any() == true;
                        if (!hasMetaData && !hasCategoryData)
                            return;
                        var metadataDict = hasMetaData
                            ? new Dictionary<string, string>(metadata.MetadataDictionary)
                            : item.CategoryValues.ToDictionary(kv => kv.Key,
                                kv => Convert.ToString(kv.Value.Cast<DataValue>().FirstOrDefault()?.Object, CultureInfo.InvariantCulture));
                        if (!metadataDict.TryGetValue("MetricName", out var metricName))
                        {
                            //Debug.WriteLine($"Warning: No MetricName retrieved - {item.Name}");
                            //return;
                            metricName = item.Name;
                        }

                        var unit = item.Unit;
                        var sensorName = item.Name;
                        var sensorType = item.Category;
                        var sourceName = item.Source.SourceName;
                        var value = item.Value.Object;
                        var valueType = item.Value.Type.Name;

                        var objectDict = new Dictionary<string, object>();
                        objectDict.Add("unit", unit);
                        objectDict.Add("sensor_type", sensorType);
                        objectDict.Add("sensor", sensorName);
                        objectDict.Add("source", sourceName);
                        objectDict.Add("value", value);
                        objectDict.Add("valueType", valueType);

                        if (flattenMeta)
                        {
                            foreach (var key in metadataDict.Keys)
                            {
                                if (objectDict.ContainsKey(key))
                                    continue;
                                objectDict.Add(key, metadataDict[key]);
                            }
                        }
                        else
                        {
                            objectDict.Add("metadata", metadataDict);
                        }

                        // Move Entity name as prefix for MetricName
                        if (objectDict.ContainsKey("Entity"))
                        {
                            var entity = objectDict["Entity"];
                            metricName = entity + "_" + metricName;
                        }

                        if (!String.IsNullOrEmpty(unit))
                        {
                            metricName += "_" + unit;
                        }

                        var finalMetricName = $"{providerMetrixPrefix}{cleanupMetricName(metricName)}";
                        objectDict.Add("metric", finalMetricName);
                        /*
                        var categoryParts = objectDict.Keys
                            .Select(mapKeyName)
                            .Where(keyItem => keyItem.keyName != null)
                            .Select(keyItem => $"{cleanupKeyName(keyItem.keyName)}=\"{objectDict[keyItem.key]}\"").ToArray();
                        */

                        // TODO: Check mapKeyName & cleanupKeyName requirements
                        parallelBag.Add((sourceName, sensorName, objectDict));
                    });
                var orderedObjects = parallelBag
                    .OrderBy(item => item.sourceName)
                    .ThenBy(item => item.sensorName)
                    .Select(item => item.dataObject)
                    .ToArray();

                object dataToSerialize;
                if (groupBy != null)
                {
                    var byVal = groupBy.FirstOrDefault();
                    var grp = orderedObjects.GroupBy(item => byVal != null && item.ContainsKey(byVal) ? item[byVal].ToString() : "");
                    var groupedObjects = grp.Select(item => new
                        {
                            key = item.Key,
                            keyField = byVal,
                            data = item.ToArray()
                        }).OrderBy(item => item.key)
                        .ToArray();
                    dataToSerialize = groupedObjects;
                }
                else
                {
                    dataToSerialize = orderedObjects;
                }

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true,
                    }
                };

                var jsonObject = JsonConvert.SerializeObject(dataToSerialize, new JsonSerializerSettings()
                {
                    ContractResolver = contractResolver,
                    Formatting = Formatting.Indented
                });

                (string mimeType, object data) result = ("application/json", jsonObject);
                return result;



            };


            (string key, string keyName) mapKeyName(string key)
            {
                if (!dataItemRegexProcessorNameMap.TryGetValue(key, out var mappingResult))
                    return (key, key);
                return (key, mappingResult);
            }

            string cleanupMetricName(string name)
            {
                var charLQ = name
                    .Where(c => Char.IsLetterOrDigit(c) || c == '_' || c == ' ')
                    .Select(c => c == ' ' ? '_' : Char.ToLowerInvariant(c));
                var stringBuilder = new StringBuilder(name.Length);
                foreach (var c in charLQ)
                    stringBuilder.Append(c);
                var fixedName = stringBuilder.ToString().TrimEnd('_');
                return fixedName;
            }

            string cleanupKeyName(string name)
            {
                return name.ToLowerInvariant().Replace(" ", "_");
            }
            string getMetrictType(string unit)
            {
                switch (unit)
                {
                    default:
                        return null;
                }
            }

        }

        public static async Task<IPromDapterService[]> GetServices(Assembly requestingAssembly)
        {
            var serviceTypes = await GetServiceTypes(requestingAssembly);
            var serviceInstances = serviceTypes.Select(item => Activator.CreateInstance(item))
                .Cast<IPromDapterService>().ToArray();
            return serviceInstances;
        }

        public static async Task<Type[]> GetServiceTypes(Assembly requestingAssembly)
        {
            var assembly = requestingAssembly;

            var dependencyNames = assembly.GetReferencedAssemblies();

            List<Assembly> assemblies = new List<Assembly>();
            assemblies.Add(assembly);
            foreach (var dependencyName in dependencyNames)
            {
                try
                {
                    var referencedAsm = Assembly.Load(dependencyName);
                    // Try to load the referenced assembly...
                    assemblies.Add(referencedAsm);
                }
                catch
                {
                    // Failed to load assembly. Skip it.
                }
            }

            var types = assemblies.SelectMany(item => item.GetTypes()).ToArray();
            var serviceTypes =
                types.Where(item => item.GetInterfaces().Any(inter => inter == typeof(IPromDapterService)))
                    .Distinct()
                    .ToArray();
            //var serviceTypesByName = types.Where(item => item.GetInterfaces().Any(inter => inter.Name == nameof(IPromDapterService))).ToArray();
            return serviceTypes;
        }
    }
}
