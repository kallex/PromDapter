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

        public void InitializeProcessors()
        {
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
                ConcurrentBag<string[]> parallelBag = new ConcurrentBag<string[]>();

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
                        metadataDict.Add("unit", item.Unit);
                        metadataDict.Add("category", item.Category);
                        metadataDict.Add("sensor", item.Name);
                        metadataDict.Add("source", item.Source.SourceName);
                        var categoryParts = metadataDict.Keys.Select(key => $"{cleanupValue(key)}=\"{metadataDict[key]}\"").ToArray();
                        var categoryString = String.Join(",", categoryParts);
                        var promStr =
                            $"{cleanupValue(metricName)}{{{categoryString}}} {Convert.ToString(item.Value.Object, CultureInfo.InvariantCulture)}";
                        parallelBag.Add(new[] {promStr});
                    });
                var result = parallelBag.SelectMany(item => item).ToArray();
                return result;

                string cleanupValue(string value)
                {
                    return value.ToLowerInvariant().Replace(" ", "_");
                }
            };

        }
    }
}
