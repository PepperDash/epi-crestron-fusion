using System.Collections.Generic;

namespace DynFusion.Config
{
    public class FusionStaticAssetConfig
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public CustomAttributes Attributes { get; set; }
    }
}