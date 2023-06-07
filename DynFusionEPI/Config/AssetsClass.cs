using System.Collections.Generic;

namespace DynFusion.Config
{
    public class AssetsClass
    {
        public List<FusionOccupancyAsset> OccupancySensors { get; set; }
        public List<FusionEssentialsAsset> AnalogLinks { get; set; }
        public List<FusionEssentialsAsset> SerialLinks { get; set; }
        public List<FusionStaticAssetConfig> StaticAssets { get; set; } 
    }
}