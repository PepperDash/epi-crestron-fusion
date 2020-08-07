// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text; 
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro;

namespace PDTDynFusionEPI 
{
	public class DynFusionDevice : EssentialsBridgeableDevice
	{
		public const ushort FusionJoinOffset = 49;
		//DynFusion Joins

		private DynFusionConfigObjectTemplate _Config;
		private Dictionary<UInt32, DynFusionDigitalAttribute> DigitalAttributesToFusion;
		private Dictionary<UInt32, DynFusionAnalogAttribute> AnalogAttributesToFusion;
		private Dictionary<UInt32, DynFusionSerialAttribute> SerialAttributesToFusion;
		private Dictionary<UInt32, DynFusionDigitalAttribute> DigitalAttributesFromFusion;
		private Dictionary<UInt32, DynFusionAnalogAttribute> AnalogAttributesFromFusion;
		private Dictionary<UInt32, DynFusionSerialAttribute> SerialAttributesFromFusion;
		private static DynFusionJoinMap JoinMapStatic;

		public BoolFeedback FusionOnlineFeedback;

		public FusionRoom FusionSymbol;

		public DynFusionDevice(string key, string name, DynFusionConfigObjectTemplate config)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new DynFusionDevice instance");
		    _Config = config;
			DigitalAttributesToFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
			AnalogAttributesToFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
			SerialAttributesToFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
			DigitalAttributesFromFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
			AnalogAttributesFromFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
			SerialAttributesFromFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
			JoinMapStatic = new DynFusionJoinMap(1);

		}

		public override bool CustomActivate()
		{

			Initialize();
			return true;
		}

