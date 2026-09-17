using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Config
{
    public class FusionCustomProperty
    {
		[JsonProperty("id")]
        public string Id { get; set; }

		[JsonProperty("joinNumber")]
        public uint JoinNumber { get; set; }
    }
}