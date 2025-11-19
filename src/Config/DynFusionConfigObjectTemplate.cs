using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace DynFusion.Config
{
	public class DynFusionConfigObjectTemplate
	{
		[JsonProperty("control")]
		public EssentialsControlPropertiesConfig Control {get; set;}

		[JsonProperty("customAttributes")]
		public CustomAttributes CustomAttributes { get; set; }

		[JsonProperty("customProperties")]
		public CustomProperties CustomProperties { get; set; }

		[JsonProperty("assets")]
		public AssetsClass Assets { get; set; }

		[JsonProperty("deviceUsage")]
		public DeviceUsage DeviceUsage { get; set; }

		[JsonProperty("callStatistics")]
        public CallStatistics CallStatistics { get; set; }
	}
}