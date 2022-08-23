using System.Collections.Generic;

namespace DynFusion.Config
{
    public class CallStatistics
    {
        public List<CallStatisticsDeviceConfig> Devices { get; set; }

        public CallStatistics()
        {
            Devices = new List<CallStatisticsDeviceConfig>();
        }
    }
}