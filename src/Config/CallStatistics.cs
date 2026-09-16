using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Essentials.Plugins.Config
{
    public class CallStatistics
    {
		[JsonProperty("devices")]		
        public List<CallStatisticsDeviceConfig> Devices { get; set; }

        public CallStatistics()
        {
            Devices = new List<CallStatisticsDeviceConfig>();
        }
    }
}