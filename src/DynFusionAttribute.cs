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
using Crestron.SimplSharp.Reflection;
using Newtonsoft.Json;

namespace DynFusion
{


	public class DynFusionDigitalAttribute : DynFusionAttributeBase
	{

		public DynFusionDigitalAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.Bool, joinNumber)
		{
			BoolValueFeedback = new BoolFeedback(() => { return BoolValue; });
			Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}

		public DynFusionDigitalAttribute(string name, UInt32 joinNumber, string deviceKey, string boolAction, string boolFeedback)
			: base(name, eSigType.Bool, joinNumber)
		{
			
			BoolValueFeedback = new BoolFeedback(() => { return BoolValue; });
			Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);


			if (deviceKey != null)
			{
				_devicekey = deviceKey;
				if (boolAction != null)
				{
					_action = boolAction;
				}

				if (boolFeedback != null)
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, boolFeedback) as BoolFeedback;
						fb.OutputChange += ((sender, args) => 
						{
							this.BoolValue = args.BoolValue;
						});
					}
					catch (Exception ex)
					{
						Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFuison Issue linking Device {0} BoolFB {1}\n{2}", deviceKey, boolFeedback, ex);
					}

				}
			}

		}

		private string _devicekey { get; set; }
		private string _action { get; set; }
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

		public void CallAction(bool value)
		{
			if (_devicekey != null && _action != null)
			{
				var payload = new
				{
					deviceKey = _devicekey,
					methodName = _action,
					@params = new object[] { value }
				};
				string jsonString = JsonConvert.SerializeObject(payload);
				DeviceJsonApi.DoDeviceActionWithJson(jsonString);
			}
		}
	}
	public class DynFusionAnalogAttribute : DynFusionAttributeBase
	{
		public DynFusionAnalogAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.UShort, joinNumber)
		{
			UShortValueFeedback = new IntFeedback( () => { return (int)UShortValue; });

			Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}

		public DynFusionAnalogAttribute(string name, UInt32 joinNumber, string deviceKey, string intAction, string intFeedback)
			: base(name, eSigType.UShort, joinNumber)
		{
			Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);

			if (deviceKey != null)
			{
				_devicekey = deviceKey;
				if (intAction != null)
				{
					_action = intAction;
				}

				if (intFeedback != null)
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, intFeedback) as IntFeedback;
						fb.OutputChange += ((sender, args) =>
						{
							this.UShortValue = args.UShortValue;
						});
					}
					catch (Exception ex)
					{
						Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFuison Issue linking Device {0} BoolFB {1}\n{2}", deviceKey, intFeedback, ex);
					}

				}
			}
		}
		private string _devicekey { get; set; }
		private string _action { get; set; }

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

		public void CallAction(uint value)
		{
			if (_devicekey != null && _action != null)
			{
				var payload = new
				{
					deviceKey = _devicekey,
					methodName = _action,
					@params = new object[] { value }
				};
				string jsonString = JsonConvert.SerializeObject(payload);
				DeviceJsonApi.DoDeviceActionWithJson(jsonString);
			}
		}
	}
	public class DynFusionSerialAttribute : DynFusionAttributeBase
	{
		public DynFusionSerialAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => { return StringValue; });

			Debug.Console(2, "Creating StringAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);
		}

		public DynFusionSerialAttribute(string name, UInt32 joinNumber, string deviceKey, string stringAction, string stringFeedback)
			: base(name, eSigType.String, joinNumber)
		{
			Debug.Console(2, "Creating StringAttribute {0} {1} {2}", this.JoinNumber, this.Name, this.RwType);

			if (deviceKey != null)
			{
				_devicekey = deviceKey;
				if (stringAction != null)
				{
					_action = stringAction;
				}

				if (stringFeedback != null)
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, stringFeedback) as StringFeedback;
						fb.OutputChange += ((sender, args) =>
						{
							this.StringValue = args.StringValue;
						});
					}
					catch (Exception ex)
					{
						Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFuison Issue linking Device {0} BoolFB {1}\n{2}", deviceKey, stringFeedback, ex);
					}

				}
			}
		}
		private string _devicekey { get; set; }
		private string _action { get; set; }

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

		public void CallAction(string value)
		{
			if (this._devicekey != null && this._action != null)
            {
				var payload = new
				{
					deviceKey = _devicekey,
					methodName = _action,
					@params = new object[] { value }
				};
				string jsonString = JsonConvert.SerializeObject(payload);
				DeviceJsonApi.DoDeviceActionWithJson(jsonString);
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

		[JsonProperty("SignalType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eSigType	SignalType { get; set; }

		[JsonProperty("JoinNumber")]
		public UInt32 JoinNumber { get; set; }
		
		[JsonProperty("Name")]
		public string Name { get; set; }

		[JsonProperty("RwType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eReadWrite RwType { get; set; }

		[JsonProperty("LinkDeviceKey")]
		public string LinkDeviceKey { get; set; }

		[JsonProperty("LinkDeviceMethod")]
		public string LinkDeviceMethod { get; set; }

		[JsonProperty("LinkDeviceFeedback")]
		public string LinkDeviceFeedback { get; set; }


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