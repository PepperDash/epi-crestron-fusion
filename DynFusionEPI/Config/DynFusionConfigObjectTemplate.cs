using PepperDash.Essentials.Core;

namespace DynFusion.Config
{
	public class DynFusionConfigObjectTemplate
	{
		public EssentialsControlPropertiesConfig Control {get; set;}
		public CustomAttributes CustomAttributes { get; set; }
		public CustomProperties CustomProperties { get; set; }
		public AssetsClass Assets { get; set; }
		public DeviceUsage DeviceUsage { get; set; } 
	}
}