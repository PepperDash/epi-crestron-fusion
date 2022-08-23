using System.Collections.Generic;

namespace DynFusion.Config
{
    public class DeviceUsage
    {
        public int UsageMinThreshold { get; set; }
        public List<DeviceUsageDevice> Devices { get; set; }
        public List<DeviceUsageSource> Sources { get; set; }
        public List<DisplayUsageDevice> Displays { get; set; }

        public DeviceUsage()
        {
            Devices = new List<DeviceUsageDevice>();
            Sources = new List<DeviceUsageSource>();
            Displays = new List<DisplayUsageDevice>();
        }
    }
}