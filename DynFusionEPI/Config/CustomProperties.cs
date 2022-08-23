using System.Collections.Generic;

namespace DynFusion.Config
{
    public class CustomProperties
    {
        public List<FusionCustomProperty> DigitalProperties { get; set; }
        public List<FusionCustomProperty> AnalogProperties { get; set; }
        public List<FusionCustomProperty> SerialProperties { get; set; }
    }
}