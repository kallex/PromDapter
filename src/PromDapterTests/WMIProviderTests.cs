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
        public async Task WMIProviderTest_Win32_LogicalDisk__DeviceID__Size_FreeSpace()
        {
            var dataItems = await getWMIDataFromMethodName();

            Assert.True(dataItems.Any());
        }

        [Fact()]
        public async Task WMIProviderTest_Win32_DesktopMonitor() // Win32_DesktopMonitor
        {
            var dataItems = await getWMIDataFromMethodName();

            Assert.True(dataItems.Any());
        }

        [Fact()]
        public async Task WMIProviderTest_Win32_Process__Name_ProcessId()
        {
            var dataItems = await getWMIDataFromMethodName();
            dataItems = dataItems.OrderBy(item => item.CategoryValues["ProcessId"].First().Object?.ToString())
                .ThenBy(item => item.CategoryValues["Name"].First().Object.ToString()).ToArray();

            Assert.True(dataItems.Any());
        }


        [Fact()]
        public async Task WMIProviderTest_Win32_Process__Name_ProcessId__WorkingSetSize()
        {
            //var dataItems = await getWMIDataFromMethodName(new[] {"Name", "ProcessId"});
            //var dataItems = await getWMIDataFromMethodName(new[] {"Name", "ProcessId"}, new [] { "WorkingSetSize"});
            var dataItems = await getWMIDataFromMethodName();
            dataItems = dataItems.OrderBy(item => item.CategoryValues["ProcessId"].First().Object?.ToString())
                .ThenBy(item => item.CategoryValues["Name"].First().Object.ToString()).ToArray();

            Assert.True(dataItems.Any());
        }

        [Fact()]
        public async Task WMIProviderTest_Win32_PerfFormattedData_PerfProc_Process__Name__PercentProcessorTime()
        {
            var dataItems = await getWMIDataFromMethodName();
            dataItems = dataItems.OrderBy(item => item.CategoryValues["Name"].First().Object?.ToString()).ToArray();

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
            var dataItems = await getWMIDataFromMethodName(new []{"Caption"});

            Assert.True(dataItems.Any());
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<DataItem[]> getWMIDataFromMethodName(string[] identifierNames = null, string[] propertyFilter = null)
        {
            const string partSeparator = "__";
            const string arraySeparator = "_";

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


            var methodName = callingMethodName.Replace(testMethodNamePrefix, "");
            var parameterParts = methodName.Split(partSeparator);

            var wmiClassName = parameterParts.FirstOrDefault();
            if (identifierNames == null)
            {
                var identifierPart = parameterParts.Skip(1).FirstOrDefault();
                identifierNames = identifierPart?.Split(arraySeparator);
            }

            if (propertyFilter == null)
            {
                var propertyFilterPart = parameterParts.Skip(2).FirstOrDefault();
                propertyFilter = propertyFilterPart?.Split(arraySeparator);
            }

            var result = await getWMIDataItems(wmiClassName, identifierNames, propertyFilter);
            return result;
        }

        private static async Task<DataItem[]> getWMIDataItems(string wmiClassName, string[] identifierNames, string[] propertyFilter)
        {
            var provider = new WMIProvider();
            await provider.Open();

            var wmiParameters = (itemName: wmiClassName, identifierNames: identifierNames, propertyFilter: propertyFilter);
            var dataItems = await provider.GetDataItems(new object[] { wmiParameters });
            dataItems = dataItems.OrderBy(item => item.Source.SourceName).ThenBy(item => item.Name).ToArray();

            await provider.Close();
            return dataItems;
        }
    }
}