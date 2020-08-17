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

		[JsonProperty("customProperties")]
		public CustomProperties CustomProperties { get; set; } 

	}

	public class CustomAttributes
	{
		public List<DynFusionAttributeBase> DigitalAttributes {get; set;}
		public List<DynFusionAttributeBase> AnalogAttributes { get; set; }
		public List<DynFusionAttributeBase> SerialAttributes { get; set; }
	}
	public class CustomProperties
	{
		public List<FusionCustomProperty> DigitalProperties { get; set; }
		public List<FusionCustomProperty> AnalogProperties { get; set; }
		public List<FusionCustomProperty> SerialProperties { get; set; }
	}

	public class FusionCustomProperty
	{
		[JsonProperty("ID")]
		public string ID { get; set; }
		[JsonProperty("JoinNumber")]
		public UInt32 JoinNumber { get; set; }

	}
}