using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Config
{
    public class FusionOccupancyAsset
    {
		[JsonProperty("key")]
        public string Key { get; set; }

		[JsonProperty("LinkToDeviceKey")]
        public string LinkToDeviceKey { get; set; }
    }
}