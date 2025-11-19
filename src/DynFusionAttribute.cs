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

		public DynFusionDigitalAttribute(string name, uint joinNumber)
			: base(name, eSigType.Bool, joinNumber)
		{
			BoolValueFeedback = new BoolFeedback(() => { return BoolValue; });
			Debug.LogVerbose("Creating DigitalAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);
		}

		public DynFusionDigitalAttribute(string name, uint joinNumber, string deviceKey, string boolAction, string boolFeedback)
			: base(name, eSigType.Bool, joinNumber)
		{

			BoolValueFeedback = new BoolFeedback(() => { return BoolValue; });
			Debug.LogVerbose("Creating DigitalAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);


			if (!string.IsNullOrEmpty(deviceKey))
			{
				_devicekey = deviceKey;
				if (!string.IsNullOrEmpty(boolAction))
				{
					_action = boolAction;
				}

				if (!string.IsNullOrEmpty(boolFeedback))
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, boolFeedback) as BoolFeedback;
						if (fb != null)
						{
							fb.OutputChange += ((sender, args) =>
							{
								BoolValue = args.BoolValue;
							});
						}
					}
					catch (Exception ex)
					{
						Debug.LogError("DynFuison Issue linking Device {deviceKey} BoolFB {value}: {message}", deviceKey, boolFeedback, ex.Message);
						Debug.LogDebug(ex, "Stack Trace: ");
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
				Debug.LogDebug("Changed Value of DigitalAttribute {0} {1} {2}", JoinNumber, Name, value);

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
		public DynFusionAnalogAttribute(string name, uint joinNumber)
			: base(name, eSigType.UShort, joinNumber)
		{
			UShortValueFeedback = new IntFeedback(() => { return (int)UShortValue; });

			Debug.LogVerbose("Creating AnalogAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);
		}

		public DynFusionAnalogAttribute(string name, uint joinNumber, string deviceKey, string intAction, string intFeedback)
			: base(name, eSigType.UShort, joinNumber)
		{
			UShortValueFeedback = new IntFeedback(() => { return (int)UShortValue; });
			Debug.LogVerbose("Creating AnalogAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);

			if (!string.IsNullOrEmpty(deviceKey))
			{
				_devicekey = deviceKey;
				if (!string.IsNullOrEmpty(intAction))
				{
					_action = intAction;
				}

				if (!string.IsNullOrEmpty(intFeedback))
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, intFeedback) as IntFeedback;
						if (fb != null)
						{
							fb.OutputChange += ((sender, args) =>
							{
								UShortValue = args.UShortValue;
							});
						}
					}
					catch (Exception ex)
					{
						Debug.LogError("DynFusion Issue linking Device {deviceKey} BoolFB {value}: {message}", deviceKey, intFeedback, ex.Message);
						Debug.LogDebug(ex, "Stack Trace: ");
					}

				}
			}
		}
		private string _devicekey { get; set; }
		private string _action { get; set; }

		public IntFeedback UShortValueFeedback { get; set; }
		private uint _UShortValue { get; set; }
		public uint UShortValue
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
		public DynFusionSerialAttribute(string name, uint joinNumber)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => { return StringValue; });

			Debug.LogVerbose("Creating StringAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);
		}

		public DynFusionSerialAttribute(string name, uint joinNumber, string deviceKey, string stringAction, string stringFeedback)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => { return StringValue; });
			Debug.LogVerbose("Creating StringAttribute {joinNumber} {joinName} {rwType}", JoinNumber, Name, RwType);

			if (!string.IsNullOrEmpty(deviceKey))
			{
				_devicekey = deviceKey;
				if (!string.IsNullOrEmpty(stringAction))
				{
					_action = stringAction;
				}

				if (!string.IsNullOrEmpty(stringFeedback))
				{
					try
					{
						var fb = DeviceJsonApi.GetPropertyByName(deviceKey, stringFeedback) as StringFeedback;
						if (fb != null)
						{
							fb.OutputChange += ((sender, args) =>
							{
								StringValue = args.StringValue;
							});
						}
					}
					catch (Exception ex)
					{
						Debug.LogError("DynFusion Issue linking Device {deviceKey} BoolFB {value}: {message}", deviceKey, stringFeedback, ex.Message);
						Debug.LogDebug(ex, "Stack Trace: ");
					}

				}
			}
		}
		private string _devicekey { get; set; }
		private string _action { get; set; }

		public StringFeedback StringValueFeedback { get; set; }
		private string _StringValue { get; set; }
		public string StringValue
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
	public class DynFusionAttributeBase
	{
		public DynFusionAttributeBase(string name, eSigType type, uint joinNumber)
		{
			Name = name;
			SignalType = type;
			JoinNumber = joinNumber;

		}

		[JsonProperty("SignalType")]
		[JsonConverter(typeof(StringEnumConverter))]
		public eSigType SignalType { get; set; }

		[JsonProperty("JoinNumber")]
		public uint JoinNumber { get; set; }

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