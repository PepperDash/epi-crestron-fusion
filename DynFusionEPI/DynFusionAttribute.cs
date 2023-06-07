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

		public DynFusionDigitalAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.Bool, joinNumber)
		{
			BoolValueFeedback = new BoolFeedback(() => BoolValue);
            Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);
		}
        public DynFusionDigitalAttribute(DynFusionAttributeBase config)
            : base(config.Name, eSigType.Bool, config.JoinNumber, config.RwType)
        {
            Debug.Console(2, "Creating DigitalAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);

            LinkDeviceKey = config.LinkDeviceKey;
            LinkDeviceFeedback = config.LinkDeviceFeedback;
            LinkDeviceMethod = config.LinkDeviceMethod;
            InvertFeedback = config.InvertFeedback;


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
				Debug.Console(2, "Changed Value of DigitalAttribute {0} {1} {2}", JoinNumber, Name, value);

			}
		}

	    public override void LinkData()
	    {
            Debug.Console(0, "Linking Digital Data for {0} : LinkDeviceKey - {1}, LinkDeviceFeedback - {2}, LinkDeviceMethod - {3}", Name, LinkDeviceKey, LinkDeviceFeedback, LinkDeviceMethod);
	        if (string.IsNullOrEmpty(LinkDeviceKey)) return;
	        if (!string.IsNullOrEmpty(LinkDeviceFeedback))
	        {
	            try
	            {
                    BoolValueFeedback = new BoolFeedback(() => BoolValue);
	                var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as BoolFeedback;
	                if (fb == null) return;
	                fb.OutputChange += ((sender, args) =>
	                {
	                    BoolValue = InvertFeedback ? !args.BoolValue : args.BoolValue;
	                    Debug.Console(2, "DigitalAttribute {0} from device {2} = {1}", Name, BoolValue, LinkDeviceKey);
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
	                var actionWrapper = new DeviceActionWrapper()
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
            : base(config.Name, eSigType.UShort, config.JoinNumber, config.RwType)
	    {
            LinkDeviceKey = config.LinkDeviceKey;
            LinkDeviceFeedback = config.LinkDeviceFeedback;
            LinkDeviceMethod = config.LinkDeviceMethod;


            Debug.Console(2, "Creating AnalogAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);

	    }

        public override void LinkData()
        {
            Debug.Console(0, "Linking Analog Data for {0} : LinkDeviceKey - {1}, LinkDeviceFeedback - {2}, LinkDeviceMethod - {3}", Name, LinkDeviceKey, LinkDeviceFeedback, LinkDeviceMethod);

            if (string.IsNullOrEmpty(LinkDeviceKey)) return;
            if (!string.IsNullOrEmpty(LinkDeviceFeedback))
            {
                try
                {
                    UShortValueFeedback = new IntFeedback(() => (int)UShortValue);

                    var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as IntFeedback;
                    if (fb == null) return;
                    fb.OutputChange += ((sender, args) =>
                    {
                        UShortValue = args.UShortValue;
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
                    var actionWrapper = new DeviceActionWrapper()
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
		public DynFusionSerialAttribute(string name, UInt32 joinNumber)
			: base(name, eSigType.String, joinNumber)
		{
			StringValueFeedback = new StringFeedback(() => StringValue);

            Debug.Console(2, "Creating SerialAttribute {0} {1} {2}", JoinNumber, Name, (int)RwType);
		}

	    public DynFusionSerialAttribute(DynFusionAttributeBase config)
	        : base(config.Name, eSigType.String, config.JoinNumber, config.RwType)
	    {
	        Debug.Console(2, "Creating SerialAttribute {0} {1} {2}", JoinNumber, Name, (int) RwType);

	        LinkDeviceKey = config.LinkDeviceKey;
	        LinkDeviceFeedback = config.LinkDeviceFeedback;
	        LinkDeviceMethod = config.LinkDeviceMethod;

	    }

	    public override void LinkData()
        {
            Debug.Console(0, "Linking Serial Data for {0} : LinkDeviceKey - {1}, LinkDeviceFeedback - {2}, LinkDeviceMethod - {3}", Name, LinkDeviceKey, LinkDeviceFeedback, LinkDeviceMethod);

            if (string.IsNullOrEmpty(LinkDeviceKey)) return;
            if (!string.IsNullOrEmpty(LinkDeviceFeedback))
            {
                try
                {
                    StringValueFeedback = new StringFeedback(() => StringValue);

                    var fb = DeviceJsonApi.GetPropertyByName(LinkDeviceKey, LinkDeviceFeedback) as StringFeedback;
                    if (fb == null) return;
                    fb.OutputChange += ((sender, args) =>
                    {
                        StringValue = args.StringValue;
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
                    var actionWrapper = new DeviceActionWrapper()
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

	    public virtual void ExecuteAction()
	    {
	        AttributeAction.Invoke();
	    }

	    public Action AttributeAction;

        public virtual void LinkData()
        {
            Debug.Console(0, "LinkData in base Attribute");
        }


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