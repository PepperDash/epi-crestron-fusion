// For Basic SIMPL# Classes
// For Basic SIMPL#Pro classes

using Crestron.SimplSharpPro.DeviceSupport;
using DynFusion.Config;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Interfaces;
using PepperDash.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp;


namespace DynFusion
{
    public class DynFusionDevice : EssentialsBridgeableDevice, ILogStringsWithLevel, ILogStrings
    {
        public const ushort FusionJoinOffset = 49;
        //DynFusion Joins

        private readonly DynFusionConfigObjectTemplate _config;
        private readonly Dictionary<UInt32, DynFusionDigitalAttribute> _digitalAttributesToFusion;
        private readonly Dictionary<UInt32, DynFusionAnalogAttribute> _analogAttributesToFusion;
        private readonly Dictionary<UInt32, DynFusionSerialAttribute> _serialAttributesToFusion;
        private readonly Dictionary<UInt32, DynFusionDigitalAttribute> _digitalAttributesFromFusion;
        private readonly Dictionary<UInt32, DynFusionAnalogAttribute> _analogAttributesFromFusion;
        private readonly Dictionary<UInt32, DynFusionSerialAttribute> _serialAttributesFromFusion;

        private readonly IDictionary<uint, DynFusionCallStatisticsDevice> _callStatisticsDevices 
            = new Dictionary<uint, DynFusionCallStatisticsDevice>();

        private static DynFusionJoinMap _joinMapStatic;

        public BoolFeedback FusionOnlineFeedback;
        public RoomInformation RoomInformation;

        public DynFusionDeviceUsage DeviceUsage;
        public FusionRoom FusionSymbol;
        private CTimer _errorLogTimer;
        private string _errorLogLastMessageSent;

        public DynFusionDevice(string key, string name, DynFusionConfigObjectTemplate config)
            : base(key, name)
        {
            Debug.Console(0, this, "Constructing new DynFusionDevice instance");
            _config = config;
            _digitalAttributesToFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
            _analogAttributesToFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
            _serialAttributesToFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
            _digitalAttributesFromFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
            _analogAttributesFromFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
            _serialAttributesFromFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
            _joinMapStatic = new DynFusionJoinMap(1);
            Debug.Console(2, "Creating Fusion Symbol {0} {1}", _config.Control.IpId, Key);
            FusionSymbol = new FusionRoom(_config.Control.IpIdInt, Global.ControlSystem, "", Guid.NewGuid().ToString());

            if (FusionSymbol.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                Debug.Console(0, this, "Faliure to register Fusion Symbol");
            }
            FusionSymbol.ExtenderFusionRoomDataReservedSigs.Use();
        }

        public override bool CustomActivate()
        {
            Initialize();
            return true;
        }

