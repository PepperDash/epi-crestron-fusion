using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DynFusion
{
	public class DynFusionConfigObjectTemplate
	{
		public EssentialsControlPropertiesConfig control {get; set;}

		[JsonProperty("customAttributes")]
		public CustomAttributes CustomAttributes { get; set; }

		[JsonProperty("customProperties")]
		public CustomProperties CustomProperties { get; set; }

		[JsonProperty("Assets")]
		public AssetsClass Assets { get; set; }

		[JsonProperty("DeviceUsage")]
		public DeviceUsage DeviceUsage { get; set; } 

	}

	public class CustomAttributes
	{
		public List<DynFusionAttributeBase> DigitalAttributes {get; set;}
		public List<DynFusionAttributeBase> AnalogAttributes { get; set; }
		public List<DynFusionAttributeBase> SerialAttributes { get; set; }
	}
	public class CustomProperties
	{
		public List<FusionCustomProperty> DigitalProperties { get; set; }
		public List<FusionCustomProperty> AnalogProperties { get; set; }
		public List<FusionCustomProperty> SerialProperties { get; set; }
	}

	public class AssetsClass
	{
		public List<FusionOccupancyAsset> OccupancySensors { get; set; }
		public List<FusionEssentialsAsset> AnalogLinks { get; set; }
		public List<FusionEssentialsAsset> SerialLinks { get; set; }
	}

	public class FusionCustomProperty
	{
		[JsonProperty("ID")]
		public string ID { get; set; }
		[JsonProperty("JoinNumber")]
		public UInt32 JoinNumber { get; set; }

	}
	public class FusionOccupancyAsset
	{
		[JsonProperty("Key")]
		public string Key { get; set; }

		[JsonProperty("LinkToDeviceKey")]
		public string LinkToDeviceKey { get; set; }

	}
	public class FusionEssentialsAsset
	{
		[JsonProperty("DeviceKey")]
		public string DeviceKey { get; set; }

		[JsonProperty("JoinNumber")]
		public UInt32 JoinNumber { get; set; }

		[JsonProperty("FeedbackName")]
		public string Feedback { get; set; }

		[JsonProperty("Name")]
		public string Name { get; set; }
	}
	public class DeviceUsage
	{
		public int usageMinThreshold;
		public List<DeviceUsageDevice> Devices;
		public List<DeviceUsageSoruce> Sources;
		public List<DisplayUsageDevice> Displays;
	}
	public class DeviceUsageDevice
	{
		public string name;
		public string type;
		public uint joinNumber;
	}
	public class DisplayUsageDevice
	{
		public string name;
		public uint joinNumber;
	}
	public class DeviceUsageSoruce
	{
		public string name;
		public string type;
		public ushort sourceNumber;
	}
}