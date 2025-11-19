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
using PepperDash.Core.Logging;


namespace DynFusion
{
    public class DynFusionDevice : EssentialsBridgeableDevice, ILogStringsWithLevel, ILogStrings
    {
        public const ushort FusionJoinOffset = 49;
        //DynFusion Joins

        private readonly DynFusionConfigObjectTemplate _Config;
        private readonly Dictionary<uint, DynFusionDigitalAttribute> DigitalAttributesToFusion;
        private readonly Dictionary<uint, DynFusionAnalogAttribute> AnalogAttributesToFusion;
        private readonly Dictionary<uint, DynFusionSerialAttribute> SerialAttributesToFusion;
        private readonly Dictionary<uint, DynFusionDigitalAttribute> DigitalAttributesFromFusion;
        private readonly Dictionary<uint, DynFusionAnalogAttribute> AnalogAttributesFromFusion;
        private readonly Dictionary<uint, DynFusionSerialAttribute> SerialAttributesFromFusion;

        private readonly IDictionary<uint, DynFusionCallStatisticsDevice> _callStatisticsDevices
            = new Dictionary<uint, DynFusionCallStatisticsDevice>();

        private readonly IDictionary<uint, DynFusionStaticAsset> _staticAssets
            = new Dictionary<uint, DynFusionStaticAsset>();

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
            _Config = config;
            DigitalAttributesToFusion = new Dictionary<uint, DynFusionDigitalAttribute>();
            AnalogAttributesToFusion = new Dictionary<uint, DynFusionAnalogAttribute>();
            SerialAttributesToFusion = new Dictionary<uint, DynFusionSerialAttribute>();
            DigitalAttributesFromFusion = new Dictionary<uint, DynFusionDigitalAttribute>();
            AnalogAttributesFromFusion = new Dictionary<uint, DynFusionAnalogAttribute>();
            SerialAttributesFromFusion = new Dictionary<uint, DynFusionSerialAttribute>();
            this.LogVerbose("Creating Fusion Symbol {ipId:X}", _Config.Control.IpId, Key);
            FusionSymbol = new FusionRoom(_Config.Control.IpIdInt, Global.ControlSystem, "", Guid.NewGuid().ToString());

            if (FusionSymbol.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                this.LogError("Failure to register Fusion Symbol");
            }
            FusionSymbol.ExtenderFusionRoomDataReservedSigs.Use();
        }

        public override bool CustomActivate()
        {
            try
            {
                // Online Status 
                FusionOnlineFeedback = new BoolFeedback(() => { return FusionSymbol.IsOnline; });
                FusionSymbol.OnlineStatusChange += new OnlineStatusChangeEventHandler(FusionSymbol_OnlineStatusChange);

                // Attribute State Changes 
                FusionSymbol.FusionStateChange += new FusionStateEventHandler(FusionSymbol_FusionStateChange);
                FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange +=
                    new DeviceExtenderJoinChangeEventHandler(FusionSymbol_RoomDataDeviceExtenderSigChange);

                // Create Custom Atributes 
                SetupCustomAtributesDigital();
                SetupCustomAttributesAnalog();
                SetupCustomAttributesSerial();

                // Create Links for Standard joins 
                SetupStandardJoins();

                SetupCustomProperties();

                SetupAssets();

                SetupCallStatistics();

                DeviceUsageFactory();
                // Scheduling Bits for Future 
                //FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.Use();
                //FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(ExtenderRoomViewSchedulingDataReservedSigs_DeviceExtenderSigChange);

                // Future for time sync
                // FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(ExtenderFusionRoomDataReservedSigs_DeviceExtenderSigChange);
                if (string.IsNullOrEmpty(FusionSymbol.ParameterRoomName))
                {
                    FusionSymbol.ParameterRoomName = EthernetHelper.LanHelper.Hostname + "-program " + InitialParametersClass.ApplicationNumber + CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0) + CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, 0);

                }


