using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class CallStatisticsDeviceConfig
    {
		[JsonProperty("name")]
        public string Name { get; set; }

		[JsonProperty("type")]
        public string Type { get; set; }

		[JsonProperty("joinNumber")]
        public uint JoinNumber { get; set; }

		[JsonProperty("userCallTimer")]
        public bool UseCallTimer { get; set; }

		[JsonProperty("postMeetingId")]
        public bool PostMeetingId { get; set; }
    }
}