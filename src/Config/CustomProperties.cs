using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Config
{
    public class CustomProperties
    {
		[JsonProperty("digitalProperties")]
        public List<FusionCustomProperty> DigitalProperties { get; set; }

		[JsonProperty("analogProperties")]
        public List<FusionCustomProperty> AnalogProperties { get; set; }

		[JsonProperty("serialProperties")]
        public List<FusionCustomProperty> SerialProperties { get; set; }
    }
}