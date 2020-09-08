using Xunit;
using SensorMonHTTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PromDapterDeclarations;

namespace SensorMonHTTP.Tests
{
    public class WMIProviderTests
    {
        [Fact()]
        public async Task WMIProviderTest_Win32_LogicalDisk()
        {
            var dataItems = await getWMIDataFromMethodName();

            Assert.True(dataItems.Any());
        }


        [Fact()]
        public async Task WMIProviderTest_Win32_Process()
        {
            var dataItems = await getWMIDataFromMethodName("Name", "ProcessId");

            Assert.True(dataItems.Any());
        }

        [Fact()]
        public async Task WMIProviderTest_Win32_NetworkAdapter()
        {
            var dataItems = await getWMIDataFromMethodName();

            Assert.True(dataItems.Any());
        }

        [Fact()]
        public async Task WMIProviderTest_Win32_NetworkAdapterConfiguration()
        {
            var dataItems = await getWMIDataFromMethodName("Caption");

            Assert.True(dataItems.Any());
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<DataItem[]> getWMIDataFromMethodName(params string[] identifierNames)
        {
            const string testMethodNamePrefix = "WMIProviderTest_";
            var stack = new StackTrace();
            var frames = stack.GetFrames();
            var callingMethodName = frames
                .FirstOrDefault(frame => frame.GetMethod().GetCustomAttributes(typeof(FactAttribute), true).Any())
                ?.GetMethod().Name;
            if(callingMethodName == null)
                throw new InvalidOperationException("No test method found in callstack");
            if(!callingMethodName.StartsWith(testMethodNamePrefix))
                throw new InvalidDataException($"Not supported format in test method name {callingMethodName} missing prefix {testMethodNamePrefix}");
            var wmiClassName = callingMethodName.Replace(testMethodNamePrefix, "");
            var result = await getWMIDataItems(wmiClassName, identifierNames);
            return result;
        }

        private static async Task<DataItem[]> getWMIDataItems(string wmiClassName, string[] identifierNames)
        {
            var provider = new WMIProvider();
            await provider.Open();

            var dataItems = await provider.GetDataItems(wmiClassName, identifierNames);
            dataItems = dataItems.OrderBy(item => item.Source.SourceName).ThenBy(item => item.Name).ToArray();

            await provider.Close();
            return dataItems;
        }
    }
}