using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Crestron.SimplSharpPro;
using PepperDash.Core;

namespace PDTDynFusionEPI
{

	public class DynFusionDigitalAttribute : DynFusionAttributeBase
	{
		public DynFusionDigitalAttribute(string name, UInt32 joinNumber, eReadWrite rw)
			: base(name, eSigType.Bool, joinNumber, rw)
		{
			BoolValueFeedback = new BoolFeedback(() => { return BoolValue; });
			Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}
		public BoolFeedback BoolValueFeedback { get; set; }
		private bool _BoolValue { get; set; }
		public bool BoolValue
		{
			get
			{
				
				return _BoolValue;

			}
			set
			{
				_BoolValue = value;
				BoolValueFeedback.FireUpdate();
				Debug.Console(2, "Changed Value of DigitalAttribute {0} {1} {2}", this.JoinNumber, this.Name, value);

			}
		}
	}
	public class DynFusionAnalogAttribute : DynFusionAttributeBase
	{
		public DynFusionAnalogAttribute(string name, UInt32 joinNumber, eReadWrite rw)
			: base(name, eSigType.UShort, joinNumber, rw)
		{
			UShortValueFeedback = new IntFeedback( () => { return (int)UShortValue; });

			Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}

		public IntFeedback UShortValueFeedback { get; set; }
		private UInt32 _UShortValue { get; set; }
		public UInt32 UShortValue
		{
			get
			{
				return _UShortValue;

			}
			set
			{
				_UShortValue = value;
				UShortValueFeedback.FireUpdate();

			}
		}
	}
	public class DynFusionSerialAttribute : DynFusionAttributeBase
	{
		public DynFusionSerialAttribute(string name, UInt32 joinNumber, eReadWrite rw)
			: base(name, eSigType.String, joinNumber, rw)
		{
			StringValueFeedback = new StringFeedback(() => { return StringValue; });

			Debug.Console(2, "Creating StringAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}
		public StringFeedback StringValueFeedback { get; set; }
		private String _StringValue { get; set; }
		public String StringValue
		{
			get
			{
				return _StringValue;

			}
			set
			{
				_StringValue = value;
				StringValueFeedback.FireUpdate();

			}
		}
	}
	public class DynFusionAttributeBase
	{
		public DynFusionAttributeBase (string name, eSigType type, UInt32 joinNumber, eReadWrite rw)
		{
			Name = name; 
			SignalType = type;
			JoinNumber = joinNumber;
			RwType = rw;
		}

		[JsonProperty("SignalType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eSigType	SignalType { get; set; }

		[JsonProperty("JoinNumber")]
		public UInt32		JoinNumber { get; set; }
		
		[JsonProperty("Name")]
		public string		Name { get; set; }

		[JsonProperty("RwType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eReadWrite		RwType { get; set; }


	}
	public enum eReadWrite
	{
		Read = 1,
		Write = 2,
		R = 1, 
		W = 2,
		ReadWrite = 3,
		RW = 3
	}
}