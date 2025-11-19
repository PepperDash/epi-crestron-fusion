using System.Collections.Generic;
using Newtonsoft.Json;
using PepperDash.Core;

namespace DynFusion.Config
{
	public class FusionStaticAssetConfig
	{
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("attributeJoinOffset")]
		public uint AttributeJoinOffset { get; set; }

		[JsonProperty("customAttributeJoinOffset")]
		public uint CustomAttributeJoinOffset { get; set; }

		[JsonProperty("Make")]
		public string Make { get; set; }

		[JsonProperty("Model")]
		public string Model { get; set; }

		[JsonProperty("attributes")]
		public CustomAttributes Attributes { get; set; }

		[JsonProperty("customAttributes")]
		public CustomAttributes CustomAttributes { get; set; }
	}
}