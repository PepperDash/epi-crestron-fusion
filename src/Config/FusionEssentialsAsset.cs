using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Config
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