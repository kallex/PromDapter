using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using PromDapterDeclarations;

namespace SensorMonHTTP
{
    public class WMIProvider : IPromDapterService
    {
        public WMIProvider()
        {
            Close = CloseAsync;
            GetDataItems = GetDataItemsAsync;
            Open = OpenAsync;
        }

        public Open Open { get; }
        public GetDataItems GetDataItems { get; }
        public Close Close { get; }

        private async Task<DataItem[]> GetDataItemsAsync(object[] itemNameAndIdentifierTuples)
        {
            var itemNamesAndIdentifiers = itemNameAndIdentifierTuples.Cast<(string itemName, string[] identifiers)>().ToArray();
            const string defaultIdentifierName = "Name";
            var result = new List<DataItem>();

            foreach (var itemNameAndIdentifier in itemNamesAndIdentifiers)
            {
                var itemName = itemNameAndIdentifier.itemName;
                var identifierNames = itemNameAndIdentifier.identifiers;
                if (identifierNames?.Any() != true)
                    identifierNames = new[] {defaultIdentifierName};

                var queryText = $"SELECT * FROM {itemName}";
                using (var managementObjectSearcher = new ManagementObjectSearcher(queryText))
                using (var mosResult = managementObjectSearcher.Get())
                {
                    foreach (var managementBaseObject in mosResult)
                    {
                        Source source = new Source();
                        var identifierProps = managementBaseObject.Properties
                            .Cast<PropertyData>()
                            .Where(pd => identifierNames.Contains(pd.Name))
                            .OrderBy(pd => Array.IndexOf(identifierNames, pd.Name)).ToArray();
                        source.SourceName = itemName;
                        var categoryValues = identifierProps.ToDictionary(pd => pd.Name,
                            pd => new[]
                            {
                                new DataValue() {Object = pd.Value, Type = pd.Value?.GetType() ?? typeof(object)}
                            });
                        var dataItems = getDataItems(managementBaseObject, source).ToArray();
                        foreach (var item in dataItems)
                        {
                            item.CategoryValues = categoryValues.ToDictionary(kv => kv.Key, kv => kv.Value);
                            //item.CategoryValues.Add("MetricName",
                            //    new[] {new DataValue() {Object = item.Name, Type = item.Name?.GetType()}});
                        }

                        result.AddRange(dataItems);
                    }
                }
            }

            return result.ToArray();
        }

        private IEnumerable<DataItem> getDataItems(ManagementBaseObject mObj, Source source)
        {
            var result = mObj.Properties.Cast<PropertyData>()
                .Where(pd => pd.Value != null)
                .Select(pd =>
            {
                var dataItem = new DataItem
                {
                    Name = pd.Name,
                    Category = pd.Origin,
                    Value = new DataValue() {
                        Type = pd.Value.GetType(), 
                        Object = pd.Value
                    },
                    Source = source
                };
                return dataItem;
            });
            return result;
        }

        private async Task OpenAsync(object[] parameters)
        {
        }

        private async Task CloseAsync(object[] parameters)
        {
        }

        private async Task<DataItem[]> getDriveData()
        {
			var driveQuery = new ManagementObjectSearcher("select * from Win32_DiskDrive");
			foreach (ManagementObject d in driveQuery.Get())
			{
				var deviceId = d.Properties["DeviceId"].Value;
				//Console.WriteLine("Device");
				//Console.WriteLine(d);
				var partitionQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_DiskDriveToDiskPartition", d.Path.RelativePath);
				var partitionQuery = new ManagementObjectSearcher(partitionQueryText);
				foreach (ManagementObject p in partitionQuery.Get())
				{
					//Console.WriteLine("Partition");
					//Console.WriteLine(p);
					var logicalDriveQueryText = string.Format("associators of {{{0}}} where AssocClass = Win32_LogicalDiskToPartition", p.Path.RelativePath);
					var logicalDriveQuery = new ManagementObjectSearcher(logicalDriveQueryText);
					foreach (ManagementObject ld in logicalDriveQuery.Get())
					{
						//Console.WriteLine("Logical drive");
						//Console.WriteLine(ld);

						var physicalName = Convert.ToString(d.Properties["Name"].Value); // \\.\PHYSICALDRIVE2
						var diskName = Convert.ToString(d.Properties["Caption"].Value); // WDC WD5001AALS-xxxxxx
						var diskModel = Convert.ToString(d.Properties["Model"].Value); // WDC WD5001AALS-xxxxxx
						var diskInterface = Convert.ToString(d.Properties["InterfaceType"].Value); // IDE
						var capabilities = (UInt16[])d.Properties["Capabilities"].Value; // 3,4 - random access, supports writing
						var mediaLoaded = Convert.ToBoolean(d.Properties["MediaLoaded"].Value); // bool
						var mediaType = Convert.ToString(d.Properties["MediaType"].Value); // Fixed hard disk media
						var mediaSignature = Convert.ToUInt32(d.Properties["Signature"].Value); // int32
						var mediaStatus = Convert.ToString(d.Properties["Status"].Value); // OK

						var driveName = Convert.ToString(ld.Properties["Name"].Value); // C:
						var driveId = Convert.ToString(ld.Properties["DeviceId"].Value); // C:
						var driveCompressed = Convert.ToBoolean(ld.Properties["Compressed"].Value);
						var driveType = Convert.ToUInt32(ld.Properties["DriveType"].Value); // C: - 3
						var fileSystem = Convert.ToString(ld.Properties["FileSystem"].Value); // NTFS
						var freeSpace = Convert.ToUInt64(ld.Properties["FreeSpace"].Value); // in bytes
						var totalSpace = Convert.ToUInt64(ld.Properties["Size"].Value); // in bytes
						var driveMediaType = Convert.ToUInt32(ld.Properties["MediaType"].Value); // c: 12
						var volumeName = Convert.ToString(ld.Properties["VolumeName"].Value); // System
						var volumeSerial = Convert.ToString(ld.Properties["VolumeSerialNumber"].Value); // 12345678

						Console.WriteLine("PhysicalName: {0}", physicalName);
						Console.WriteLine("DiskName: {0}", diskName);
						Console.WriteLine("DiskModel: {0}", diskModel);
						Console.WriteLine("DiskInterface: {0}", diskInterface);
						// Console.WriteLine("Capabilities: {0}", capabilities);
						Console.WriteLine("MediaLoaded: {0}", mediaLoaded);
						Console.WriteLine("MediaType: {0}", mediaType);
						Console.WriteLine("MediaSignature: {0}", mediaSignature);
						Console.WriteLine("MediaStatus: {0}", mediaStatus);

						Console.WriteLine("DriveName: {0}", driveName);
						Console.WriteLine("DriveId: {0}", driveId);
						Console.WriteLine("DriveCompressed: {0}", driveCompressed);
						Console.WriteLine("DriveType: {0}", driveType);
						Console.WriteLine("FileSystem: {0}", fileSystem);
						Console.WriteLine("FreeSpace: {0}", freeSpace);
						Console.WriteLine("TotalSpace: {0}", totalSpace);
						Console.WriteLine("DriveMediaType: {0}", driveMediaType);
						Console.WriteLine("VolumeName: {0}", volumeName);
						Console.WriteLine("VolumeSerial: {0}", volumeSerial);

						Console.WriteLine(new string('-', 79));
					}
				}
			}

            return null;
        }

    }
}
