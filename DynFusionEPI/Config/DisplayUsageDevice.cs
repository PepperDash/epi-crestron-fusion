using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class DisplayUsageDevice
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("joinNumber")]
        public uint JoinNumber { get; set; }
    }
}