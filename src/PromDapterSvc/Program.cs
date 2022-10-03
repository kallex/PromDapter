using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PromDapterSvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                //.AddJsonFile("hosting.json", optional: true)
                .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                //.AddCommandLine(args)
                //.AddEnvironmentVariables()
                .Build();
            CreateHostBuilder(args, config).Build().Run();
        }

        public class AppConfig
        {
            public string ServerUrl { get; set; }
        } 

        public static IHostBuilder CreateHostBuilder(string[] args, IConfigurationRoot configurationRoot) 
        {
            var appConfig = configurationRoot.GetSection(nameof(AppConfig)).Get<AppConfig>();

            var builder = Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                //.UseConfig
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    //webBuilder.UseConfiguration(config);
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls(appConfig.ServerUrl);
                    //webBuilder.UseUrls("http://0.0.0.0:10445");
                })
                .ConfigureLogging(loggingBuilder =>
                {
                    loggingBuilder.AddEventSourceLogger();
                    //loggingBuilder.AddEventLog();
                    /*
                    logSettings =>
                    {
                        logSettings.SourceName = nameof(PromDapterSvc);
                        logSettings.Filter = (s, level) => true;
                    });*/
                });
            return builder;
        }
    }
}
