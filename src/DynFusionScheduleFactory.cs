using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace DynFusion
{
    public class DynFusionScheduleFactory : EssentialsPluginDeviceFactory<DynFusionDevice>
    {
        public DynFusionScheduleFactory()
        {
            // Set the minimum Essentials Framework Version
            MinimumEssentialsFrameworkVersion = "1.5.5";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
            TypeNames = new List<string>() { "DynFusionSchedule" };
        }

        // Builds and returns an instance of EssentialsPluginDeviceTemplate
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.LogDebug("Factory Attempting to create new device from type: {type}", dc.Type);


            var propertiesConfig = dc.Properties.ToObject<SchedulingConfig>();
            return new DynFusionSchedule(dc.Key, dc.Name, propertiesConfig);
        }

    }
}