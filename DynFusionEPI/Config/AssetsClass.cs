using System.Collections.Generic;
using Newtonsoft.Json;

namespace DynFusion.Config
{
    public class AssetsClass
    {
		[JsonProperty("occupancySensors")]
        public List<FusionOccupancyAsset> OccupancySensors { get; set; }

		[JsonProperty("analogLinks")]
        public List<FusionEssentialsAsset> AnalogLinks { get; set; }

		[JsonProperty("serialLinks")]
        public List<FusionEssentialsAsset> SerialLinks { get; set; }
    }
}