using System;
using System.IO;
using System.Reflection;

namespace PrometheusProcessor.Tests
{
    public static class Tests
    {
        public static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public static string GetConfigFilename()
        {
            //var yamlContent = await File.ReadAllTextAsync();
            var fileName = "Prometheusmapping.yaml";
            //var commonAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            //var commonFilePath = Path.Combine(commonAppDataFolder, "PromDapter", fileName);
            //return commonFilePath;

            var filePath = Path.Combine(GetAssemblyDirectory(), fileName);
            return filePath;
        }
    }
}