using System.Collections.Generic;
using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class DeviceUsage
    {
        [JsonProperty("usageMinThreshold")]
        public int UsageMinThreshold { get; set; }
        [JsonProperty("devices")]
        public List<DeviceUsageDevice> Devices { get; set; }
        [JsonProperty("sources")]
        public List<DeviceUsageSource> Sources { get; set; }
        [JsonProperty("displays")]
        public List<DisplayUsageDevice> Displays { get; set; }

        public DeviceUsage()
        {
            Devices = new List<DeviceUsageDevice>();
            Sources = new List<DeviceUsageSource>();
            Displays = new List<DisplayUsageDevice>();
        }
    }
}