        private void Initialize()
        {
            try
            {
                // Online Status 
                FusionOnlineFeedback = new BoolFeedback(() => FusionSymbol.IsOnline);
                FusionSymbol.OnlineStatusChange += FusionSymbol_OnlineStatusChange;

                // Attribute State Changes 
                FusionSymbol.FusionStateChange += FusionSymbol_FusionStateChange;
                FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange +=
                    FusionSymbol_RoomDataDeviceExtenderSigChange;

                // Create Custom Atributes 
                foreach (var att in _config.CustomAttributes.DigitalAttributes)
                {
                    FusionSymbol.AddSig(eSigType.Bool, att.JoinNumber - FusionJoinOffset, att.Name,
                        GetIoMask(att.RwType));

                    if ((att.RwType & eReadWrite.Read) != 0)
                    {
                        _digitalAttributesToFusion.Add(att.JoinNumber,
                            new DynFusionDigitalAttribute(att.Name, att.JoinNumber, att.LinkDeviceKey,
                                att.LinkDeviceMethod, att.LinkDeviceFeedback));
                        _digitalAttributesToFusion[att.JoinNumber].BoolValueFeedback.LinkInputSig(
                            FusionSymbol.UserDefinedBooleanSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                    }
                    if ((att.RwType & eReadWrite.Write) != 0)
                    {
                        _digitalAttributesFromFusion.Add(att.JoinNumber,
                            new DynFusionDigitalAttribute(att.Name, att.JoinNumber));
                    }
                }

                foreach (var att in _config.CustomAttributes.AnalogAttributes)
                {
                    FusionSymbol.AddSig(eSigType.UShort, att.JoinNumber - FusionJoinOffset, att.Name,
                        GetIoMask(att.RwType));

                    if ((att.RwType & eReadWrite.Read) != 0)
                    {
                        _analogAttributesToFusion.Add(att.JoinNumber,
                            new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
                        _analogAttributesToFusion[att.JoinNumber].UShortValueFeedback.LinkInputSig(
                            FusionSymbol.UserDefinedUShortSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                    }
                    if ((att.RwType & eReadWrite.Write) != 0)
                    {
                        _analogAttributesFromFusion.Add(att.JoinNumber,
                            new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
                    }
                }
                foreach (var att in _config.CustomAttributes.SerialAttributes)
                {
                    FusionSymbol.AddSig(eSigType.String, att.JoinNumber - FusionJoinOffset, att.Name,
                        GetIoMask(att.RwType));
                    if ((att.RwType & eReadWrite.Read) != 0)
                    {
                        _serialAttributesToFusion.Add(att.JoinNumber,
                            new DynFusionSerialAttribute(att.Name, att.JoinNumber));
                        _serialAttributesToFusion[att.JoinNumber].StringValueFeedback.LinkInputSig(
                            FusionSymbol.UserDefinedStringSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                    }
                    if ((att.RwType & eReadWrite.Write) != 0)
                    {
                        _serialAttributesFromFusion.Add(att.JoinNumber,
                            new DynFusionSerialAttribute(att.Name, att.JoinNumber));
                    }
                }

                // Create Links for Standard joins 
                CreateStandardJoin(_joinMapStatic.SystemPowerOn, FusionSymbol.SystemPowerOn);
                CreateStandardJoin(_joinMapStatic.SystemPowerOff, FusionSymbol.SystemPowerOff);
                CreateStandardJoin(_joinMapStatic.DisplayPowerOn, FusionSymbol.DisplayPowerOn);
                CreateStandardJoin(_joinMapStatic.DisplayPowerOff, FusionSymbol.DisplayPowerOff);
                CreateStandardJoin(_joinMapStatic.MsgBroadcastEnabled, FusionSymbol.MessageBroadcastEnabled);
                CreateStandardJoin(_joinMapStatic.AuthenticationSucceeded, FusionSymbol.AuthenticateSucceeded);
                CreateStandardJoin(_joinMapStatic.AuthenticationFailed, FusionSymbol.AuthenticateFailed);

                CreateStandardJoin(_joinMapStatic.DeviceUsage, FusionSymbol.DisplayUsage);
                CreateStandardJoin(_joinMapStatic.BoradcasetMsgType, FusionSymbol.BroadcastMessageType);

                CreateStandardJoin(_joinMapStatic.HelpMsg, FusionSymbol.Help);
                CreateStandardJoin(_joinMapStatic.ErrorMsg, FusionSymbol.ErrorMessage);
                CreateStandardJoin(_joinMapStatic.LogText, FusionSymbol.LogText);

                // Room Data Extender 
                CreateStandardJoin(_joinMapStatic.ActionQuery,
                    FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery);
                CreateStandardJoin(_joinMapStatic.RoomConfig,
                    FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigQuery);

                if (_config.CustomProperties != null)
                {
                    if (_config.CustomProperties.DigitalProperties != null)
                    {
                        foreach (var att in _config.CustomProperties.DigitalProperties)
                        {
                            _digitalAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionDigitalAttribute(att.Id, att.JoinNumber));
                        }
                    }
                    if (_config.CustomProperties.AnalogProperties != null)
                    {
                        foreach (var att in _config.CustomProperties.AnalogProperties)
                        {
                            _analogAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionAnalogAttribute(att.Id, att.JoinNumber));
                        }
                    }
                    if (_config.CustomProperties.SerialProperties != null)
                    {
                        foreach (var att in _config.CustomProperties.SerialProperties)
                        {
                            _serialAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionSerialAttribute(att.Id, att.JoinNumber));
                        }
                    }
                }

                if (_config.Assets != null)
                {
                    if (_config.Assets.OccupancySensors != null)
                    {
                        var sensors = from occSensorConfig in _config.Assets.OccupancySensors
                            select
                                new DynFusionAssetOccupancySensor(
                                    occSensorConfig.Key,
                                    occSensorConfig.LinkToDeviceKey,
                                    FusionSymbol,
                                    GetNextAvailableAssetNumber(FusionSymbol));

                        sensors
                            .ToList()
                            .ForEach(sensor =>
                                FusionSymbol.AddAsset(
                                    eAssetType.OccupancySensor,
                                    sensor.AssetNumber,
                                    sensor.Key,
                                    "Occupancy Sensor",
                                    Guid.NewGuid().ToString()));
                    }
                }

                if (_config.CallStatistics != null)
                {
                    var callStats = from callStatsConfig in _config.CallStatistics.Devices
                        select
                            new DynFusionCallStatisticsDevice(
                                Key + "-" + callStatsConfig.Name, 
                                callStatsConfig.Name,
                                FusionSymbol, 
                                callStatsConfig.Type, 
                                callStatsConfig.UseCallTimer,
                                callStatsConfig.PostMeetingId, 
                                callStatsConfig.JoinNumber);

                    callStats
                        .ToList()
                        .ForEach(callStat => _callStatisticsDevices.Add(callStat.JoinNumber, callStat));
                }

                DeviceUsageFactory();
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

        private void DeviceUsageFactory()
        {
            if (_config.DeviceUsage == null) return;
            DeviceUsage = new DynFusionDeviceUsage(string.Format("{0}-DeviceUsage", Key), this);
            if (_config.DeviceUsage.UsageMinThreshold > 0)
            {
                DeviceUsage.UsageMinThreshold = _config.DeviceUsage.UsageMinThreshold;
            }

            if (_config.DeviceUsage.Devices != null && _config.DeviceUsage.Devices.Count > 0)
            {
                foreach (var device in _config.DeviceUsage.Devices)
                {
                    try
                    {
                        Debug.Console(1, this, "Creating Device: {0}, {1}, {2}", device.JoinNumber, device.Type,
                            device.Name);
                        DeviceUsage.CreateDevice(device.JoinNumber, device.Type, device.Name);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, this, "{0}", ex);
                    }
                }
            }
            if (_config.DeviceUsage.Displays != null && _config.DeviceUsage.Displays.Count > 0)
            {
                foreach (var display in _config.DeviceUsage.Displays)
                {
                    try
                    {
                        Debug.Console(1, this, "Creating Display: {0}, {1}", display.JoinNumber, display.Name);
                        DeviceUsage.CreateDisplay(display.JoinNumber, display.Name);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, this, "{0}", ex);
                    }
                }
            }
            if (_config.DeviceUsage.Sources != null && _config.DeviceUsage.Sources.Count > 0)
            {
                foreach (var source in _config.DeviceUsage.Sources)
                {
                    try
                    {
                        Debug.Console(1, this, "Creating Source: {0}, {1}", source.SourceNumber, source.Name);
                        DeviceUsage.CreateSource(source.SourceNumber, source.Name, source.Type);
                    }
                    catch (Exception ex)
                    {
                        Debug.Console(0, this, "{0}", ex);
                    }
                }
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, BooleanSigDataFixedName sig)
        {
            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.ToSIMPL) != 0)
            {
                _digitalAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.FromSIMPL) != 0)
            {
                _digitalAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
                _digitalAttributesToFusion[join.JoinNumber].BoolValueFeedback.LinkInputSig(sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, UShortSigDataFixedName sig)
        {
            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.ToSIMPL) != 0)
            {
                _analogAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.FromSIMPL) != 0)
            {
                _analogAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
                _analogAttributesToFusion[join.JoinNumber].UShortValueFeedback.LinkInputSig(sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, StringSigDataFixedName sig)
        {
            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.ToSIMPL) != 0)
            {
                _serialAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.FromSIMPL) != 0)
            {
                _serialAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
                _serialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, StringInputSig sig)
        {
            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.ToSIMPL) != 0)
            {
                _serialAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if ((join.Metadata.JoinCapabilities & eJoinCapabilities.FromSIMPL) != 0)
            {
                _serialAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
                _serialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(sig);
            }
        }

        private void FusionSymbol_RoomDataDeviceExtenderSigChange(DeviceExtender currentDeviceExtender,
            SigEventArgs args)
        {
            Debug.Console(2, this,
                string.Format("DynFusion DeviceExtenderChange {0} {1} {2} {3}", currentDeviceExtender.ToString(),
                    args.Sig.Number, args.Sig.Type, args.Sig.StringValue));

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


                    if (_serialAttributesFromFusion.TryGetValue(args.Sig.Number, out output))
                    {
                        output.StringValue = args.Sig.StringValue;
                    }

                    if (args.Sig == FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigResponse &&
                        args.Sig.StringValue != null)
                    {
                        RoomConfigParseData(args.Sig.StringValue);
                    }


                    break;
                }
            }
        }

        private void FusionSymbol_FusionStateChange(FusionBase device, FusionStateEventArgs args)
        {
            Debug.Console(2, this, "DynFusion FusionStateChange {0} {1}", args.EventId,
                args.UserConfiguredSigDetail.ToString());
            switch (args.EventId)
            {
                case FusionEventIds.SystemPowerOnReceivedEventId:
                {
                    // Comments
                    ProcessSystemPowerOnEvent(args);
                    break;
                }
                case FusionEventIds.SystemPowerOffReceivedEventId:
                {
                    ProcessSystemPowerOffEvent(args);
                    break;
                }
                case FusionEventIds.DisplayPowerOnReceivedEventId:
                {
                    ProcessDisplayPowerOnEvent(args);
                    break;
                }
                case FusionEventIds.DisplayPowerOffReceivedEventId:
                {
                    ProcessDisplayPowerOffEvent(args);
                    break;
                }
                case FusionEventIds.BroadcastMessageTypeReceivedEventId:
                {
                    ProcessBroadcastMessageTypeReceivedEvent(args);
                    break;
                }
                case FusionEventIds.HelpMessageReceivedEventId:
                {
                    ProcessHelpMessageReceivedEvent(args);
                    break;
                }
                case FusionEventIds.TextMessageFromRoomReceivedEventId:
                {
                    ProcessTextMessageFromRoomEvent(args);
                    break;
                }
                case FusionEventIds.BroadcastMessageReceivedEventId:
                {
                    ProcessBroadcastMessageReceivedEvent(args);
                    break;
                }
                case FusionEventIds.GroupMembershipRequestReceivedEventId:
                {
                    ProcessGroupMembershipRequestEvent(args);
                    break;
                }
                case FusionEventIds.AuthenticateFailedReceivedEventId:
                {
                    ProcessAuthenticateFailedEvent(args);
                    break;
                }
                case FusionEventIds.AuthenticateSucceededReceivedEventId:
                {
                    ProcessAuthenticateSucceededEvent(args);
                    break;
                }
                case FusionEventIds.UserConfiguredBoolSigChangeEventId:
                {
                    ProcessUserConfiguredBoolEvent(args);
                    break;
                }

                case FusionEventIds.UserConfiguredUShortSigChangeEventId:
                {
                    ProcessUserConfiguredUshortEvent(args);
                    break;
                }
                case FusionEventIds.UserConfiguredStringSigChangeEventId:
                {
                    ProcessUserConfiguredStringEvent(args);
                    break;
                }
            }
        }

        private void ProcessUserConfiguredStringEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as StringSigData;
            if (sigDetails == null) return;
            var joinNumber = sigDetails.Number + FusionJoinOffset;
            DynFusionSerialAttribute output;
            Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber,
                sigDetails.Name, sigDetails.OutputSig.StringValue);
            if (!_serialAttributesFromFusion.TryGetValue(joinNumber, out output)) return;

            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessUserConfiguredBoolEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as BooleanSigData;
            if (sigDetails == null) return;
            var joinNumber = sigDetails.Number + FusionJoinOffset;
            DynFusionDigitalAttribute output;
            Debug.Console(2, this, "DynFusion UserAttribute Digital Join:{0} Name:{1} Value:{2}", joinNumber,
                sigDetails.Name, sigDetails.OutputSig.BoolValue);
            if (!_digitalAttributesFromFusion.TryGetValue(joinNumber, out output)) return;

            output.BoolValue = sigDetails.OutputSig.BoolValue;
        }

        private void ProcessUserConfiguredUshortEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
            if (sigDetails == null) return;
            var joinNumber = sigDetails.Number + FusionJoinOffset;
            DynFusionAnalogAttribute output;
            Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber,
                sigDetails.Name, sigDetails.OutputSig.UShortValue);
            if (!_analogAttributesFromFusion.TryGetValue(joinNumber, out output)) return;

            output.UShortValue = sigDetails.OutputSig.UShortValue;
        }

        private void ProcessAuthenticateSucceededEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;

            DynFusionSerialAttribute output;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.AuthenticationSucceeded.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessAuthenticateFailedEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionSerialAttribute output;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.AuthenticationFailed.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessGroupMembershipRequestEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionSerialAttribute output;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.GroupMembership.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessBroadcastMessageReceivedEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            DynFusionSerialAttribute output;
            if (sigDetails == null) return;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.BroadcastMsg.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessTextMessageFromRoomEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionSerialAttribute output;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.TextMessage.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessHelpMessageReceivedEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionSerialAttribute output;
            if (!_serialAttributesFromFusion.TryGetValue(_joinMapStatic.SystemPowerOn.JoinNumber, out output)) return;
            output.StringValue = sigDetails.OutputSig.StringValue;
        }

        private void ProcessBroadcastMessageTypeReceivedEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as UShortSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionAnalogAttribute output;
            if (!_analogAttributesFromFusion.TryGetValue(_joinMapStatic.BoradcasetMsgType.JoinNumber, out output)) return;
            output.UShortValue = sigDetails.OutputSig.UShortValue;
        }

        private void ProcessDisplayPowerOffEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionDigitalAttribute output;
            if (!_digitalAttributesFromFusion.TryGetValue(_joinMapStatic.DisplayPowerOff.JoinNumber, out output)) return;
            output.BoolValue = sigDetails.OutputSig.BoolValue;
        }

        private void ProcessDisplayPowerOnEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionDigitalAttribute output;
            if (!_digitalAttributesFromFusion.TryGetValue(_joinMapStatic.DisplayPowerOn.JoinNumber, out output)) return;
            output.BoolValue = sigDetails.OutputSig.BoolValue;
        }

        private void ProcessSystemPowerOffEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionDigitalAttribute output;
            if (!_digitalAttributesFromFusion.TryGetValue(_joinMapStatic.SystemPowerOff.JoinNumber, out output)) return;
            output.BoolValue = sigDetails.OutputSig.BoolValue;
        }

        private void ProcessSystemPowerOnEvent(FusionStateEventArgs args)
        {
            var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
            if (sigDetails == null) return;
            DynFusionDigitalAttribute output;
            if (!_digitalAttributesFromFusion.TryGetValue(_joinMapStatic.SystemPowerOn.JoinNumber, out output)) return;
            output.BoolValue = sigDetails.OutputSig.BoolValue;
        }

        private void FusionSymbol_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            FusionOnlineFeedback.FireUpdate();
            if (args.DeviceOnLine)
            {
                GetRoomConfig();
            }
        }

        private static eSigIoMask GetIoMask(eReadWrite mask)
        {
            var type = eSigIoMask.NA;

            switch (mask)
            {
                case eReadWrite.R:
                    type = eSigIoMask.InputSigOnly;
                    break;
                case eReadWrite.W:
                    type = eSigIoMask.OutputSigOnly;
                    break;
                case eReadWrite.Rw:
                    type = eSigIoMask.InputOutputSig;
                    break;
            }
            return (type);
        }

