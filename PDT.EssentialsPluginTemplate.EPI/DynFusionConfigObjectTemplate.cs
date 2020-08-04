using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PDTDynFusionEPI
{
	public class DynFusionConfigObjectTemplate
	{
		public EssentialsControlPropertiesConfig control {get; set;}

		[JsonProperty("customAttributes")]
		public CustomAttributes CustomAttributes { get; set; }


	}

	public class CustomAttributes
	{
		public List<DynFusionAttributeBase> DigitalAttributes {get; set;}
		public List<DynFusionAttributeBase> AnalogAttributes { get; set; }
		public List<DynFusionAttributeBase> SerialAttributes { get; set; }
	}
}