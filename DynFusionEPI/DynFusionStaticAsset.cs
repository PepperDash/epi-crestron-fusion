﻿using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Fusion;
using DynFusion.Config;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace DynFusion
{
    public class DynFusionStaticAsset : EssentialsBridgeableDevice
    {
        private readonly FusionRoom _fusionSymbol;
        private FusionStaticAssetConfig Config { get; set; }
        public readonly uint AssetNumber;
        public readonly string Type;
        private readonly FusionStaticAsset _asset;

        private DynFusionStaticAssetJoinMapDynamic JoinMap { get; set; }
        private BasicTriList Trilist { get; set; }

        public List<DynFusionAttributeBase> Attributes = new List<DynFusionAttributeBase>(); 
        public List<DynFusionAttributeBase> CustomAttributes = new List<DynFusionAttributeBase>();

        public const int FusionCustomAssetJoinOffset = 49;


        public DynFusionStaticAsset(FusionStaticAssetConfig config, FusionRoom symbol, uint assetNumber, IKeyed parent)
            : base(string.Format("{0}-Asset-{1}", parent.Key, config.Key))
        {
            Config = config;
            Type = Config.Type;
            _fusionSymbol = symbol;
            AssetNumber = assetNumber;
            //Key = string.Format("{0}-Asset-{1}", parent.Key, Config.Key);

            Debug.Console(0, this, "Key = {0}", Key);


            _fusionSymbol.AddAsset(eAssetType.StaticAsset, AssetNumber, config.Name, Type, Guid.NewGuid().ToString());
            _asset = _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset as FusionStaticAsset;

            try
            {
                Debug.Console(0, this, "{0}", _asset == null ? "Asset Is Null" : "Running Setup");
                if (_asset == null) return;
                _asset.PowerOn.AddSigToRVIFile = false;
                _asset.PowerOff.AddSigToRVIFile = false;
                _asset.Connected.AddSigToRVIFile = false;
                _asset.AssetError.AddSigToRVIFile = false;
                _asset.AssetUsage.AddSigToRVIFile = false;
                _asset.ParamMake.Value = Config.Make.NullIfEmpty() ?? string.Empty;
                _asset.ParamModel.Value = Config.Model.NullIfEmpty() ?? string.Empty;

                var modelDynamic = Config.Attributes.SerialAttributes.FirstOrDefault(o => o.Name.ToLower() == "model");
                var makeDynamic = Config.Attributes.SerialAttributes.FirstOrDefault(o => o.Name.ToLower() == "make");
                var connectedDynamic = Config.Attributes.DigitalAttributes.FirstOrDefault(
                        o => o.Name.ToLower().Replace(" ", string.Empty) == "powerison");
                var powerOnDynamic =
                    Config.Attributes.DigitalAttributes.FirstOrDefault(
                        o => o.Name.ToLower().Replace(" ", string.Empty) == "poweron");
                var powerOffDynamic =
                    Config.Attributes.DigitalAttributes.FirstOrDefault(
                        o => o.Name.ToLower().Replace(" ", string.Empty) == "poweroff");
                var assetErrorDynamic =
                    Config.Attributes.SerialAttributes.FirstOrDefault(
                        o => o.Name.ToLower().Replace(" ", string.Empty) == "asseterror");
                var assetUsageDynamic =
                    Config.Attributes.SerialAttributes.FirstOrDefault(
                        o => o.Name.ToLower().Replace(" ", string.Empty) == "assetusage");


                if (modelDynamic != null)
                {
                    Config.Attributes.SerialAttributes.Remove(modelDynamic);
                    var model = new DynFusionSerialAttribute(modelDynamic).StringValue;
                    _asset.ParamModel.Value = model;
                }
                if (makeDynamic != null)
                {
                    Config.Attributes.SerialAttributes.Remove(makeDynamic);
                    var make = new DynFusionSerialAttribute(makeDynamic).StringValue;
                    _asset.ParamModel.Value = make;
                }

                if (connectedDynamic != null)
                {
                    Config.Attributes.DigitalAttributes.Remove(connectedDynamic);
                    connectedDynamic.JoinNumber = _asset.Connected.Number;
                    var connected = new DynFusionDigitalAttribute(connectedDynamic);
                    Attributes.Add(connected);
                    _asset.Connected.AddSigToRVIFile = true;
                }

                if (powerOnDynamic != null)
                {
                    Config.Attributes.DigitalAttributes.Remove(powerOnDynamic);
                    powerOnDynamic.JoinNumber = _asset.PowerOn.Number;
                    var powerOn = new DynFusionDigitalAttribute(powerOnDynamic);
                    Attributes.Add(powerOn);
                    _asset.PowerOn.AddSigToRVIFile = true;
                }

                if (powerOffDynamic != null)
                {
                    Config.Attributes.DigitalAttributes.Remove(powerOffDynamic);
                    powerOffDynamic.JoinNumber = _asset.PowerOff.Number;
                    var powerOff = new DynFusionDigitalAttribute(powerOffDynamic);
                    Attributes.Add(powerOff);
                    _asset.PowerOff.AddSigToRVIFile = true;
                }

                if (assetErrorDynamic != null)
                {
                    Config.Attributes.SerialAttributes.Remove(assetErrorDynamic);
                    assetErrorDynamic.JoinNumber = _asset.AssetError.Number;
                    var assetError = new DynFusionSerialAttribute(assetErrorDynamic);
                    Attributes.Add(assetError);
                    _asset.AssetError.AddSigToRVIFile = true;
                }

                if (assetUsageDynamic != null)
                {
                    Config.Attributes.SerialAttributes.Remove(assetUsageDynamic);
                    assetUsageDynamic.JoinNumber = _asset.AssetUsage.Number;
                    var assetUsage = new DynFusionSerialAttribute(assetUsageDynamic);
                    Attributes.Add(assetUsage);
                    _asset.AssetUsage.AddSigToRVIFile = true;
                }

                Debug.Console(1, this, "Adding Serial Attributes");
                foreach (
                    var dynAttribute in
                        Config.Attributes.SerialAttributes.Select(
                            attribute => new DynFusionSerialAttribute(attribute)))
                {
                    CustomAttributes.Add(dynAttribute);
                    AddSigHelper(dynAttribute);
                }
                Debug.Console(1, this, "Adding Digital Attributes");

                foreach (
                    var dynAttribute in
                        Config.Attributes.DigitalAttributes.Select(
                            attribute => new DynFusionDigitalAttribute(attribute)))
                {
                    CustomAttributes.Add(dynAttribute);
                    AddSigHelper(dynAttribute);
                }
                Debug.Console(0, this, "Adding Analog Attributes");

                foreach (
                    var dynAttribute in
                        Config.Attributes.AnalogAttributes.Select(
                            attribute => new DynFusionAnalogAttribute(attribute)))
                {
                    CustomAttributes.Add(dynAttribute);
                    AddSigHelper(dynAttribute);
                }
                Debug.Console(2, this, "There are {0} Base Attributes", Attributes.Count);
                Debug.Console(2, this, "There are {0} Custom Attributes", CustomAttributes.Count);
            }

            catch (Exception ex)
            {
                Debug.Console(2, this, "Problem Initializing Static Asset : {0}", ex.Message);
            }

        }

        public void LinkAllAttributeData()
        {
            LinkBaseAttributeData();
            LinkCustomAttributeData();
        }


        private void LinkBaseAttributeData()
        {
            foreach (var item in Attributes)
            {
                var attribute = item;
                LinkItem(attribute);
            }
        }
        private void LinkCustomAttributeData()
        {
            foreach (var item in CustomAttributes)
            {
                var asset = item;
                asset.LinkData();
            }
        }


        private static void LinkItem(DynFusionAttributeBase attribute)
        {
            switch (attribute.SignalType)
            {
                case (eSigType.Bool):
                    attribute = attribute as DynFusionDigitalAttribute;
                    if (attribute == null) return;
                    attribute.LinkData();
                    break;
                case (eSigType.String):
                    attribute = attribute as DynFusionSerialAttribute;
                    if (attribute == null) return;
                    attribute.LinkData();
                    break;
                case (eSigType.UShort):
                    attribute = attribute as DynFusionAnalogAttribute;
                    if (attribute == null) return;
                    attribute.LinkData();
                    break;
            }
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new DynFusionStaticAssetJoinMapDynamic(joinStart, Attributes, CustomAttributes);
            // No Custom Joinmap Allowed Here
            if(bridge != null) bridge.AddJoinMap(Key, joinMap);
            JoinMap = joinMap;
            Trilist = trilist;



            LinkBaseAttributes();
            LinkCustomAttributes();

            trilist.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                LinkBaseAttributes();
                LinkCustomAttributes();
            };
        }

        private void LinkBaseAttributes()
        {
            foreach (var baseAttribute in Attributes)
            {
                JoinDataComplete joinData;
                var attribute = baseAttribute;

                if (!JoinMap.Joins.TryGetValue(attribute.Name, out joinData)) return;
                LinkBaseAttribute(joinData, Trilist, attribute);
            }
        }

        private void LinkCustomAttributes()
        {
            foreach (var customAttribute in CustomAttributes)
            {
                JoinDataComplete joinData;
                var attribute = customAttribute;

                if (!JoinMap.Joins.TryGetValue(attribute.Name, out joinData)) return;
                LinkCustomAttribute(joinData, Trilist, attribute);
            }

        }

        private void AddSigHelper(DynFusionDigitalAttribute attribute)
        {
            var sigNumber = attribute.JoinNumber - FusionCustomAssetJoinOffset;
            _asset.AddSig(eSigType.Bool, sigNumber, attribute.Name, (eSigIoMask)(int)attribute.RwType);
            if ((int)attribute.RwType == 1 || (int)attribute.RwType == 3)
            {
                var testBoolOutput =
                    _asset.FusionGenericAssetDigitalsAsset1.BooleanOutput[sigNumber];
                var debugData = testBoolOutput == null
                    ? string.Format("Asset {0} Output Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Output Attribute {1} is valid", Key, attribute.Name);
                Debug.Console(2, this, debugData);
            }
            if ((int)attribute.RwType == 2 || (int)attribute.RwType == 3)
            {
                var testOutput =
                    _asset.FusionGenericAssetDigitalsAsset1.BooleanInput[sigNumber];
                var debugData = testOutput == null
                    ? string.Format("Asset {0} Input Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Input Attribute {1} is valid", Key, attribute.Name);
                Debug.Console(2, this, debugData);
            }
        }

        private void AddSigHelper(DynFusionAnalogAttribute attribute)
        {
            var sigNumber = attribute.JoinNumber - FusionCustomAssetJoinOffset;
            _asset.AddSig(eSigType.UShort, sigNumber, attribute.Name, (eSigIoMask)(int)attribute.RwType);
            if ((int)attribute.RwType == 1 || (int)attribute.RwType == 3)
            {
                var testOutput =
                    _asset.FusionGenericAssetAnalogsAsset2.UShortOutput[sigNumber];
                var debugData = testOutput == null
                    ? string.Format("Asset {0} Output Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Output Attribute {1} is valid with the number {2}", Key, attribute.Name, testOutput.Number);
                Debug.Console(2, this, debugData);
            }
            if ((int)attribute.RwType == 2 || (int)attribute.RwType == 3)
            {
                var testOutput =
                    _asset.FusionGenericAssetAnalogsAsset2.UShortInput[sigNumber];
                var debugData = testOutput == null
                    ? string.Format("Asset {0} Input Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Input Attribute {1} is valid", Key, attribute.Name);
                Debug.Console(2, this, debugData);
            }
        }

        private void AddSigHelper(DynFusionSerialAttribute attribute)
        {
            var sigNumber = attribute.JoinNumber - FusionCustomAssetJoinOffset;
            _asset.AddSig(eSigType.String, sigNumber, attribute.Name, (eSigIoMask)(int)attribute.RwType);
            if ((int)attribute.RwType == 1 || (int)attribute.RwType == 3)
            {
                var testOutput =
                    _asset.FusionGenericAssetSerialsAsset3.StringOutput[sigNumber];
                var debugData = testOutput == null
                    ? string.Format("Asset {0} Output Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Output Attribute {1} is valid with the number {2} and name {3}", Key, attribute.Name, testOutput.Number, testOutput.Name);
                Debug.Console(2, this, debugData);
            }
            if ((int)attribute.RwType == 2 || (int)attribute.RwType == 3)
            {
                var testOutput =
                    _asset.FusionGenericAssetSerialsAsset3.StringInput[sigNumber];
                var debugData = testOutput == null
                    ? string.Format("Asset {0} Input Attribute {1} is invalid", Key, attribute.Name)
                    : string.Format("Asset {0} Input Attribute {1} is valid with the number {2} and name {3}", Key, attribute.Name, testOutput.Number, testOutput.Name);
                Debug.Console(2, this, debugData);
            }
        }

        private void LinkCustomAttribute(JoinDataComplete joinData, BasicTriList trilist, DynFusionAttributeBase attribute)
        {
            switch (joinData.Metadata.JoinType)
            {
                case eJoinType.Digital:
                    LinkDigitalAttribute(joinData, trilist, attribute);
                    break;
                case eJoinType.Analog:
                    LinkAnalogAttribute(joinData, trilist, attribute);
                    break;
                case eJoinType.Serial:
                    LinkSerialAttribute(joinData, trilist, attribute);
                    break;
            }
        }

        private void LinkDigitalAttribute(JoinDataComplete joinData, BasicTriList trilist, DynFusionAttributeBase attribute)
        {
            var dynFusionDigitalAttribute = attribute as DynFusionDigitalAttribute;
            uint sigNumber = 0;
            if (attribute.RwType == eReadWrite.Read || attribute.RwType == eReadWrite.ReadWrite)
            {
                _fusionSymbol.FusionAssetStateChange += (d, a) =>
                {
                    if (a.UserConfigurableAssetDetailIndex != AssetNumber) return;
                    if (a.EventId != FusionAssetEventId.StaticAssetAssetBoolAssetSigEventReceivedEventId) return;
                    var sigDetails = a.UserConfiguredSigDetail as BooleanSigData;
                    if (sigDetails == null) return;
                    if (sigDetails.Name != attribute.Name) return;
                    sigNumber = sigDetails.Number;
                    if (attribute.AttributeAction != null && sigDetails.OutputSig.BoolValue)
                        attribute.AttributeAction.Invoke();
                    trilist.BooleanInput[joinData.JoinNumber].BoolValue = sigDetails.OutputSig.BoolValue;
                };
            }
            if (attribute.RwType != eReadWrite.Write && attribute.RwType != eReadWrite.ReadWrite) return;
            trilist.SetBoolSigAction(joinData.JoinNumber, b =>
            {
                if (attribute.AttributeAction != null && b) attribute.AttributeAction.Invoke();
                _asset.FusionGenericAssetDigitalsAsset1.BooleanInput[attribute.JoinNumber].BoolValue = b;
            });
            if (dynFusionDigitalAttribute == null) return;
            Debug.Console(2, this, "DynFusionDigitalAttribute for {0} is valid", attribute.Name);
            if (dynFusionDigitalAttribute.BoolValueFeedback == null) return;
            Debug.Console(2, this, "DynFusionDigitalAttribute Feedback for {0} is valid", attribute.Name);

            dynFusionDigitalAttribute.BoolValueFeedback.OutputChange += (s, a) =>
            {
                Debug.Console(2, this, "Feedback Fired for {0}", attribute.Name);
                if (a == null) return;
                _asset.FusionGenericAssetDigitalsAsset1.BooleanInput[attribute.JoinNumber].BoolValue = a.BoolValue;
            };

            _fusionSymbol.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                _asset.FusionGenericAssetDigitalsAsset1.BooleanInput[attribute.JoinNumber].BoolValue =
                    dynFusionDigitalAttribute.BoolValue;
                if (sigNumber == 0) return;
                trilist.BooleanInput[joinData.JoinNumber].BoolValue =
                    _asset.FusionGenericAssetDigitalsAsset1.BooleanOutput[sigNumber].BoolValue;
            };

            trilist.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                if (sigNumber == 0) return;
                trilist.BooleanInput[joinData.JoinNumber].BoolValue =
                    _asset.FusionGenericAssetDigitalsAsset1.BooleanOutput[sigNumber].BoolValue;
            };

        }

        private void LinkAnalogAttribute(JoinDataComplete joinData, BasicTriList trilist,
            DynFusionAttributeBase attribute)
        {
            var dynFusionAnalogAttribute = attribute as DynFusionAnalogAttribute;
            uint sigNumber = 0;
            if (attribute.RwType == eReadWrite.Read || attribute.RwType == eReadWrite.ReadWrite)
            {
                _fusionSymbol.FusionAssetStateChange += (d, a) =>
                {
                    if (a.UserConfigurableAssetDetailIndex != AssetNumber) return;
                    if (a.EventId != FusionAssetEventId.StaticAssetAssetUshortAssetSigEventReceivedEventId) return;
                    var sigDetails = a.UserConfiguredSigDetail as UShortSigData;
                    if (sigDetails == null) return;
                    if (sigDetails.Name != attribute.Name) return;
                    sigNumber = sigDetails.Number;
                    if (attribute.AttributeAction != null)
                        attribute.AttributeAction.Invoke();
                    trilist.UShortInput[joinData.JoinNumber].UShortValue = sigDetails.OutputSig.UShortValue;
                };
            }
            if (attribute.RwType != eReadWrite.Write && attribute.RwType != eReadWrite.ReadWrite) return;
            trilist.SetUShortSigAction(joinData.JoinNumber, u =>
            {
                if (attribute.AttributeAction != null) attribute.AttributeAction.Invoke();
                _asset.FusionGenericAssetAnalogsAsset2.UShortInput[attribute.JoinNumber].UShortValue = u;
            });
            if (dynFusionAnalogAttribute == null) return;
            Debug.Console(2, this, "DynFusionAnalogAttribute for {0} is valid", attribute.Name);

            if (dynFusionAnalogAttribute.UShortValueFeedback == null) return;
            Debug.Console(2, this, "DynFusionAnalogAttribute Feedback for {0} is valid", attribute.Name);

            dynFusionAnalogAttribute.UShortValueFeedback.OutputChange += (s, a) =>
            {
                Debug.Console(2, this, "Feedback Fired for {0}", attribute.Name);
                if (a == null) return;
                _asset.FusionGenericAssetAnalogsAsset2.UShortInput[attribute.JoinNumber].UShortValue = a.UShortValue;
            };
            _fusionSymbol.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                _asset.FusionGenericAssetAnalogsAsset2.UShortInput[attribute.JoinNumber].UShortValue =
                    (ushort)dynFusionAnalogAttribute.UShortValue;
                if (sigNumber == 0) return;
                trilist.UShortInput[joinData.JoinNumber].UShortValue =
                    _asset.FusionGenericAssetAnalogsAsset2.UShortOutput[sigNumber].UShortValue;
            };
            trilist.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                if (sigNumber == 0) return;
                trilist.UShortInput[joinData.JoinNumber].UShortValue =
                    _asset.FusionGenericAssetAnalogsAsset2.UShortOutput[sigNumber].UShortValue;

            };


        }

        private void LinkSerialAttribute(JoinDataComplete joinData, BasicTriList trilist, DynFusionAttributeBase attribute)
        {
            var dynFusionSerialAttribute = attribute as DynFusionSerialAttribute;
            uint sigNumber = 0;
            if (attribute.RwType == eReadWrite.Read || attribute.RwType == eReadWrite.ReadWrite)
            {
                _fusionSymbol.FusionAssetStateChange += (d, a) =>
                {
                    if (a.UserConfigurableAssetDetailIndex != AssetNumber) return;
                    if (a.EventId != FusionAssetEventId.StaticAssetAssetStringAssetSigEventReceivedEventId) return;
                    var sigDetails = a.UserConfiguredSigDetail as StringSigData;
                    if (sigDetails == null) return;
                    if (sigDetails.Name != attribute.Name) return;
                    sigNumber = sigDetails.Number;
                    if (attribute.AttributeAction != null)
                        attribute.AttributeAction.Invoke();
                    trilist.StringInput[joinData.JoinNumber].StringValue = sigDetails.OutputSig.StringValue;
                };
            }
            if (attribute.RwType != eReadWrite.Write && attribute.RwType != eReadWrite.ReadWrite) return;
            trilist.SetStringSigAction(joinData.JoinNumber, s => 
            {
                if (attribute.AttributeAction != null) attribute.AttributeAction.Invoke();
                _asset.FusionGenericAssetSerialsAsset3.StringInput[attribute.JoinNumber].StringValue = s;
            });
            if (dynFusionSerialAttribute == null) return;
            Debug.Console(2, this, "DynFusionSerialAttribute for {0} is valid", attribute.Name);
            if (dynFusionSerialAttribute.StringValueFeedback == null) return;
            Debug.Console(2, this, "DynFusionSerialAttribute Feedback for {0} is valid", attribute.Name);
            dynFusionSerialAttribute.StringValueFeedback.OutputChange += (s, a) =>
            {
                Debug.Console(2, this, "Feedback Fired for {0}", attribute.Name);
                if (a == null) return;
                _asset.FusionGenericAssetSerialsAsset3.StringInput[attribute.JoinNumber].StringValue = a.StringValue;
            };
            _fusionSymbol.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                _asset.FusionGenericAssetSerialsAsset3.StringInput[attribute.JoinNumber].StringValue =
                    dynFusionSerialAttribute.StringValue;
                if (sigNumber == 0) return;
                trilist.StringInput[joinData.JoinNumber].StringValue =
                    _asset.FusionGenericAssetSerialsAsset3.StringOutput[sigNumber].StringValue;
            };
            trilist.OnlineStatusChange += (s, a) =>
            {
                if (!a.DeviceOnLine) return;
                if (sigNumber == 0) return;
                trilist.StringInput[joinData.JoinNumber].StringValue =
                    _asset.FusionGenericAssetSerialsAsset3.StringOutput[sigNumber].StringValue;
            };

        }

        private void LinkBaseAttribute(JoinDataComplete joinData, BasicTriList trilist, DynFusionAttributeBase attribute)
        {
            switch (attribute.Name.ToLower().Replace(" ", ""))
            {
                case "poweron":
                    trilist.SetBoolSigAction(joinData.JoinNumber, b =>
                    {
                        _asset.PowerOn.InputSig.BoolValue = b;
                        var digitalAttribute = attribute as DynFusionDigitalAttribute;
                        if (digitalAttribute == null) return;
                        if (digitalAttribute.BoolValueFeedback == null) return;
                        digitalAttribute.BoolValueFeedback.OutputChange += (o, e) =>
                        {
                            if (e == null) return;
                            _asset.PowerOn.InputSig.BoolValue = e.BoolValue;
                        };
                    });
                    _fusionSymbol.FusionAssetStateChange += (o, a) =>
                    {
                        if (a.UserConfigurableAssetDetailIndex != AssetNumber) return;
                        if (a.EventId != FusionAssetEventId.StaticAssetPowerOnReceivedEventId) return;
                        if (attribute.AttributeAction != null && _asset.PowerOn.OutputSig.BoolValue) 
                            attribute.AttributeAction.Invoke();
                        trilist.BooleanInput[joinData.JoinNumber].BoolValue = _asset.PowerOn.OutputSig.BoolValue;
                    };
                    break;
                case "poweroff":
                    _fusionSymbol.FusionAssetStateChange += (o, a) =>
                    {
                        if (a.UserConfigurableAssetDetailIndex != AssetNumber) return;
                        if (a.EventId != FusionAssetEventId.StaticAssetPowerOffReceivedEventId) return;
                        if (attribute.AttributeAction != null && _asset.PowerOff.OutputSig.BoolValue)
                            attribute.AttributeAction.Invoke();
                        trilist.BooleanInput[joinData.JoinNumber].BoolValue = _asset.PowerOff.OutputSig.BoolValue;
                    };
                    break;
                case "connected":
                    trilist.SetBoolSigAction(joinData.JoinNumber, b =>
                    {
                        _asset.Connected.InputSig.BoolValue = b;
                        var digitalAttribute = attribute as DynFusionDigitalAttribute;
                        if (digitalAttribute == null) return;
                        if (digitalAttribute.BoolValueFeedback == null) return;
                        digitalAttribute.BoolValueFeedback.OutputChange += (o, e) =>
                        {
                            if (e == null) return;
                            _asset.Connected.InputSig.BoolValue = e.BoolValue;
                        };
                    });
                    break;
                case "asseterror":
                    trilist.SetStringSigAction(joinData.JoinNumber, s =>
                    {
                        _asset.AssetError.InputSig.StringValue = s;
                        var serialAttribute = attribute as DynFusionSerialAttribute;
                        if (serialAttribute == null) return;
                        if (serialAttribute.StringValueFeedback == null) return;
                        serialAttribute.StringValueFeedback.OutputChange += (o, e) =>
                        {
                            if (e == null) return;
                            _asset.AssetError.InputSig.StringValue = e.StringValue;
                        };
                    });
                    break;
                case "assetusage":
                    trilist.SetStringSigAction(joinData.JoinNumber, s =>
                    {
                        _asset.AssetUsage.InputSig.StringValue = s;
                        var serialAttribute = attribute as DynFusionSerialAttribute;
                        if (serialAttribute == null) return;
                        if (serialAttribute.StringValueFeedback == null) return;
                        serialAttribute.StringValueFeedback.OutputChange += (o, e) =>
                        {
                            if (e == null) return;
                            _asset.AssetUsage.InputSig.StringValue = e.StringValue;
                        };
                    });
                    break;
            }
        }
    }
}