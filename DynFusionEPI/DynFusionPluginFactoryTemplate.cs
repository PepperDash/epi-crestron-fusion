using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using DynFusion.Config;
using PepperDash.Core;
using PepperDash.Essentials.Core;

using Newtonsoft.Json;

namespace DynFusion
{
    public class EssentialsPluginFactoryTemplate : EssentialsPluginDeviceFactory<EssentialsBridgeableDevice>
    {
        public EssentialsPluginFactoryTemplate()
        {
            // Set the minimum Essentials Framework Version
            MinimumEssentialsFrameworkVersion = "1.5.5";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
            TypeNames = new List<string>() { "DynFusion", "DynFusionSchedule" };
        }

        // Builds and returns an instance of EssentialsPluginDeviceTemplate
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, "Factory Attempting to create new device from type: {0}", dc.Type);

            
			if (dc.Type == "DynFusion")
			{
				var propertiesConfig = dc.Properties.ToObject<DynFusionConfigObjectTemplate>();
				return new DynFusionDevice(dc.Key, dc.Name, propertiesConfig);
			}
			else if (dc.Type == "DynFusionSchedule")
			{
				var propertiesConfig = dc.Properties.ToObject<SchedulingConfig>();
				return new DynFusionSchedule(dc.Key, dc.Name, propertiesConfig);
			}
			else
			{
				return null;
			}
        }

    }
}