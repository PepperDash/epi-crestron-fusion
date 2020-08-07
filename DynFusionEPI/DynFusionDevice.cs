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
		private Dictionary<UInt32, DynFusionDigitalAttribute> DigitalAttributes;
		private Dictionary<UInt32, DynFusionAnalogAttribute> AnalogAttributes;
		private Dictionary<UInt32, DynFusionSerialAttribute> SerialAttributes;
		private static DynFusionJoinMap JoinMapStatic;

		public BoolFeedback FusionOnlineFeedback;

		public FusionRoom FusionSymbol;

		public DynFusionDevice(string key, string name, DynFusionConfigObjectTemplate config)
			: base(key, name)
		{
            Debug.Console(0, this, "Constructing new DynFusionDevice instance");
		    _Config = config;
			DigitalAttributes = new Dictionary<UInt32, DynFusionDigitalAttribute>();
			AnalogAttributes = new Dictionary<UInt32, DynFusionAnalogAttribute>();
			SerialAttributes = new Dictionary<UInt32, DynFusionSerialAttribute>();
			

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

				// Create Custom Atributes 
				foreach (var att in _Config.CustomAttributes.DigitalAttributes)
				{
					DigitalAttributes.Add(att.JoinNumber, new DynFusionDigitalAttribute(att.Name, att.JoinNumber, att.RwType));
					FusionSymbol.AddSig(eSigType.Bool, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));
					DigitalAttributes[att.JoinNumber].BoolValueFeedback.LinkInputSig(FusionSymbol.UserDefinedBooleanSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
				}
				
				foreach (var att in _Config.CustomAttributes.AnalogAttributes)
				{
					AnalogAttributes.Add(att.JoinNumber, new DynFusionAnalogAttribute(att.Name, att.JoinNumber, att.RwType));
					FusionSymbol.AddSig(eSigType.UShort, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));
					AnalogAttributes[att.JoinNumber].UShortValueFeedback.LinkInputSig(FusionSymbol.UserDefinedUShortSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);

				}
				foreach (var att in _Config.CustomAttributes.SerialAttributes)
				{
					SerialAttributes.Add(att.JoinNumber, new DynFusionSerialAttribute(att.Name, att.JoinNumber, att.RwType));
					FusionSymbol.AddSig(eSigType.String, att.JoinNumber - FusionJoinOffset, att.Name, GetIOMask(att.RwType));
					SerialAttributes[att.JoinNumber].StringValueFeedback.LinkInputSig(FusionSymbol.UserDefinedStringSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
				}

				// Create Links for Statndard joins 
				var tempJoinMap = new DynFusionJoinMap(1);
				
				CreateStandardJoin(tempJoinMap.SystemPowerOn, FusionSymbol.SystemPowerOn);
				CreateStandardJoin(tempJoinMap.SystemPowerOff, FusionSymbol.SystemPowerOff);
				CreateStandardJoin(tempJoinMap.DisplayPowerOn, FusionSymbol.DisplayPowerOn);
				CreateStandardJoin(tempJoinMap.DisplayPowerOff, FusionSymbol.DisplayPowerOff);
				CreateStandardJoin(tempJoinMap.MsgBroadcastEnabled, FusionSymbol.MessageBroadcastEnabled);
				CreateStandardJoin(tempJoinMap.AuthenticationSucceeded, FusionSymbol.AuthenticateSucceeded);
				CreateStandardJoin(tempJoinMap.AuthenticationFailed, FusionSymbol.AuthenticateFailed);

				CreateStandardJoin(tempJoinMap.DeviceUsage, FusionSymbol.DisplayUsage);
				CreateStandardJoin(tempJoinMap.BoradcasetMsgType, FusionSymbol.BroadcastMessageType);

				CreateStandardJoin(tempJoinMap.HelpMsg, FusionSymbol.Help);
				CreateStandardJoin(tempJoinMap.ErrorMsg, FusionSymbol.ErrorMessage);
				CreateStandardJoin(tempJoinMap.LogText, FusionSymbol.LogText);
		

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

			DigitalAttributes.Add(join.JoinNumber, new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber, GeteReadWrite(join.Metadata.JoinCapabilities)));
			
			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				DigitalAttributes[join.JoinNumber].BoolValueFeedback.LinkInputSig(Sig.InputSig);
			}
		}
		void CreateStandardJoin(JoinDataComplete join, UShortSigDataFixedName Sig)
		{
			AnalogAttributes.Add(join.JoinNumber, new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber, GeteReadWrite(join.Metadata.JoinCapabilities)));

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				AnalogAttributes[join.JoinNumber].UShortValueFeedback.LinkInputSig(Sig.InputSig);
			}
		}
		void CreateStandardJoin(JoinDataComplete join, StringSigDataFixedName Sig)
		{
			SerialAttributes.Add(join.JoinNumber, new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber, GeteReadWrite(join.Metadata.JoinCapabilities)));

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				SerialAttributes[join.JoinNumber].StringValueFeedback.LinkInputSig(Sig.InputSig);
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
						if (DigitalAttributes.TryGetValue(JoinMapStatic.SystemPowerOn.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.SystemPowerOffReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributes.TryGetValue(JoinMapStatic.SystemPowerOff.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.DisplayPowerOnReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributes.TryGetValue(JoinMapStatic.DisplayPowerOn.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.DisplayPowerOffReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (DigitalAttributes.TryGetValue(JoinMapStatic.DisplayPowerOff.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionEventIds.BroadcastMessageTypeReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionAnalogAttribute output;
						if (AnalogAttributes.TryGetValue(JoinMapStatic.BoradcasetMsgType.JoinNumber, out output))
						{
							output.UShortValue = sigDetails.OutputSig.UShortValue;
						}
						break;
					}
				case FusionEventIds.HelpMessageReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributes.TryGetValue(JoinMapStatic.SystemPowerOn.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.TextMessageFromRoomReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributes.TryGetValue(JoinMapStatic.TextMessage.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.BroadcastMessageReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributes.TryGetValue(JoinMapStatic.BroadcastMsg.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
				case FusionEventIds.GroupMembershipRequestReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributes.TryGetValue(JoinMapStatic.GroupMembership.JoinNumber, out output))
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

						if (DigitalAttributes.TryGetValue(joinNumber, out output))
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

						if (AnalogAttributes.TryGetValue(joinNumber, out output))
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

						if (SerialAttributes.TryGetValue(joinNumber, out output))
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

			

			foreach (var att in DigitalAttributes)
			{
				var attLocal = att.Value;
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Read)
				{
					trilist.SetBoolSigAction(attLocal.JoinNumber, (b) => { attLocal.BoolValue = b; });
				}
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Write)
				{
					attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[attLocal.JoinNumber]);
				}

			}
			foreach (var att in AnalogAttributes)
			{
				var attLocal = att.Value;
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Read)
				{
					trilist.SetUShortSigAction(attLocal.JoinNumber, (a) => { attLocal.UShortValue = a; });
				}
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Write)
				{
					attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[attLocal.JoinNumber]);
				}

			}
			foreach (var att in SerialAttributes)
			{
				var attLocal = att.Value;
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Read)
				{
					trilist.SetStringSigAction(attLocal.JoinNumber, (a) => { attLocal.StringValue = a; });
				}
				if (attLocal.RwType == eReadWrite.ReadWrite || attLocal.RwType == eReadWrite.Write)
				{
					attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[attLocal.JoinNumber]);
				}

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

