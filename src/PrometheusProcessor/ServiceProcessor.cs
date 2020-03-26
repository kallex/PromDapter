using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PromDapterDeclarations;
using SharpYaml.Model;

namespace PrometheusProcessor
{
    public class ServiceProcessor
    {
        public delegate Task<string[]> Processor(IPromDapterService service);
        public ServiceProcessor()
        {

        }

        public async Task InitializeServices(IPromDapterService[] services)
        {
            Services = services;
            RegexMapData = new YamlRegexMapData();
            InitializeProcessors();
        }

        public IPromDapterService[] Services { get; set; }

        public async Task<string[]> GetPrometheusData()
        {
            Task<string[]>[] serviceFetcherTasks = Services.Select(item => CurrentProcessor(item)).ToArray();
            await Task.WhenAll(serviceFetcherTasks);
            var result = serviceFetcherTasks.SelectMany(item => item.Result).ToArray();
            return result;
        }

        public Processor CurrentProcessor { get; set; }

        public YamlRegexMapData RegexMapData = new YamlRegexMapData();

        public Processor DataItemRegexProcessor;

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


            DataItemRegexProcessor = async service =>
            {

                await service.Open();
                var dataItems = await service.GetDataItems() ;
                await service.Close(true);

                if (dataItems == null)
                    return new string[0];

                const double MinimumAmountPerCore = 100.0;
                var itemCount = dataItems.Length;
                // Above 4 we're starting to get diminishing returns; tested on 16 Core Ryzen 3950X with 370 metrics
                // thus we estimate around 100 per core is sensible metric worthwhile the scheduling
                var degreeOfParallelism = (int) Math.Ceiling(itemCount / MinimumAmountPerCore);
                var parallelBag = new ConcurrentBag<(string helpString, string typeString, string[] metricLines)>();

                Parallel.ForEach(dataItems, new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = degreeOfParallelism
                    },
                    item =>
                    {
                        var metadata = RegexMapData.GetSensorMetadata(item.Name);
                        if (metadata == (null, null))
                            return;
                        var metadataDict = new Dictionary<string, string>(metadata.MetadataDictionary);
                        if (!metadataDict.TryGetValue("MetricName", out var metricName))
                        {
                            Debug.WriteLine($"Warning: No MetricName retrieved - {item.Name}");
                            return;
                        }

                        var unit = item.Unit;
                        var sensorName = item.Name;
                        var sensorType = item.Category;
                        var sourceName = item.Source.SourceName;
                        metadataDict.Add("unit", unit);
                        metadataDict.Add("sensor_type", sensorType);
                        metadataDict.Add("sensor", sensorName);
                        metadataDict.Add("source", sourceName);

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
                        var helpString = $"# HELP {finalMetricName} {metricName.Replace("_", " ")} - {sourceName}";
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
                        parallelBag.Add((helpString, typeString, new[] {promStr}));
                    });
                var grouped = parallelBag
                    .OrderBy(item => item.helpString).ThenBy(item => item.typeString)
                    .GroupBy(item => (item.helpString, item.typeString));
                var result = grouped
                    .SelectMany(grp => new[] {grp.Key.helpString, grp.Key.typeString}
                        .Concat(grp.SelectMany(item => item.metricLines).OrderBy(item => item))
                        .Where(item => !String.IsNullOrEmpty(item))).ToArray();
                return result;

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
            };

        }

        
    }
}