		private void Initialize () 
		{
			try
			{

				Debug.Console(2, "Creating Fusion Symbol {0} {1}", _Config.control.IpId, Key);
				FusionSymbol = new FusionRoom(_Config.control.IpIdInt, Global.ControlSystem, "", Guid.NewGuid().ToString());
				if (FusionSymbol.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
				{
					Debug.Console(0, this, "Faliure to register Fusion Symbol");
				}

				FusionSymbol.ExtenderFusionRoomDataReservedSigs.Use();

				// Online Status 
				FusionOnlineFeedback = new BoolFeedback(() => { return FusionSymbol.IsOnline; });
				FusionSymbol.OnlineStatusChange += new OnlineStatusChangeEventHandler(FusionSymbol_OnlineStatusChange);
	
				// Attribute State Changes 
				FusionSymbol.FusionStateChange += new FusionStateEventHandler(FusionSymbol_FusionStateChange);
				//	FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(ExtenderFusionRoomDataReservedSigs_DeviceExtenderSigChange);

				// Create Custom Atributes 
				foreach (var att in _Config.CustomAttributes.DigitalAttributes)
				{
					FusionSymbol.AddSig(eSigType.Bool, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));

					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
					{
						DigitalAttributesToFusion.Add(att.JoinNumber, new DynFusionDigitalAttribute(att.Name, att.JoinNumber));	
						DigitalAttributesToFusion[att.JoinNumber].BoolValueFeedback.LinkInputSig(FusionSymbol.UserDefinedBooleanSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
					}
					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
					{
						DigitalAttributesFromFusion.Add(att.JoinNumber, new DynFusionDigitalAttribute(att.Name, att.JoinNumber));	
					}
				}
				
				foreach (var att in _Config.CustomAttributes.AnalogAttributes)
				{
					FusionSymbol.AddSig(eSigType.UShort, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));

					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
					{
						AnalogAttributesToFusion.Add(att.JoinNumber, new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
						AnalogAttributesToFusion[att.JoinNumber].UShortValueFeedback.LinkInputSig(FusionSymbol.UserDefinedUShortSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
					}
					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
					{
						AnalogAttributesFromFusion.Add(att.JoinNumber, new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
					}

				}
				foreach (var att in _Config.CustomAttributes.SerialAttributes)
				{
					FusionSymbol.AddSig(eSigType.String, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));
					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
					{
						SerialAttributesToFusion.Add(att.JoinNumber, new DynFusionSerialAttribute(att.Name, att.JoinNumber));
						SerialAttributesToFusion[att.JoinNumber].StringValueFeedback.LinkInputSig(FusionSymbol.UserDefinedStringSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
					}
					if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
					{
						SerialAttributesFromFusion.Add(att.JoinNumber, new DynFusionSerialAttribute(att.Name, att.JoinNumber));
					}

				}

				// Create Links for Statndard joins 
				CreateStandardJoin(JoinMapStatic.SystemPowerOn, FusionSymbol.SystemPowerOn);
				CreateStandardJoin(JoinMapStatic.SystemPowerOff, FusionSymbol.SystemPowerOff);
				CreateStandardJoin(JoinMapStatic.DisplayPowerOn, FusionSymbol.DisplayPowerOn);
				CreateStandardJoin(JoinMapStatic.DisplayPowerOff, FusionSymbol.DisplayPowerOff);
				CreateStandardJoin(JoinMapStatic.MsgBroadcastEnabled, FusionSymbol.MessageBroadcastEnabled);
				CreateStandardJoin(JoinMapStatic.AuthenticationSucceeded, FusionSymbol.AuthenticateSucceeded);
				CreateStandardJoin(JoinMapStatic.AuthenticationFailed, FusionSymbol.AuthenticateFailed);

				CreateStandardJoin(JoinMapStatic.DeviceUsage, FusionSymbol.DisplayUsage);
				CreateStandardJoin(JoinMapStatic.BoradcasetMsgType, FusionSymbol.BroadcastMessageType);

				CreateStandardJoin(JoinMapStatic.HelpMsg, FusionSymbol.Help);
				CreateStandardJoin(JoinMapStatic.ErrorMsg, FusionSymbol.ErrorMessage);
				CreateStandardJoin(JoinMapStatic.LogText, FusionSymbol.LogText);
		

				// DigitalAttributes[tempJoinMap.SystemPowerOff.JoinNumber].BoolValueFeedback.LinkInputSig(FusionSymbol.SystemPowerOff.InputSig);

				

				// Scheduling Bits for Future 
				//FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.Use();
				//FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(ExtenderRoomViewSchedulingDataReservedSigs_DeviceExtenderSigChange);

				// Future for time sync
				// FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(ExtenderFusionRoomDataReservedSigs_DeviceExtenderSigChange);

				
				 
				FusionRVI.GenerateFileForAllFusionDevices();
			}
			catch (Exception ex)
			{
				Debug.Console(2, this, "Exception DynFusion Initialize {0}", ex);
			}

		}
		void CreateStandardJoin(JoinDataComplete join, BooleanSigDataFixedName Sig)
		{
			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				DigitalAttributesFromFusion.Add(join.JoinNumber, new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				DigitalAttributesToFusion.Add(join.JoinNumber, new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
				DigitalAttributesToFusion[join.JoinNumber].BoolValueFeedback.LinkInputSig(Sig.InputSig);
			}
		}
		void CreateStandardJoin(JoinDataComplete join, UShortSigDataFixedName Sig)
		{

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				AnalogAttributesFromFusion.Add(join.JoinNumber, new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				AnalogAttributesToFusion.Add(join.JoinNumber, new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
				AnalogAttributesToFusion[join.JoinNumber].UShortValueFeedback.LinkInputSig(Sig.InputSig);
			}
		}
		void CreateStandardJoin(JoinDataComplete join, StringSigDataFixedName Sig)
		{
			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				SerialAttributesFromFusion.Add(join.JoinNumber, new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				SerialAttributesToFusion.Add(join.JoinNumber, new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
				SerialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(Sig.InputSig);
			}
		}

		void FusionSymbol_FusionStateChange(FusionBase device, FusionStateEventArgs args)
		{
			Debug.Console(2, this, "DynFusion FusionStateChange {0} {1}", args.EventId, args.UserConfiguredSigDetail.ToString());
			switch (args.EventId)
			{
				
				case FusionEventIds.SystemPowerOnReceivedEventId:
					{
						
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributesFromFusion.TryGetValue(JoinMapStatic.SystemPowerOn.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.SystemPowerOffReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributesFromFusion.TryGetValue(JoinMapStatic.SystemPowerOff.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.DisplayPowerOnReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributesFromFusion.TryGetValue(JoinMapStatic.DisplayPowerOn.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.DisplayPowerOffReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributesFromFusion.TryGetValue(JoinMapStatic.DisplayPowerOff.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.BroadcastMessageTypeReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionAnalogAttribute output;
						if (AnalogAttributesFromFusion.TryGetValue(JoinMapStatic.BoradcasetMsgType.JoinNumber, out output))
						{
							output.UShortValue = sigDetails.OutputSig.UShortValue;
						}
						break;
					}
				case FusionEventIds.HelpMessageReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.SystemPowerOn.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.TextMessageFromRoomReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.TextMessage.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.BroadcastMessageReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.BroadcastMsg.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.GroupMembershipRequestReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.GroupMembership.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				
				case FusionEventIds.UserConfiguredBoolSigChangeEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigData;
						uint joinNumber = (uint)(sigDetails.Number + FusionJoinOffset);
						DynFusionDigitalAttribute output;
						Debug.Console(2, this, "DynFusion UserAttribute Digital Join:{0} Name:{1} Value:{2}", joinNumber, sigDetails.Name, sigDetails.OutputSig.BoolValue);

						if (DigitalAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				
				case FusionEventIds.UserConfiguredUShortSigChangeEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
						uint joinNumber = (uint)(sigDetails.Number + FusionJoinOffset);
						DynFusionAnalogAttribute output;
						Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber, sigDetails.Name, sigDetails.OutputSig.UShortValue);

						if (AnalogAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.UShortValue = sigDetails.OutputSig.UShortValue;
						}
						break;
					}
				case FusionEventIds.UserConfiguredStringSigChangeEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as StringSigData;
						uint joinNumber = (uint)(sigDetails.Number + FusionJoinOffset);
						DynFusionSerialAttribute output;
						Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber, sigDetails.Name, sigDetails.OutputSig.StringValue);

						if (SerialAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}

				 
			}
		}

		void FusionSymbol_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
		{
			FusionOnlineFeedback.FireUpdate();
		}
		private static eSigIoMask GetIOMask(eReadWrite mask)
		{
			var type = eSigIoMask.NA;

			switch (mask)
			{
				case eReadWrite.R: type = eSigIoMask.InputSigOnly; break;
				case eReadWrite.W: type = eSigIoMask.OutputSigOnly; break;
				case eReadWrite.RW: type = eSigIoMask.InputOutputSig; break;
			}
			return (type);
		}
		private static eSigIoMask GetIOMask(string mask)
		{
			var _RWType = eSigIoMask.NA;

			switch (mask)
			{
				case "R": _RWType = eSigIoMask.InputSigOnly; break;
				case "W": _RWType = eSigIoMask.OutputSigOnly; break;
				case "RW": _RWType = eSigIoMask.InputOutputSig; break;
			}
			return (_RWType);
		}
		private static eReadWrite GeteReadWrite(eJoinCapabilities mask)
		{
			eReadWrite type = eReadWrite.ReadWrite;

			switch (mask)
			{
				case eJoinCapabilities.FromSIMPL: type = eReadWrite.Read; break;
				case eJoinCapabilities.ToSIMPL: type = eReadWrite.Write; break;
				case eJoinCapabilities.ToFromSIMPL: type = eReadWrite.ReadWrite; break;
			}
			return (type);
		}
	    #region Overrides of EssentialsBridgeableDevice

	    public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
	    {
	        /*
			 * /var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

	        // This adds the join map to the collection on the bridge
	        if (bridge != null)
	        {
	            bridge.AddJoinMap(Key, joinMap);
	        }

	        var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

	        if (customJoins != null)
	        {
	            joinMap.SetCustomJoinData(customJoins);
	        }
			 * */ 

	        Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
	        Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

			

			foreach (var att in DigitalAttributesToFusion)
			{
				var attLocal = att.Value;
				trilist.SetBoolSigAction(attLocal.JoinNumber, (b) => { attLocal.BoolValue = b; });
			}
			foreach (var att in DigitalAttributesFromFusion)
			{
				var attLocal = att.Value;
				attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[attLocal.JoinNumber]);
			}
			foreach (var att in AnalogAttributesToFusion)
			{
				var attLocal = att.Value;
				trilist.SetUShortSigAction(attLocal.JoinNumber, (a) => { attLocal.UShortValue = a; });
			}
			foreach (var att in AnalogAttributesFromFusion)
			{
				var attLocal = att.Value;
				attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[attLocal.JoinNumber]);	
			}

			foreach (var att in SerialAttributesToFusion)
			{
				var attLocal = att.Value;
				trilist.SetStringSigAction(attLocal.JoinNumber, (a) => { attLocal.StringValue = a; });
			}
			foreach(var att in SerialAttributesFromFusion)
			{
				var attLocal = att.Value;
				attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[attLocal.JoinNumber]);
			}
	        trilist.OnlineStatusChange += (o, a) =>
	        {
	            if (a.DeviceOnLine)
	            {
	                // trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
	            }
	        };
	    }


	    #endregion
	}
}

