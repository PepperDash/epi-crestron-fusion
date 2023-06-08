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

        private DynFusionConfigObjectTemplate _Config;
        private Dictionary<UInt32, DynFusionDigitalAttribute> DigitalAttributesToFusion;
        private Dictionary<UInt32, DynFusionAnalogAttribute> AnalogAttributesToFusion;
        private Dictionary<UInt32, DynFusionSerialAttribute> SerialAttributesToFusion;
        private Dictionary<UInt32, DynFusionDigitalAttribute> DigitalAttributesFromFusion;
        private Dictionary<UInt32, DynFusionAnalogAttribute> AnalogAttributesFromFusion;
        private Dictionary<UInt32, DynFusionSerialAttribute> SerialAttributesFromFusion;

        private readonly IDictionary<uint, DynFusionCallStatisticsDevice> _callStatisticsDevices 
            = new Dictionary<uint, DynFusionCallStatisticsDevice>();

        private static DynFusionJoinMap JoinMapStatic;

        public BoolFeedback FusionOnlineFeedback;
        public RoomInformation RoomInformation;

        public DynFusionDeviceUsage DeviceUsage;
        public FusionRoom FusionSymbol;
        private CTimer ErrorLogTimer;
        private string ErrorLogLastMessageSent;

        public DynFusionDevice(string key, string name, DynFusionConfigObjectTemplate config)
            : base(key, name)
        {
            Debug.Console(0, this, "Constructing new DynFusionDevice instance");
            Name = name;
            _Config = config;
            DigitalAttributesToFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
            AnalogAttributesToFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
            SerialAttributesToFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
            DigitalAttributesFromFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
            AnalogAttributesFromFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
            SerialAttributesFromFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
            JoinMapStatic = new DynFusionJoinMap(1);
            Debug.Console(2, "Creating Fusion Symbol {0} {1}", _Config.Control.IpId, Key);
            FusionSymbol = new FusionRoom(_Config.Control.IpIdInt, Global.ControlSystem, String.IsNullOrEmpty(Name) ? "" : Name , Guid.NewGuid().ToString());

            if (FusionSymbol.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                Debug.Console(0, this, "Faliure to register Fusion Symbol");
            }
            FusionSymbol.ExtenderFusionRoomDataReservedSigs.Use();
            AddCustomAssets();


        }


        public override
            bool CustomActivate()
        {
            Initialize();
            return true;
        }

        private void AddCustomAssets()
        {
            if (_Config.Assets == null) return;
            if (_Config.Assets.OccupancySensors != null)
            {
                var sensors = from occSensorConfig in _Config.Assets.OccupancySensors
                    select
                        new DynFusionAssetOccupancySensor(
                            occSensorConfig.Key,
                            occSensorConfig.LinkToDeviceKey,
                            FusionSymbol,
                            GetNextAvailableAssetNumber(FusionSymbol), this);

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
            if (_Config.Assets.StaticAssets != null)
            {

                foreach (
                    var newStaticAsset in
                        _Config.Assets.StaticAssets.Select(staticAssetConfig => new DynFusionStaticAsset(
                            staticAssetConfig,
                            FusionSymbol,
                            GetNextAvailableAssetNumber(FusionSymbol), this)))
                {
                    AddPostActivationAction(newStaticAsset.LinkAllAttributeData);
                    DeviceManager.AddDevice(newStaticAsset);
                }
            }
        }

        private void Initialize()
        {
            try
            {
                Debug.Console(2, this, "Start Initialize");

                // Online Status 
                FusionOnlineFeedback = new BoolFeedback(() => FusionSymbol.IsOnline);
                FusionSymbol.OnlineStatusChange += FusionSymbol_OnlineStatusChange;

                // Attribute State Changes 
                FusionSymbol.FusionStateChange += FusionSymbol_FusionStateChange;
                FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange +=
                    FusionSymbol_RoomDataDeviceExtenderSigChange;

                // Create Custom Atributes 
                if (_Config.CustomAttributes != null)
                {
                    CreateDigitalAttributes();

                    CreateAnalogAttributes();

                    CreateSerialAttributes();
                }
                // Create Links for Standard joins 
                CreateStandardJoins();

                if (_Config.CustomProperties != null)
                {
                    if (_Config.CustomProperties.DigitalProperties != null)
                    {
                        foreach (var att in _Config.CustomProperties.DigitalProperties)
                        {
                            DigitalAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionDigitalAttribute(att.Id, att.JoinNumber));
                        }
                    }
                    if (_Config.CustomProperties.AnalogProperties != null)
                    {
                        foreach (var att in _Config.CustomProperties.AnalogProperties)
                        {
                            AnalogAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionAnalogAttribute(att.Id, att.JoinNumber));
                        }
                    }
                    if (_Config.CustomProperties.SerialProperties != null)
                    {
                        foreach (var att in _Config.CustomProperties.SerialProperties)
                        {
                            SerialAttributesFromFusion.Add(att.JoinNumber,
                                new DynFusionSerialAttribute(att.Id, att.JoinNumber));
                        }
                    }
                }



                if (_Config.CallStatistics != null)
                {
                    var callStats = from callStatsConfig in _Config.CallStatistics.Devices
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

                FusionRVI.GenerateFileForAllFusionDevices();
            }
            catch (Exception ex)
            {
                Debug.Console(2, this, "Exception DynFusion Initialize {0}", ex.Message);
            }
        }

        private void CreateStandardJoins()
        {
            if (JoinMapStatic == null) return;
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
            CreateStandardJoin(JoinMapStatic.ActionQuery,
                FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery);
            CreateStandardJoin(JoinMapStatic.RoomConfig,
                FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigQuery);
        }

        private void CreateSerialAttributes()
        {
            if (_Config.CustomAttributes.SerialAttributes == null) return;

            foreach (var att in _Config.CustomAttributes.SerialAttributes)
            {
                FusionSymbol.AddSig(eSigType.String, att.JoinNumber - FusionJoinOffset, att.Name,
                    GetIOMask(att.RwType));
                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
                {
                    SerialAttributesToFusion.Add(att.JoinNumber,
                        new DynFusionSerialAttribute(att.Name, att.JoinNumber));
                    SerialAttributesToFusion[att.JoinNumber].StringValueFeedback.LinkInputSig(
                        FusionSymbol.UserDefinedStringSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                }
                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
                {
                    SerialAttributesFromFusion.Add(att.JoinNumber,
                        new DynFusionSerialAttribute(att.Name, att.JoinNumber));
                }
            }
        }

        private void CreateAnalogAttributes()
        {
            if (_Config.CustomAttributes.AnalogAttributes == null) return;

            foreach (var att in _Config.CustomAttributes.AnalogAttributes)
            {
                FusionSymbol.AddSig(eSigType.UShort, att.JoinNumber - FusionJoinOffset, att.Name,
                    GetIOMask(att.RwType));

                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
                {
                    AnalogAttributesToFusion.Add(att.JoinNumber,
                        new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
                    AnalogAttributesToFusion[att.JoinNumber].UShortValueFeedback.LinkInputSig(
                        FusionSymbol.UserDefinedUShortSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                }
                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
                {
                    AnalogAttributesFromFusion.Add(att.JoinNumber,
                        new DynFusionAnalogAttribute(att.Name, att.JoinNumber));
                }
            }
        }

        private void CreateDigitalAttributes()
        {
            if (_Config.CustomAttributes.DigitalAttributes == null) return;
            foreach (var att in _Config.CustomAttributes.DigitalAttributes)
            {
                FusionSymbol.AddSig(eSigType.Bool, att.JoinNumber - FusionJoinOffset, att.Name,
                    GetIOMask(att.RwType));

                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read)
                {
                    DigitalAttributesToFusion.Add(att.JoinNumber,
                        new DynFusionDigitalAttribute(att));
                    DigitalAttributesToFusion[att.JoinNumber].BoolValueFeedback.LinkInputSig(
                        FusionSymbol.UserDefinedBooleanSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                }
                if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Write)
                {
                    DigitalAttributesFromFusion.Add(att.JoinNumber,
                        new DynFusionDigitalAttribute(att.Name, att.JoinNumber));
                }
            }
        }

        private void DeviceUsageFactory()
        {
            if (_Config.DeviceUsage == null) return;
            DeviceUsage = new DynFusionDeviceUsage(string.Format("{0}-DeviceUsage", Key), this);
            if (_Config.DeviceUsage.UsageMinThreshold > 0)
            {
                DeviceUsage.usageMinThreshold = (int) _Config.DeviceUsage.UsageMinThreshold;
            }

            if (_Config.DeviceUsage.Devices != null && _Config.DeviceUsage.Devices.Count > 0)
            {
                foreach (var device in _Config.DeviceUsage.Devices)
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
            if (_Config.DeviceUsage.Displays != null && _Config.DeviceUsage.Displays.Count > 0)
            {
                foreach (var display in _Config.DeviceUsage.Displays)
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
            if (_Config.DeviceUsage.Sources != null && _Config.DeviceUsage.Sources.Count > 0)
            {
                foreach (var source in _Config.DeviceUsage.Sources)
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

        private void CreateStandardJoin(JoinDataComplete join, BooleanSigDataFixedName Sig)
        {
            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
            {
                DigitalAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
            {
                DigitalAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
                DigitalAttributesToFusion[join.JoinNumber].BoolValueFeedback.LinkInputSig(Sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, UShortSigDataFixedName Sig)
        {
            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
            {
                AnalogAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
            {
                AnalogAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
                AnalogAttributesToFusion[join.JoinNumber].UShortValueFeedback.LinkInputSig(Sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, StringSigDataFixedName Sig)
        {
            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
            {
                SerialAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
            {
                SerialAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
                SerialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(Sig.InputSig);
            }
        }

        private void CreateStandardJoin(JoinDataComplete join, StringInputSig Sig)
        {
            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
            {
                SerialAttributesFromFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
            }

            if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
                join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
            {
                SerialAttributesToFusion.Add(join.JoinNumber,
                    new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
                SerialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(Sig);
            }
        }

        private void FusionSymbol_RoomDataDeviceExtenderSigChange(DeviceExtender currentDeviceExtender,
            SigEventArgs args)
        {
            Debug.Console(2, this,
                string.Format("DynFusion DeviceExtenderChange {0} {1} {2} {3}", currentDeviceExtender.ToString(),
                    args.Sig.Number, args.Sig.Type, args.Sig.StringValue));
            ushort joinNumber = (ushort) args.Sig.Number;

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
                    if (SerialAttributesFromFusion.TryGetValue(JoinMapStatic.AuthenticationSucceeded.JoinNumber,
                        out output))
                    {
                        output.StringValue = sigDetails.OutputSig.StringValue;
                    }
                    break;
                }
                case FusionEventIds.UserConfiguredBoolSigChangeEventId:
                {
                    var sigDetails = args.UserConfiguredSigDetail as BooleanSigData;
                    uint joinNumber = (uint) (sigDetails.Number + FusionJoinOffset);
                    DynFusionDigitalAttribute output;
                    Debug.Console(2, this, "DynFusion UserAttribute Digital Join:{0} Name:{1} Value:{2}", joinNumber,
                        sigDetails.Name, sigDetails.OutputSig.BoolValue);

                    if (DigitalAttributesFromFusion.TryGetValue(joinNumber, out output))
                    {
                        output.BoolValue = sigDetails.OutputSig.BoolValue;
                    }
                    break;
                }

                case FusionEventIds.UserConfiguredUShortSigChangeEventId:
                {
                    var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
                    uint joinNumber = (uint) (sigDetails.Number + FusionJoinOffset);
                    DynFusionAnalogAttribute output;
                    Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber,
                        sigDetails.Name, sigDetails.OutputSig.UShortValue);

                    if (AnalogAttributesFromFusion.TryGetValue(joinNumber, out output))
                    {
                        output.UShortValue = sigDetails.OutputSig.UShortValue;
                    }
                    break;
                }
                case FusionEventIds.UserConfiguredStringSigChangeEventId:
                {
                    var sigDetails = args.UserConfiguredSigDetail as StringSigData;
                    uint joinNumber = (uint) (sigDetails.Number + FusionJoinOffset);
                    DynFusionSerialAttribute output;
                    Debug.Console(2, this, "DynFusion UserAttribute Analog Join:{0} Name:{1} Value:{2}", joinNumber,
                        sigDetails.Name, sigDetails.OutputSig.StringValue);

                    if (SerialAttributesFromFusion.TryGetValue(joinNumber, out output))
                    {
                        output.StringValue = sigDetails.OutputSig.StringValue;
                    }
                    break;
                }
            }
        }

        private void FusionSymbol_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            FusionOnlineFeedback.FireUpdate();
            if (args.DeviceOnLine)
            {
                GetRoomConfig();
            }
        }

        private static eSigIoMask GetIOMask(eReadWrite mask)
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
                case eReadWrite.RW:
                    type = eSigIoMask.InputOutputSig;
                    break;
            }
            return (type);
        }

        private static eSigIoMask GetIOMask(string mask)
        {
            var _RWType = eSigIoMask.NA;

            switch (mask)
            {
                case "R":
                    _RWType = eSigIoMask.InputSigOnly;
                    break;
                case "W":
                    _RWType = eSigIoMask.OutputSigOnly;
                    break;
                case "RW":
                    _RWType = eSigIoMask.InputOutputSig;
                    break;
            }
            return (_RWType);
        }

        private static eReadWrite GeteReadWrite(eJoinCapabilities mask)
        {
            eReadWrite type = eReadWrite.ReadWrite;

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

        public uint GetNextAvailableAssetNumber(FusionRoom room)
        {
            uint slotNum = 0;
            foreach (var item in room.UserConfigurableAssetDetails)
            {
                slotNum = item.Number > slotNum ? item.Number : slotNum;
            }
            var returnValue = slotNum < 5 ? 5 : slotNum + 1;
            Debug.Console(2, this, "Next available fusion asset number is: {0}", returnValue);

            return returnValue;
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
            long errorlogThrottleTime = 60000;
            if (ErrorLogLastMessageSent != tempLogMessage)
            {
                ErrorLogLastMessageSent = tempLogMessage;
                if (ErrorLogTimer == null)
                {
                    ErrorLogTimer = new CTimer(o =>
                    {
                        Debug.Console(2, this, "Sent Message {0}", ErrorLogLastMessageSent);
                        FusionSymbol.ErrorMessage.InputSig.StringValue = ErrorLogLastMessageSent;
                    }, errorlogThrottleTime);
                }
                else
                {
                    ErrorLogTimer.Reset(errorlogThrottleTime);
                }
            }
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
                            var attirbute = SerialAttributesFromFusion.SingleOrDefault(x => x.Value.Name == "Name");

                            if (attirbute.Value != null)
                            {
                                attirbute.Value.StringValue = RoomInformation.Name;
                            }
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
                                    var attribute = DigitalAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                                    if (attribute.Value != null)
                                    {
                                        attribute.Value.BoolValue = Boolean.Parse(val);
                                    }
                                }
                                else if (type == "Integer")
                                {
                                    var attribute = AnalogAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                                    if (attribute.Value != null)
                                    {
                                        attribute.Value.UShortValue = uint.Parse(val);
                                    }
                                }
                                else if (type == "String" || type == "Text" || type == "URL")
                                {
                                    var attribute = SerialAttributesFromFusion.SingleOrDefault(x => x.Value.Name == id);

                                    if (attribute.Value != null)
                                    {
                                        attribute.Value.StringValue = val;
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
            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);
            var joinMap = new DynFusionJoinMap(joinStart);

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
            foreach (var att in SerialAttributesFromFusion)
            {
                var attLocal = att.Value;
                attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[attLocal.JoinNumber]);
            }

            trilist.SetSigTrueAction(joinMap.RoomConfig.JoinNumber, () => GetRoomConfig());

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
                foreach (var device in DeviceUsage.usageInfoDict)
                {
                    switch (device.Value.usageType)
                    {
                        case DynFusionDeviceUsage.UsageType.Display:
                        {
                            ushort x = device.Value.joinNumber;
                            trilist.SetUShortSigAction(device.Value.joinNumber,
                                (args) => DeviceUsage.changeSource(x, args));
                            break;
                        }
                        case DynFusionDeviceUsage.UsageType.Device:
                        {
                            ushort x = device.Value.joinNumber;
                            trilist.SetBoolSigAction(device.Value.joinNumber,
                                (args) => DeviceUsage.StartStopDevice(x, args));
                            break;
                        }
                    }
                }
            }
            trilist.OnlineStatusChange += (o, a) =>
            {
                if (a.DeviceOnLine)
                {
                    GetRoomConfig();
                    foreach (var att in SerialAttributesFromFusion)
                    {
                        var attLocal = att.Value;
                        var trilistLocal = o as BasicTriList;
                        trilistLocal.StringInput[attLocal.JoinNumber].StringValue = attLocal.StringValue;
                    }
                }
            };
        }
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