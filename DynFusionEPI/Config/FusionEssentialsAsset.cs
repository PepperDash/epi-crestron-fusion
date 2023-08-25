using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class FusionEssentialsAsset
    {
		[JsonProperty("deviceKey")]
        public string DeviceKey { get; set; }

		[JsonProperty("joinNumber")]
        public uint JoinNumber { get; set; }

		[JsonProperty("feedback")]
        public string Feedback { get; set; }

		[JsonProperty("name")]
        public string Name { get; set; }
    }
}