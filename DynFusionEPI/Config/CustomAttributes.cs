using System.Collections.Generic;

namespace DynFusion.Config
{
    public class CustomAttributes
    {
        public List<DynFusionAttributeBase> DigitalAttributes {get; set;}
        public List<DynFusionAttributeBase> AnalogAttributes { get; set; }
        public List<DynFusionAttributeBase> SerialAttributes { get; set; }
    }
}