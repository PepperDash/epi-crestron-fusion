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
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp.CrestronXmlLinq;
using DynFusion.Assets;

namespace DynFusion 
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
		public RoomInformation RoomInformation;

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
			Debug.Console(2, "Creating Fusion Symbol {0} {1}", _Config.control.IpId, Key);
			FusionSymbol = new FusionRoom(_Config.control.IpIdInt, Global.ControlSystem, "", Guid.NewGuid().ToString());
			FusionSymbol.ExtenderFusionRoomDataReservedSigs.Use();
			if (FusionSymbol.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
			{
				Debug.Console(0, this, "Faliure to register Fusion Symbol");
			}


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

				
				

				// Online Status 
				FusionOnlineFeedback = new BoolFeedback(() => { return FusionSymbol.IsOnline; });
				FusionSymbol.OnlineStatusChange += new OnlineStatusChangeEventHandler(FusionSymbol_OnlineStatusChange);
	
				// Attribute State Changes 
				FusionSymbol.FusionStateChange += new FusionStateEventHandler(FusionSymbol_FusionStateChange);
				FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(FusionSymbol_RoomDataDeviceExtenderSigChange);

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

				// Create Links for Standard joins 
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
		
				// Room Data Extender 
				CreateStandardJoin(JoinMapStatic.ActionQuery, FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery);
				CreateStandardJoin(JoinMapStatic.RoomConfig, FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigQuery);
				if (_Config.CustomProperties != null)
				{
					if (_Config.CustomProperties.DigitalProperties != null)
					{
						foreach (var att in _Config.CustomProperties.DigitalProperties)
						{
							DigitalAttributesFromFusion.Add(att.JoinNumber, new DynFusionDigitalAttribute(att.ID, att.JoinNumber));
						}
					}
					if (_Config.CustomProperties.AnalogProperties != null)
					{

						foreach (var att in _Config.CustomProperties.AnalogProperties)
						{
							AnalogAttributesFromFusion.Add(att.JoinNumber, new DynFusionAnalogAttribute(att.ID, att.JoinNumber));
						}
					}
					if (_Config.CustomProperties.SerialProperties != null)
					{

						foreach (var att in _Config.CustomProperties.SerialProperties)
						{
							SerialAttributesFromFusion.Add(att.JoinNumber, new DynFusionSerialAttribute(att.ID, att.JoinNumber));
						}
					}
				}

				if (_Config.Assets != null)
				{
					if (_Config.Assets.OccupancySensors != null)
					{
						foreach (var occSensorConfig in _Config.Assets.OccupancySensors)
						{
							uint tempAssetNumber = GetNextAvailableAssetNumber(FusionSymbol);
							Debug.Console(2, this, string.Format("Creating occSensor: {0}, {1}", tempAssetNumber, occSensorConfig.Key));
							FusionSymbol.AddAsset(eAssetType.OccupancySensor, tempAssetNumber, occSensorConfig.Key, "Occupancy Sensor", Guid.NewGuid().ToString());
							DynFusionAssetOccupancySensor OccSensor = new DynFusionAssetOccupancySensor(occSensorConfig.Key, occSensorConfig.LinkToDeviceKey, FusionSymbol, tempAssetNumber);
							//API.StringActionDict[(ushort)occSensorConfig.join] = (args) => OccSensor.sendChange(args.Sig.StringValue);
						}
					}

				}

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
		void CreateStandardJoin(JoinDataComplete join, StringInputSig Sig)
		{
			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				SerialAttributesFromFusion.Add(join.JoinNumber, new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL || join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				SerialAttributesToFusion.Add(join.JoinNumber, new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
				SerialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(Sig);
			}
		}

		void FusionSymbol_RoomDataDeviceExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{
			Debug.Console(2, this, string.Format("DynFusion DeviceExtenderChange {0} {1} {2} {3}", currentDeviceExtender.ToString(), args.Sig.Number, args.Sig.Type, args.Sig.StringValue));
			ushort joinNumber = (ushort)args.Sig.Number;
			
			switch (args.Sig.Type)
			{
				case eSigType.Bool:
					{

						break;
					}
				case eSigType.UShort:
					{

						break;
					}

				case eSigType.String:
					{
						//var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionSerialAttribute output;


						
						if (SerialAttributesFromFusion.TryGetValue(args.Sig.Number, out output))
						{
							output.StringValue = args.Sig.StringValue;
						}
						
						if (args.Sig == FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigResponse && args.Sig.StringValue != null)
						{
							RoomConfigParseData(args.Sig.StringValue);
						}
						 

						break;
					}
			}

		}

		void FusionSymbol_FusionStateChange(FusionBase device, FusionStateEventArgs args)
		{
			Debug.Console(2, this, "DynFusion FusionStateChange {0} {1}", args.EventId, args.UserConfiguredSigDetail.ToString());
			switch (args.EventId)
			{
				
				case FusionEventIds.SystemPowerOnReceivedEventId:
					{
						// Comments
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
				case FusionEventIds.AuthenticateFailedReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.AuthenticationFailed.JoinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					
					}
				case FusionEventIds.AuthenticateSucceededReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
						DynFusionSerialAttribute output;
						if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.AuthenticationSucceeded.JoinNumber, out output))
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
			GetRoomConfig();
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
		public static uint GetNextAvailableAssetNumber(FusionRoom room)
		{
			uint slotNum = 0;
			foreach (var item in room.UserConfigurableAssetDetails) 
			{
				if (item.Number > slotNum) {
					slotNum = item.Number;
					}
				}
				if (slotNum < 5){
					slotNum = 5;
					}
				else
					slotNum = slotNum + 1;
				Debug.Console(2, string.Format("#Next available fusion asset number is: {0}", slotNum));

				return slotNum;
			}
	    #region Overrides of EssentialsBridgeableDevice

		public void GetRoomConfig()
		{
			try
			{
				if (FusionSymbol.IsOnline)
				{

					string fusionRoomConfigRequest = String.Format("<RequestRoomConfiguration><RequestID>RoomConfigurationRequest</RequestID><CustomProperties><Property></Property></CustomProperties></RequestRoomConfiguration>");

					Debug.Console(2, this, "Room Request: {0}", fusionRoomConfigRequest);
					SerialAttributesToFusion[JoinMapStatic.RoomConfig.JoinNumber].StringValue = fusionRoomConfigRequest;
				}
			}
			catch (Exception e)
			{
				Debug.Console(2, this, "GetRoomConfig Error {0}", e);
			}

		}

		private void RoomConfigParseData(string data)
		{
                data = data.Replace("&", "and");

                try
                {
                    XmlDocument roomConfigResponse = new XmlDocument();

                    roomConfigResponse.LoadXml(data);

                    var requestRoomConfiguration = roomConfigResponse["RoomConfigurationResponse"];

                    if (requestRoomConfiguration != null)
                    {
                        foreach (XmlElement e in roomConfigResponse.FirstChild.ChildNodes)
                        {
                            if (e.Name == "RoomInformation")
                            {
                                XmlReader roomInfo = new XmlReader(e.OuterXml);

                                RoomInformation = CrestronXMLSerialization.DeSerializeObject<RoomInformation>(roomInfo);
                            }
                            else if (e.Name == "CustomFields")
                            {
								foreach (XmlElement el in e)
                                {
									var id = el.Attributes["ID"].Value;
				
									var type = el.SelectSingleNode("CustomFieldType").InnerText;
									var val = el.SelectSingleNode("CustomFieldValue").InnerText;
									if (type == "Boolean")
									{
										
										var attirbute = DigitalAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

										if (attirbute.Value != null)
										{
											attirbute.Value.BoolValue = Boolean.Parse(val);
										}
										
									}
									else if (type == "Integer")
									{
										var attirbute = AnalogAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

										if (attirbute.Value != null)
										{
											attirbute.Value.UShortValue = uint.Parse(val);
										}
									}
									else if (type == "String" || type == "Text" || type == "URL")
									{
										var attirbute = SerialAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

										if (attirbute.Value != null)
										{
											attirbute.Value.StringValue = val;
										}
									}
									Debug.Console(2, this, "RoomConfigParseData {0} {1} {2}", type, id, val); 
									}
							
							
                            }
                        }
                    }
                }
				catch (Exception e)
				{
					Debug.Console(2, this, "GetRoomConfig Error {0}", e);
				}
		}
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
					GetRoomConfig();
	            }
	        };
	    }


	    #endregion
	}
	public class RoomInformation
	{
		public string ID { get; set; }
		public string Name { get; set; }
		public string Location { get; set; }
		public string Description { get; set; }
		public string TimeZone { get; set; }
		public string WebcamURL { get; set; }
		public string BacklogMsg { get; set; }
		public string SubErrorMsg { get; set; }
		public string EmailInfo { get; set; }
		public List<FusionCustomProperty> FusionCustomProperties { get; set; }

		public RoomInformation()
		{
			FusionCustomProperties = new List<FusionCustomProperty>();
		}
	}
}

