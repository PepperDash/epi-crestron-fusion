using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class DeviceUsageSource
    {
		[JsonProperty("name")]
        public string Name { get; set; }

		[JsonProperty("type")]
        public string Type { get; set; }

		[JsonProperty("sourceNumber")]
        public ushort SourceNumber { get; set; }
    }
}