/*
        private static eSigIoMask GetIoMask(string mask)
        {
            var rwType = eSigIoMask.NA;

            switch (mask)
            {
                case "R":
                    rwType = eSigIoMask.InputSigOnly;
                    break;
                case "W":
                    rwType = eSigIoMask.OutputSigOnly;
                    break;
                case "RW":
                    rwType = eSigIoMask.InputOutputSig;
                    break;
            }
            return (rwType);
        }
*/

        private static eReadWrite GeteReadWrite(eJoinCapabilities mask)
        {
            var type = eReadWrite.ReadWrite;

            switch (mask)
            {
                case eJoinCapabilities.FromSIMPL:
                    type = eReadWrite.Read;
                    break;
                case eJoinCapabilities.ToSIMPL:
                    type = eReadWrite.Write;
                    break;
                case eJoinCapabilities.ToFromSIMPL:
                    type = eReadWrite.ReadWrite;
                    break;
            }
            return (type);
        }

        public static uint GetNextAvailableAssetNumber(FusionRoom room)
        {
            uint slotNum = 0;
            foreach (var item in room.UserConfigurableAssetDetails)
            {
                if (item.Number > slotNum)
                {
                    slotNum = item.Number;
                }
            }
            if (slotNum < 5)
            {
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
                    string fusionRoomConfigRequest =
                        String.Format(
                            "<RequestRoomConfiguration><RequestID>RoomConfigurationRequest</RequestID><CustomProperties><Property></Property></CustomProperties></RequestRoomConfiguration>");

                    Debug.Console(2, this, "Room Request: {0}", fusionRoomConfigRequest);
                    FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigQuery.StringValue =
                        fusionRoomConfigRequest;
                }
            }
            catch (Exception e)
            {
                Debug.Console(2, this, "GetRoomConfig Error {0}", e);
            }
        }

        #endregion

        #region ILogStringsWithLevel Members

        public void SendToLog(IKeyed device, Debug.ErrorLogLevel level, string logMessage)
        {
            int fusionLevel;
            switch (level)
            {
                case Debug.ErrorLogLevel.Error:
                {
                    fusionLevel = 3;
                    break;
                }
                case Debug.ErrorLogLevel.Notice:
                {
                    fusionLevel = 1;
                    break;
                }
                case Debug.ErrorLogLevel.Warning:
                {
                    fusionLevel = 2;
                    break;
                }
                case Debug.ErrorLogLevel.None:
                {
                    fusionLevel = 0;
                    break;
                }
                default:
                {
                    fusionLevel = 0;
                    break;
                }
            }
            var tempLogMessage = string.Format("{0}:{1}", fusionLevel, logMessage);
            const long errorlogThrottleTime = 60000;
            if (_errorLogLastMessageSent == tempLogMessage) return;
            _errorLogLastMessageSent = tempLogMessage;
            if (_errorLogTimer == null)
            {
                _errorLogTimer = new CTimer(o =>
                {
                    Debug.Console(2, this, "Sent Message {0}", _errorLogLastMessageSent);
                    FusionSymbol.ErrorMessage.InputSig.StringValue = _errorLogLastMessageSent;
                }, errorlogThrottleTime);
                return;
            }
            _errorLogTimer.Reset(errorlogThrottleTime);
        }

        #endregion

        #region ILogStrings Members

        public void SendToLog(IKeyed device, string logMessage)
        {
            FusionSymbol.LogText.InputSig.StringValue = logMessage;
        }

        #endregion

        private void RoomConfigParseData(string data)
        {
            data = data.Replace("&", "and");

            try
            {
                var roomConfigResponse = new XmlDocument();

                roomConfigResponse.LoadXml(data);

                var requestRoomConfiguration = roomConfigResponse["RoomConfigurationResponse"];

                if (requestRoomConfiguration == null) return;
                foreach (XmlElement element in roomConfigResponse.FirstChild.ChildNodes)
                {
                    GetRoomConfigResponseValue(element);
                }
            }
            catch (Exception e)
            {
                Debug.Console(2, this, "GetRoomConfig Error {0}", e);
            }
        }

        private void GetRoomConfigResponseValue(XmlElement e)
        {
            switch (e.Name)
            {
                case "RoomInformation":
                {
                    var roomInfo = new XmlReader(e.OuterXml);

                    RoomInformation = CrestronXMLSerialization.DeSerializeObject<RoomInformation>(roomInfo);
                    var attirbute = _serialAttributesFromFusion.SingleOrDefault(x => x.Value.Name == "Name");

                    if (attirbute.Value == null) break;
                    attirbute.Value.StringValue = RoomInformation.Name;
                    break;
                }
                case "CustomFields":
                {
                    foreach (XmlElement element in e)
                    {
                        GetCustomFieldValue(element);
                    }
                    break;
                }
            }
        }

        private void GetCustomFieldValue(XmlElement el)
        {
            var id = el.Attributes["ID"].Value;

            var type = el.SelectSingleNode("CustomFieldType").InnerText;
            var val = el.SelectSingleNode("CustomFieldValue").InnerText;
            Debug.Console(2, this, "RoomConfigParseData {0} {1} {2}", type, id, val);

            switch (type)
            {
                case "Boolean":
                {
                    var attribute =
                        _digitalAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                    if (attribute.Value == null) break;
                    attribute.Value.BoolValue = Boolean.Parse(val);
                    break;
                }
                case "Integer":
                {
                    var attribute =
                        _analogAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                    if (attribute.Value == null) break;
                    attribute.Value.UShortValue = uint.Parse(val);
                    break;
                }
                case "URL":
                case "Text":
                case "String":
                {
                    var attribute =
                        _serialAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                    if (attribute.Value == null) break;
                    attribute.Value.StringValue = val;
                    break;
                }
            }
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);
            var joinMap = new DynFusionJoinMap(joinStart);

            foreach (var att in _digitalAttributesToFusion)
            {
                var attLocal = att.Value;
                trilist.SetBoolSigAction(attLocal.JoinNumber, b => { attLocal.BoolValue = b; });
            }
            foreach (var att in _digitalAttributesFromFusion)
            {
                var attLocal = att.Value;
                attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[attLocal.JoinNumber]);
            }
            foreach (var att in _analogAttributesToFusion)
            {
                var attLocal = att.Value;
                trilist.SetUShortSigAction(attLocal.JoinNumber, a => { attLocal.UShortValue = a; });
            }
            foreach (var att in _analogAttributesFromFusion)
            {
                var attLocal = att.Value;
                attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[attLocal.JoinNumber]);
            }

            foreach (var att in _serialAttributesToFusion)
            {
                var attLocal = att.Value;
                trilist.SetStringSigAction(attLocal.JoinNumber, a => { attLocal.StringValue = a; });
            }
            foreach (var att in _serialAttributesFromFusion)
            {
                var attLocal = att.Value;
                attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[attLocal.JoinNumber]);
            }

            trilist.SetSigTrueAction(joinMap.RoomConfig.JoinNumber, GetRoomConfig);

            foreach (var callStatisticsDevice in _callStatisticsDevices)
            {
                var join = callStatisticsDevice.Key;
                var device = callStatisticsDevice.Value;

                trilist.SetBoolSigAction(join, state =>
                {
                    if (state)
                    {
                        device.StartDevice();
                    }
                    else
                    {
                        device.StopDevice();
                    }
                });

                device.CallTimeFeedback.LinkInputSig(trilist.StringInput[join]);
            }

            if (DeviceUsage != null)
            {
                foreach (var device in DeviceUsage.UsageInfoDict)
                {
                    switch (device.Value.usageType)
                    {
                        case DynFusionDeviceUsage.UsageType.Display:
                        {
                            var x = device.Value.joinNumber;
                            trilist.SetUShortSigAction(device.Value.joinNumber,
                                args => DeviceUsage.ChangeSource(x, args));
                            break;
                        }
                        case DynFusionDeviceUsage.UsageType.Device:
                        {
                            var x = device.Value.joinNumber;
                            trilist.SetBoolSigAction(device.Value.joinNumber,
                                args => DeviceUsage.StartStopDevice(x, args));
                            break;
                        }
                    }
                }
            }
            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;
                GetRoomConfig();
                foreach (var attLocal in _serialAttributesFromFusion.Select(att => att.Value))
                {
                    attLocal.StringValueFeedback.FireUpdate();
                }
                foreach (var attLocal in _digitalAttributesFromFusion.Select(att => att.Value))
                {
                    attLocal.BoolValueFeedback.FireUpdate();
                }
                foreach (var attLocal in _analogAttributesFromFusion.Select(att => att.Value))
                {
                    attLocal.UShortValueFeedback.FireUpdate();
                }
            };
        }
    }

    public class RoomInformation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string TimeZone { get; set; }
        public string WebcamUrl { get; set; }
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