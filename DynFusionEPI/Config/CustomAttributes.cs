using System.Collections.Generic;
using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class CustomAttributes
    {
		[JsonProperty("digitalAttributes")]
        public List<DynFusionAttributeBase> DigitalAttributes {get; set;}

		[JsonProperty("analogAttributes")]
        public List<DynFusionAttributeBase> AnalogAttributes { get; set; }

		[JsonProperty("serialAttributes")]
        public List<DynFusionAttributeBase> SerialAttributes { get; set; }
    }
}