                FusionRVI.GenerateFileForAllFusionDevices();
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion CustomActivate {message}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
            return true;
        }

        private void SetupCustomAtributesDigital()
        {
            try
            {
                if ((_Config.CustomAttributes?.DigitalAttributes) == null)
                {
                    return;
                }
                foreach (var att in _Config.CustomAttributes.DigitalAttributes)
                {
                    if (att == null || string.IsNullOrEmpty(att.Name))
                    {
                        continue;
                    }
                    FusionSymbol.AddSig(eSigType.Bool, att.JoinNumber - FusionJoinOffset, att.Name,
                        GetIOMask(att.RwType));

                    // Create single attribute instance for all access types
                    if (att.RwType != eReadWrite.ReadWrite && att.RwType != eReadWrite.Read && att.RwType != eReadWrite.Write)
                    {
                        continue;
                    }
                    DigitalAttributesToFusion.Add(att.JoinNumber,
                        new DynFusionDigitalAttribute(att.Name, att.JoinNumber, att.LinkDeviceKey ?? "",
                            att.LinkDeviceMethod ?? "", att.LinkDeviceFeedback ?? ""));

                    // Setup input signal linking for readable attributes
                    if (att.RwType != eReadWrite.ReadWrite && att.RwType != eReadWrite.Read)
                    {
                        continue;
                    }
                    if (FusionSymbol.UserDefinedBooleanSigDetails == null || FusionSymbol.UserDefinedBooleanSigDetails.Count <= (att.JoinNumber - FusionJoinOffset))
                    {
                        continue;
                    }
                    DigitalAttributesToFusion[att.JoinNumber].BoolValueFeedback.LinkInputSig(
                        FusionSymbol.UserDefinedBooleanSigDetails[att.JoinNumber - FusionJoinOffset].InputSig);
                }
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupCustomAtributesDigital {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void SetupCustomAttributesAnalog()
        {
            try
            {
                if ((_Config.CustomAttributes?.AnalogAttributes) == null)
                {
                    return;
                }

                foreach (var att in _Config.CustomAttributes.AnalogAttributes)
                {
                    if (att != null && !string.IsNullOrEmpty(att.Name))
                    {
                        FusionSymbol.AddSig(eSigType.UShort, att.JoinNumber - FusionJoinOffset, att.Name,
                            GetIOMask(att.RwType));

                        // Create single attribute instance for all access types
                        if (att.RwType == eReadWrite.ReadWrite || att.RwType == eReadWrite.Read || att.RwType == eReadWrite.Write)
                        {
                            AnalogAttributesToFusion.Add(att.JoinNumber,
                                new DynFusionAnalogAttribute(att.Name, att.JoinNumber, att.LinkDeviceKey ?? "",
                                    att.LinkDeviceMethod ?? "", att.LinkDeviceFeedback ?? ""));

                            // Setup input signal linking for readable attributes
                            if (att.RwType != eReadWrite.ReadWrite && att.RwType != eReadWrite.Read)
                            {
                                continue;
                            }

                            if (FusionSymbol.UserDefinedUShortSigDetails == null)
                            {
                                continue;
                            }

                            var targetFusionJoin = att.JoinNumber - FusionJoinOffset;

                            foreach (var sigDetail in FusionSymbol.UserDefinedUShortSigDetails)
                            {
                                if (sigDetail == null || sigDetail.Number != targetFusionJoin)
                                {
                                    continue;
                                }
                                AnalogAttributesToFusion[att.JoinNumber].UShortValueFeedback.LinkInputSig(
                                    sigDetail.InputSig);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupCustomAttributesAnalog {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void SetupCustomAttributesSerial()
        {
            try
            {
                if ((_Config.CustomAttributes?.SerialAttributes) == null)
                {
                    return;
                }

                foreach (var att in _Config.CustomAttributes.SerialAttributes)
                {
                    if (att == null || string.IsNullOrEmpty(att.Name))
                    {
                        continue;
                    }

                    FusionSymbol.AddSig(eSigType.String, att.JoinNumber - FusionJoinOffset, att.Name,
                        GetIOMask(att.RwType));

                    // Create single attribute instance for all access types
                    if (att.RwType != eReadWrite.ReadWrite && att.RwType != eReadWrite.Read && att.RwType != eReadWrite.Write)
                    {
                        continue;
                    }

                    SerialAttributesToFusion.Add(att.JoinNumber,
                        new DynFusionSerialAttribute(att.Name, att.JoinNumber, att.LinkDeviceKey ?? "",
                            att.LinkDeviceMethod ?? "", att.LinkDeviceFeedback ?? ""));

                    // Setup input signal linking for readable attributes
                    if (att.RwType != eReadWrite.ReadWrite && att.RwType != eReadWrite.Read)
                    {
                        continue;
                    }

                    var targetFusionJoin = att.JoinNumber - FusionJoinOffset;
                    if (FusionSymbol.UserDefinedStringSigDetails == null)
                    {
                        continue;
                    }

                    foreach (var sigDetail in FusionSymbol.UserDefinedStringSigDetails)
                    {
                        if (sigDetail == null || sigDetail.Number != targetFusionJoin)
                        {
                            continue;
                        }

                        SerialAttributesToFusion[att.JoinNumber].StringValueFeedback.LinkInputSig(
                            sigDetail.InputSig);

                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupCustomAttributesSerial {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void SetupStandardJoins()
        {
            try
            {
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
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupStandardJoins {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void SetupCustomProperties()
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupCustomProperties {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void SetupAssets()
        {
            try
            {
                if (_Config.Assets != null)
                {
                    if (_Config.Assets.OccupancySensors != null)
                    {
                        var sensors = from occSensorConfig in _Config.Assets.OccupancySensors
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

                    this.LogDebug("DynFusionDevice SetupAsset: StaticAssets config {staticAssetsConfig} null",
                        _Config.Assets.StaticAssets == null ? "==" : "!=");

                    if (_Config.Assets.StaticAssets != null)
                    {
                        var staticAssets = from staticAssetsConfig in _Config.Assets.StaticAssets
                                           select
                                               new DynFusionStaticAsset(
                                                   this,
                                                   FusionSymbol,
                                                   GetNextAvailableAssetNumber(FusionSymbol),
                                                   staticAssetsConfig);

                        staticAssets
                            .ToList()
                            .ForEach(staticAsset =>
                            {
                                _staticAssets.Add(staticAsset.AssetNumber, staticAsset);
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupAssets {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }


        private void SetupCallStatistics()
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                this.LogError("Exception DynFusion SetupCallStatistics {0}", ex.Message);
                this.LogDebug(ex, "Stack Trace: ");
            }
        }

        private void DeviceUsageFactory()
        {
            if (_Config.DeviceUsage != null)
            {
                DeviceUsage = new DynFusionDeviceUsage(string.Format("{0}-DeviceUsage", Key), this);
                if (_Config.DeviceUsage.UsageMinThreshold > 0)
                {
                    DeviceUsage.usageMinThreshold = _Config.DeviceUsage.UsageMinThreshold;
                }

                if (_Config.DeviceUsage.Devices != null && _Config.DeviceUsage.Devices.Count > 0)
                {
                    foreach (var device in _Config.DeviceUsage.Devices)
                    {
                        try
                        {
                            this.LogDebug("Creating Device: {deviceJoinNumber}, {deviceType}, {deviceName}", device.JoinNumber, device.Type,
                                device.Name);
                            DeviceUsage.CreateDevice(device.JoinNumber, device.Type, device.Name);
                        }
                        catch (Exception ex)
                        {
                            this.LogError("Error creating device: {0}", ex.Message);
                            this.LogDebug(ex, "Stack Trace: ");
                        }
                    }
                }
                if (_Config.DeviceUsage.Displays != null && _Config.DeviceUsage.Displays.Count > 0)
                {
                    foreach (var display in _Config.DeviceUsage.Displays)
                    {
                        try
                        {
                            this.LogDebug("Creating Display: {displayJoinNumber}, {displayName}", display.JoinNumber, display.Name);
                            DeviceUsage.CreateDisplay(display.JoinNumber, display.Name);
                        }
                        catch (Exception ex)
                        {
                            this.LogError("Error creating display: {0}", ex.Message);
                            this.LogDebug(ex, "Stack Trace: ");
                        }
                    }
                }
                if (_Config.DeviceUsage.Sources != null && _Config.DeviceUsage.Sources.Count > 0)
                {
                    foreach (var source in _Config.DeviceUsage.Sources)
                    {
                        try
                        {
                            this.LogVerbose("Creating Source: {sourceNumber}, {sourceName}", source.SourceNumber, source.Name);
                            DeviceUsage.CreateSource(source.SourceNumber, source.Name, source.Type);
                        }
                        catch (Exception ex)
                        {
                            this.LogError("Error creating source: {0}", ex.Message);
                            this.LogDebug(ex, "Stack Trace: ");
                        }
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
            this.LogVerbose("DynFusion DeviceExtenderChange {currentDeviceExtender} {sigNumber} {sigType} {sigStringValue}", currentDeviceExtender.ToString(),
                args.Sig.Number, args.Sig.Type, args.Sig.StringValue);

            var joinNumber = (ushort)args.Sig.Number;

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
            this.LogVerbose("DynFusion FusionStateChange {eventId} {sigDetail}", args.EventId,
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
                        var joinNumber = sigDetails.Number + FusionJoinOffset;
                        DynFusionDigitalAttribute output;

                        this.LogVerbose("DynFusion UserAttribute Digital Join:{joinNumber} Name:{joinName} Value:{value}", joinNumber,
                            sigDetails.Name, sigDetails.OutputSig.BoolValue);

                        if (DigitalAttributesFromFusion.TryGetValue(joinNumber, out output))
                        {
                            output.BoolValue = sigDetails.OutputSig.BoolValue;
                            output.CallAction(output.BoolValue);
                        }
                        break;
                    }

                case FusionEventIds.UserConfiguredUShortSigChangeEventId:
                    {
                        var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
                        var joinNumber = sigDetails.Number + FusionJoinOffset;
                        DynFusionAnalogAttribute output;

                        this.LogVerbose("DynFusion UserAttribute Analog Join:{joinNumber} Name:{joinName} Value:{value}", joinNumber,
                            sigDetails.Name, sigDetails.OutputSig.UShortValue);

                        if (AnalogAttributesFromFusion.TryGetValue(joinNumber, out output))
                        {
                            output.UShortValue = sigDetails.OutputSig.UShortValue;
                            output.CallAction(output.UShortValue);
                        }
                        break;
                    }
                case FusionEventIds.UserConfiguredStringSigChangeEventId:
                    {
                        var sigDetails = args.UserConfiguredSigDetail as StringSigData;
                        var joinNumber = sigDetails.Number + FusionJoinOffset;
                        DynFusionSerialAttribute output;

                        this.LogVerbose("DynFusion UserAttribute Analog Join:{joinNumber} Name:{joinName} Value:{value}", joinNumber,
                             sigDetails.Name, sigDetails.OutputSig.StringValue);

                        if (SerialAttributesFromFusion.TryGetValue(joinNumber, out output))
                        {
                            output.StringValue = sigDetails.OutputSig.StringValue;
                            output.CallAction(output.StringValue);
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

        public static eSigIoMask GetIOMask(eReadWrite mask)
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
            return type;
        }

        public static eSigIoMask GetIOMask(string mask)
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
            return _RWType;
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
            return type;
        }

        public static uint GetNextAvailableAssetNumber(FusionRoom room)
        {
            uint slotNumber = 0;
            foreach (var item in room.UserConfigurableAssetDetails)
            {
                if (item.Number > slotNumber)
                {
                    slotNumber = item.Number;
                }
            }
            if (slotNumber < 5)
            {
                slotNumber = 5;
            }
            else
                slotNumber++;

            Debug.LogVerbose("Next available fusion asset number is: {slotNumber}", slotNumber);

            return slotNumber;
        }

        #region Overrides of EssentialsBridgeableDevice

        public void GetRoomConfig()
        {
            try
            {
                if (FusionSymbol.IsOnline)
                {
                    string fusionRoomConfigRequest =
                        string.Format(
                            "<RequestRoomConfiguration><RequestID>RoomConfigurationRequest</RequestID><CustomProperties><Property></Property></CustomProperties></RequestRoomConfiguration>");

                    this.LogVerbose("Room Request: {request}", fusionRoomConfigRequest);
                    FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomConfigQuery.StringValue =
                        fusionRoomConfigRequest;
                }
            }
            catch (Exception e)
            {
                this.LogError("GetRoomConfig exception: {message}", e.Message);
                this.LogDebug(e, "Stack Trace: ");
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
                        this.LogVerbose("SendToLog Message:{message}", ErrorLogLastMessageSent);
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
                                        attribute.Value.BoolValue = bool.Parse(val);
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

                                this.LogVerbose("RoomConfigParseData {type} {id} {val}", type, id, val);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                this.LogError("GetRoomConfig exception: {message}", e.Message);
                this.LogDebug(e, "Stack Trace: ");
            }
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            this.LogDebug("Linking to Trilist '{ipId}'", trilist.ID.ToString("X"));
            this.LogDebug("Linking to Bridge AssetType {type}", GetType().Name);
            var joinMap = new DynFusionJoinMap(joinStart);
            JoinMapStatic = joinMap;

            bridge.AddJoinMap(Key, joinMap);

            FusionOnlineFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Online.JoinNumber]);

            foreach (var att in DigitalAttributesToFusion)
            {
                var attLocal = att.Value;
                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Digital,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.FromSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }

                trilist.SetBoolSigAction(attLocal.JoinNumber, (b) => { attLocal.BoolValue = b; });
            }
            foreach (var att in DigitalAttributesFromFusion)
            {
                var attLocal = att.Value;

                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Digital,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.ToSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }

                attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[attLocal.JoinNumber]);
            }
            foreach (var att in AnalogAttributesToFusion)
            {
                var attLocal = att.Value;

                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Analog,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.FromSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }
                trilist.SetUShortSigAction(attLocal.JoinNumber, (a) => { attLocal.UShortValue = a; });
            }
            foreach (var att in AnalogAttributesFromFusion)
            {
                var attLocal = att.Value;

                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Analog,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.ToSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }

                attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[attLocal.JoinNumber]);
            }

            foreach (var att in SerialAttributesToFusion)
            {
                var attLocal = att.Value;

                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Serial,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.FromSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }
                trilist.SetStringSigAction(attLocal.JoinNumber, (a) => { attLocal.StringValue = a; });
            }
            foreach (var att in SerialAttributesFromFusion)
            {
                var attLocal = att.Value;

                var joinData = new JoinDataComplete(new JoinData { JoinNumber = attLocal.JoinNumber, JoinSpan = 1 },
                    new JoinMetadata
                    {
                        JoinType = eJoinType.Serial,
                        Description = attLocal.Name,
                        JoinCapabilities = eJoinCapabilities.ToSIMPL
                    });

                if (!joinMap.Joins.ContainsKey(attLocal.Name))
                {
                    joinMap.Joins.Add(attLocal.Name, joinData);
                }
                attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[attLocal.JoinNumber]);
            }

            trilist.SetSigTrueAction(joinMap.RoomConfig.JoinNumber, () => GetRoomConfig());

            foreach (var staticAsset in _staticAssets)
            {
                var device = staticAsset.Value;
                device.LinkToApi(trilist, joinStart, joinMapKey, bridge);
            }

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