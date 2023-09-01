using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class DeviceUsageDevice
    {
		[JsonProperty("name")]
        public string Name { get; set; }

		[JsonProperty("type")]
        public string Type { get; set; }

		[JsonProperty("joinNumber")]
        public uint JoinNumber { get; set; }
    }
}