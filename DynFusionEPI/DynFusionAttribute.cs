using System;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Crestron.SimplSharpPro;
using PepperDash.Core;

namespace DynFusion
{


	public class DynFusionDigitalAttribute : DynFusionAttributeBase
	{
	    private readonly string _boolAction;

	    public DynFusionDigitalAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.Bool, joinNumber)
		{
			BoolValueFeedback = new BoolFeedback(() => BoolValue);
			Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, RwType);
		}
		public DynFusionDigitalAttribute(string name, UInt32 joinNumber, string deviceKey, string boolAction, string boolFeedback)
			: base(name, eSigType.Bool, joinNumber)
		{
		    _boolAction = boolAction;

		    BoolValueFeedback = new BoolFeedback(() => BoolValue);
			Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, RwType);

		    if (deviceKey == null) return;
		    if (boolFeedback == null) return;
		    try
		    {
		        var fb = DeviceJsonApi.GetPropertyByName(deviceKey, boolFeedback) as BoolFeedback;
		        if (fb != null) fb.OutputChange += ((sender, args) => 
		        {
		            BoolValue = args.BoolValue;
		        });
		    }
		    catch (Exception ex)
		    {
		        Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFuison Issue linking Device {0} BoolFB {1}\n{2}", deviceKey, boolFeedback, ex);
		    }
		}
		public BoolFeedback BoolValueFeedback { get; set; }

	    private bool _boolValue;
		public bool BoolValue
		{
			get
			{
				
				return _boolValue;

			}
			set
			{
				_boolValue = value;
				BoolValueFeedback.FireUpdate();
				Debug.Console(2, "Changed Value of DigitalAttribute {0} {1} {2}", JoinNumber, Name, value);

			}
		}

	    public string BoolAction
	    {
	        get { return _boolAction; }
	    }
	}
	public class DynFusionAnalogAttribute : DynFusionAttributeBase
	{
		public DynFusionAnalogAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.UShort, joinNumber)
		{
			UShortValueFeedback = new IntFeedback( () => (int)UShortValue);

			Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", JoinNumber, Name, RwType);
		}

		public IntFeedback UShortValueFeedback { get; set; }
	    private UInt32 _uShortValue;
		public UInt32 UShortValue
		{
			get
			{
				return _uShortValue;

			}
			set
			{
				_uShortValue = value;
				UShortValueFeedback.FireUpdate();

			}
		}
	}
	public class DynFusionSerialAttribute : DynFusionAttributeBase
	{
		public DynFusionSerialAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => StringValue);

			Debug.Console(2, "Creating StringAttribute {0} {1} {2}", JoinNumber, Name, RwType);
		}
		public StringFeedback StringValueFeedback { get; set; }
	    private String _stringValue;
		public String StringValue
		{
			get
			{
				return _stringValue;

			}
			set
			{
				_stringValue = value;
				StringValueFeedback.FireUpdate();

			}

		}
	}
	public class DynFusionAttributeBase
	{
		public DynFusionAttributeBase (string name, eSigType type, UInt32 joinNumber)
		{
			Name = name; 
			SignalType = type;
			JoinNumber = joinNumber;

		}

		[JsonProperty("signalType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eSigType	SignalType { get; set; }

		[JsonProperty("joinNumber")]
		public UInt32		JoinNumber { get; set; }
		
		[JsonProperty("name")]
		public string		Name { get; set; }

		[JsonProperty("rwType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eReadWrite		RwType { get; set; }

		[JsonProperty("linkDeviceKey")]
		public string LinkDeviceKey { get; set; }

		[JsonProperty("linkDeviceMethod")]
		public string LinkDeviceMethod { get; set; }

		[JsonProperty("linkDeviceFeedback")]
		public string LinkDeviceFeedback { get; set; }


	}

    [Flags]
// ReSharper disable once InconsistentNaming
    public enum eReadWrite
	{
		Read = 1,
		Write = 2,
		R = 1, 
		W = 2,
		ReadWrite = 3,
		Rw = 3
	}
}