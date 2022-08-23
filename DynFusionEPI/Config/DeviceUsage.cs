using System.Collections.Generic;

namespace DynFusion.Config
{
    public class DeviceUsage
    {
        public int UsageMinThreshol { get; set; }
        public List<DeviceUsageDevice> Devices { get; set; }
        public List<DeviceUsageSoruce> Sources { get; set; }
        public List<DisplayUsageDevice> Displays { get; set; }

        public DeviceUsage()
        {
            Devices = new List<DeviceUsageDevice>();
            Sources = new List<DeviceUsageSoruce>();
            Displays = new List<DisplayUsageDevice>();
        }
    }
}