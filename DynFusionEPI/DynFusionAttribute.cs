using System;
using Crestron.SimplSharp.Reflection;
using PepperDash.Essentials.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using PepperDash.Essentials.Core.DeviceInfo;

namespace DynFusion
{


	public class DynFusionDigitalAttribute : DynFusionAttributeBase
	{

	    public DynFusionDigitalAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.Bool, joinNumber)
		{
			BoolValueFeedback = new BoolFeedback(() => BoolValue);
            Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);
		}
        public DynFusionDigitalAttribute(DynFusionAttributeBase config)
            : base(config.Name, eSigType.Bool, config.JoinNumber, config.RwType, config.BridgeJoin, config.IsStaticData)
        {
            Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);

            LinkDeviceKey = config.LinkDeviceKey;
            LinkDeviceFeedback = config.LinkDeviceFeedback;
            LinkDeviceMethod = config.LinkDeviceMethod;
            InvertFeedback = config.InvertFeedback;
            BoolValueFeedback = new BoolFeedback(() => BoolValue);
            LinkStaticData(config);
		}

	    public override sealed void LinkStaticData(DynFusionAttributeBase config)
	    {
	        BoolValue = config.StaticDigital;
            BoolValueFeedback.FireUpdate();
	    }

	    public override void LinkData()
	    {
	        if (IsStaticData)
	        {
	            return;
	        }
	        if (string.IsNullOrEmpty(LinkDeviceKey)) return;
	        if (!string.IsNullOrEmpty(LinkDeviceFeedback))
	        {
	            try
	            {
	                var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as BoolFeedback;
	                if (fb == null)
	                {
	                    Debug.Console(0, "Unable to link Feedback for digital attribute {0}", Name);
	                    return;
	                }
	                fb.OutputChange += ((sender, args) =>
	                {
	                    BoolValue = InvertFeedback ? !args.BoolValue : args.BoolValue;
                        BoolValueFeedback.FireUpdate();
	                });
	            }
	            catch (Exception ex)
	            {
	                Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFusion Issue linking Device {0} BoolFB {1}\n{2}",
	                    LinkDeviceKey, LinkDeviceFeedback, ex);
	            }
	        }

	        if (!string.IsNullOrEmpty(LinkDeviceMethod))
	        {
	            try
	            {
	                var actionWrapper = new DeviceActionWrapper
	                {
	                    DeviceKey = LinkDeviceKey,
	                    MethodName = LinkDeviceMethod

	                };
	                AttributeAction = () => DeviceJsonApi.DoDeviceAction(actionWrapper);
	            }
	            catch (Exception ex)
	            {
	                Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to execute Action {1} on Device {0}\n{2}",
	                    LinkDeviceKey,
	                    LinkDeviceMethod, ex);
	            }
	        }
	    }

        public BoolFeedback BoolValueFeedback { get; set; }
        public bool BoolValue { get; set; }

	}


	public class DynFusionAnalogAttribute : DynFusionAttributeBase
	{
        public DynFusionAnalogAttribute(string name, UInt32 joinNumber)
            : base(name, eSigType.UShort, joinNumber)
        {
            UShortValueFeedback = new IntFeedback(() => (int)UShortValue);

            Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);
        }

        public DynFusionAnalogAttribute(DynFusionAttributeBase config)
            : base(config.Name, eSigType.UShort, config.JoinNumber, config.RwType, config.BridgeJoin, config.IsStaticData)
	    {
            LinkDeviceKey = config.LinkDeviceKey;
            LinkDeviceFeedback = config.LinkDeviceFeedback;
            LinkDeviceMethod = config.LinkDeviceMethod;
            UShortValueFeedback = new IntFeedback(() => (int)UShortValue);
            IsDeviceInfo = config.IsDeviceInfo;
            LinkStaticData(config);


            Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);

	    }

        public override sealed void LinkStaticData(DynFusionAttributeBase config)
        {
            UShortValue = config.StaticAnalog;
            UShortValueFeedback.FireUpdate();
        }


        public override void LinkData()
        {
            if (IsStaticData)
            {
                return;
            }
            if (string.IsNullOrEmpty(LinkDeviceKey)) return;
            if (!string.IsNullOrEmpty(LinkDeviceFeedback))
            {
                try
                {
                    var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as IntFeedback;
                    if (fb == null)
                    {
                        Debug.Console(0, "Unable to link Feedback for analog attribute {0}", Name);
                        return;
                    }
                    fb.OutputChange += ((sender, args) =>
                    {
                        UShortValue = args.UShortValue;
                        UShortValueFeedback.FireUpdate();
                        Debug.Console(2, "AnalogAttribute {0} from device {2} = {1}", Name, UShortValue, LinkDeviceKey);
                    });
                }
                catch (Exception ex)
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFusion Issue linking Device {0} UShortFb {1}\n{2}",
                        LinkDeviceKey, LinkDeviceFeedback, ex);
                }
            }
            if (!string.IsNullOrEmpty(LinkDeviceMethod))
            {
                try
                {
                    var actionWrapper = new DeviceActionWrapper
                    {
                        DeviceKey = LinkDeviceKey,
                        MethodName = LinkDeviceMethod

                    };
                    AttributeAction = () => DeviceJsonApi.DoDeviceAction(actionWrapper);
                }
                catch (Exception ex)
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to execute Action {1} on Device {0}\n{2}", LinkDeviceKey,
                        LinkDeviceMethod, ex);
                }
            }

        }


		public IntFeedback UShortValueFeedback { get; set; }
		public UInt32 UShortValue { get; set; }
	}


	public class DynFusionSerialAttribute : DynFusionAttributeBase
	{
		public DynFusionSerialAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => StringValue);

            Debug.Console(2, "Creating SerialAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);
		}

	    public DynFusionSerialAttribute(DynFusionAttributeBase config)
            : base(config.Name, eSigType.String, config.JoinNumber, config.RwType, config.BridgeJoin, config.IsStaticData)
	    {
	        Debug.Console(2, "Creating SerialAttribute {0} {1} {2}", JoinNumber, Name, (int) RwType);

	        LinkDeviceKey = config.LinkDeviceKey;
	        LinkDeviceFeedback = config.LinkDeviceFeedback;
	        LinkDeviceMethod = config.LinkDeviceMethod;
            StringValueFeedback = new StringFeedback(() => StringValue);
            LinkStaticData(config);
	    }

        public override sealed void LinkStaticData(DynFusionAttributeBase config)
        {
            StringValue = config.StaticSerial;
            StringValueFeedback.FireUpdate();
        }

	    public override void LinkData()
        {
            if (IsStaticData)
            {
                return;
            }

            if (string.IsNullOrEmpty(LinkDeviceKey)) return;
            if (IsDeviceInfo)
            {
                LinkDeviceInfo();
                return;
            }
            if (!string.IsNullOrEmpty(LinkDeviceFeedback))
            {
                try
                {
                    var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as StringFeedback;
                    if (fb == null)
                    {
                        Debug.Console(0, "Unable to link Feedback for serial attribute {0}", Name);
                        return;
                    }
                    fb.OutputChange += ((sender, args) =>
                    {
                        StringValue = args.StringValue;
                        StringValueFeedback.FireUpdate();
                        Debug.Console(2, "SerialAttribute {0} from device {2} = {1}", Name, StringValue, LinkDeviceKey);
                    });
                }
                catch (Exception ex)
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFusion Issue linking Device {0} StringFb {1}\n{2}",
                        LinkDeviceKey, LinkDeviceFeedback, ex);
                }
            }
            if (!string.IsNullOrEmpty(LinkDeviceMethod))
            {
                try
                {
                    var actionWrapper = new DeviceActionWrapper
                    {
                        DeviceKey = LinkDeviceKey,
                        MethodName = LinkDeviceMethod

                    };
                    AttributeAction = () => DeviceJsonApi.DoDeviceAction(actionWrapper);
                }
                catch (Exception ex)
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to execute Action {1} on Device {0}\n{2}", LinkDeviceKey,
                        LinkDeviceMethod, ex);
                }
            }
        }

        private void LinkDeviceInfo()
        {
            try
            {
                if (string.IsNullOrEmpty(LinkDeviceFeedback)) return;
                var device = DeviceManager.GetDeviceForKey(LinkDeviceKey);
                if (device == null) return;
                var infoProvider = device as IDeviceInfoProvider;
                if (infoProvider == null) return;
                infoProvider.DeviceInfoChanged += (s, a) =>
                {
                    var devInfo = a.DeviceInfo;
                    var props = devInfo.GetType().GetCType().GetProperties();

                    foreach (var prop in props)
                    {
                        var name = prop.Name;
                        if (name.ToLower() != LinkDeviceFeedback) continue;
                        var propValue = (string) prop.GetValue(devInfo, null);
                        if (string.IsNullOrEmpty(propValue)) return;
                        StringValue = propValue;
                        StringValueFeedback.FireUpdate();
                        return;
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Error, "Unable to link to DeviceInfo {1} on Device {0}\n{2}", LinkDeviceKey,
                    LinkDeviceFeedback, ex);
            }
        }


		public StringFeedback StringValueFeedback { get; set; }
		public string StringValue { get; set; }

	}
	
    
    public class DynFusionAttributeBase
	{
        [JsonConstructor]
        public DynFusionAttributeBase(string name, eSigType type, UInt32 joinNumber)
        {
            Name = name;
            SignalType = type;
            JoinNumber = joinNumber;
        }
        public DynFusionAttributeBase(string name, eSigType type, UInt32 joinNumber, eReadWrite rwType)
        {
            Name = name;
            SignalType = type;
            JoinNumber = joinNumber;
            RwType = rwType;
        }
        public DynFusionAttributeBase(string name, eSigType type, UInt32 joinNumber, eReadWrite rwType, ushort bridgeJoin)
        {
            Name = name;
            SignalType = type;
            JoinNumber = joinNumber;
            RwType = rwType;
            BridgeJoin = bridgeJoin;
        }
        public DynFusionAttributeBase(string name, eSigType type, UInt32 joinNumber, eReadWrite rwType, ushort bridgeJoin, bool isStaticData)
        {
            Name = name;
            SignalType = type;
            JoinNumber = joinNumber;
            RwType = rwType;
            BridgeJoin = bridgeJoin;
            IsStaticData = isStaticData;
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

        [JsonProperty("invertFeedback")]
        public bool InvertFeedback { get; set; }

        [JsonProperty("bridgeJoin")]
        public ushort BridgeJoin { get; set; }

        [JsonProperty("isDeviceInfo")]
        public bool IsDeviceInfo { get; set; }

        [JsonProperty("isStaticData")]
        public bool IsStaticData { get; set; }

        [JsonProperty("staticDigital")]
        public bool StaticDigital { get; set; }

        [JsonProperty("staticAnalog")]
        public ushort StaticAnalog { get; set; }

        [JsonProperty("staticSerial")]
        public string StaticSerial { get; set; }


	    public virtual void ExecuteAction()
	    {
	        AttributeAction.Invoke();
	    }

	    public Action AttributeAction;

        public virtual void LinkData()
        {
            Debug.Console(0, "LinkData in base Attribute");
        }

        public virtual void LinkStaticData(DynFusionAttributeBase config)
        {
            Debug.Console(0, "Link Static Data");
        }


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