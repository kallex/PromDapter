using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using PromDapterDeclarations;

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
                var dataItems = await service.GetDataItems();
                await service.Close(true);
                Parallel.ForEach(dataItems, new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 1
                    },
                    item =>
                {
                    var metadata = RegexMapData.GetSensorMetadata(item.Name);
                });
                string[] result = null;
                return result;
            };

        }
    }
}
