using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class FusionOccupancyAsset
    {
		[JsonProperty("key")]
        public string Key { get; set; }

		[JsonProperty("LinkToDeviceKey")]
        public string LinkToDeviceKey { get; set; }
    }
}