using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.Fusion;
using DynFusion.Config;
using PepperDash.Core;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace DynFusion
{
	public class DynFusionStaticAsset : EssentialsBridgeableDevice
	{
		private readonly FusionRoom _fusionSymbol;
		private readonly IKeyName _parentDevice;
		public readonly uint AssetNumber;
		public readonly string AssetName;
		public readonly string AssetType;
		public string Make;
		public string Model;

		private readonly FusionStaticAsset _asset;
		public readonly uint AttributeOffset;
		public readonly uint CustomAttributeOffset;
		private const uint FusionJoinOffset = 49;

		private readonly DynFusionStaticAssetJoinMap _joinMap;

		// key is the fusion join number, not the bridge join number
		private readonly Dictionary<uint, DynFusionDigitalAttribute> _digitalAttributesToFusion;
		private readonly Dictionary<uint, DynFusionAnalogAttribute> _analogAttributesToFusion;
		private readonly Dictionary<uint, DynFusionSerialAttribute> _serialAttributesToFusion;
		private readonly Dictionary<uint, DynFusionDigitalAttribute> _digitalAttributesFromFusion;
		private readonly Dictionary<uint, DynFusionAnalogAttribute> _analogAttributesFromFusion;
		private readonly Dictionary<uint, DynFusionSerialAttribute> _serialAttributesFromFusion;

		private uint _powerOffJoin;
		private uint _powerOnJoin;

		public DynFusionStaticAsset(IKeyName parentDevice, FusionRoom symbol, uint assetNumber, FusionStaticAssetConfig config)
			: base(string.Format("{0}-staticAsset-#{1}-{2}", parentDevice.Key, assetNumber, config.Name.Replace(" ", "")))
		{
			_parentDevice = parentDevice;

			AssetNumber = assetNumber;
			AssetName = config.Name;
			AssetType = config.Type;

			Make = string.IsNullOrEmpty(config.Make) ? string.Empty : config.Make;
			Model = string.IsNullOrEmpty(config.Model) ? string.Empty : config.Model;

			AttributeOffset = config.AttributeJoinOffset;
			CustomAttributeOffset = config.CustomAttributeJoinOffset;

			_digitalAttributesToFusion = new Dictionary<uint, DynFusionDigitalAttribute>();
			_analogAttributesToFusion = new Dictionary<uint, DynFusionAnalogAttribute>();
			_serialAttributesToFusion = new Dictionary<uint, DynFusionSerialAttribute>();
			_digitalAttributesFromFusion = new Dictionary<uint, DynFusionDigitalAttribute>();
			_analogAttributesFromFusion = new Dictionary<uint, DynFusionAnalogAttribute>();
			_serialAttributesFromFusion = new Dictionary<uint, DynFusionSerialAttribute>();

			_joinMap = new DynFusionStaticAssetJoinMap(config.AttributeJoinOffset + 1);

			this.LogDebug("Adding StaticAsset");

			_fusionSymbol = symbol;
			_fusionSymbol.AddAsset(eAssetType.StaticAsset, AssetNumber, AssetName, AssetType, Guid.NewGuid().ToString());
			_fusionSymbol.FusionAssetStateChange += _fusionSymbol_FusionAssetStateChange;

			_asset = _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset as FusionStaticAsset;

			SetupAsset(config);
		}

		public void SetupAsset(FusionStaticAssetConfig config)
		{
			this.LogDebug("SetupAsset: _asset is {status}", _asset == null ? "null, setup failed" : "checking config for setup");
			if (_asset == null) return;

			this.LogDebug("SetupAsset: config is {status}", config == null ? "null, setup failed" : "running setup");
			if (config == null) return;

			try
			{
				_asset.ParamMake.Value = Make;
				_asset.ParamModel.Value = Model;

				_asset.PowerOn.AddSigToRVIFile = false;
				_asset.PowerOff.AddSigToRVIFile = false;
				_asset.Connected.AddSigToRVIFile = false;
				_asset.AssetError.AddSigToRVIFile = false;
				_asset.AssetUsage.AddSigToRVIFile = false;

				_powerOffJoin = 0;
				_powerOnJoin = 0;

				CreateStandardAttributeJoin(_joinMap.PowerOn, _asset.PowerOn);
				CreateStandardAttributeJoin(_joinMap.PowerOff, _asset.PowerOff);
				CreateStandardAttributeJoin(_joinMap.Connected, _asset.Connected);
				CreateStandardAttributeJoin(_joinMap.AssetUsage, _asset.AssetUsage);
				CreateStandardAttributeJoin(_joinMap.AssetError, _asset.AssetError);

				this.LogDebug("Preparing CreateCustomAttributeJoin... digitalAttributes.Count:{count}",
					config.CustomAttributes.DigitalAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.DigitalAttributes, FusionJoinOffset, eSigType.Bool);

				this.LogDebug("Preparing CreateCustomAttributeJoin... analogAttributes.Count:{count}",
					config.CustomAttributes.AnalogAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.AnalogAttributes, FusionJoinOffset, eSigType.UShort);

				this.LogDebug("Preparing CreateCustomAttributeJoin... serialAttributes.Count:{count}",
					config.CustomAttributes.SerialAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.SerialAttributes, FusionJoinOffset, eSigType.String);
			}
			catch (Exception ex)
			{
				this.LogError("DynFusionStaticAsset Exception Message: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
			}
		}

		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			this.LogDebug("Linking to Trilist '{trilistId}'", trilist.ID.ToString("X"));
			this.LogDebug("Linking to Bridge AssetType {assetType}", GetType().Name);
			var joinMap = new DynFusionStaticAssetJoinMap(joinStart + AttributeOffset);

			LinkDigitalAttributesToApi(trilist, joinMap);
			LinkAnalogAttributesToApi(trilist, joinMap);
			LinkSerialAttributesToApi(trilist, joinMap);
		}

		private void LinkDigitalAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			this.LogDebug("Linking DigitalAttributes to Trilist '{trilistId}'", trilist.ID.ToString("X"));

			foreach (var attribute in _digitalAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking SetBoolSigAction-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				trilist.SetBoolSigAction(bridgeJoin, (b) => { attLocal.BoolValue = b; });
			}

			foreach (var attribute in _digitalAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking BoolValueFeedback-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[bridgeJoin]);
			}
		}

		private void LinkAnalogAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			this.LogDebug("Linking AnalogAttributes to Trilist '{trilistId}'", trilist.ID.ToString("X"));

			foreach (var attribute in _analogAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking SetUshortSigAction-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				trilist.SetUShortSigAction(bridgeJoin, (a) => { attLocal.UShortValue = a; });
			}

			foreach (var attribute in _analogAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking UShortValueFeedback-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[bridgeJoin]);
			}
		}

		private void LinkSerialAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			this.LogDebug("Linking SerialAttributes to Trilist '{trilistId}'", trilist.ID.ToString("X"));

			foreach (var attribute in _serialAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking SetStringSigAction-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				trilist.SetStringSigAction(bridgeJoin, (s) => { attLocal.StringValue = s; });
			}

			foreach (var attribute in _serialAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

				this.LogDebug("Linking StringValueFeedback-{name}:{bridgeJoin}", attLocal.Name, bridgeJoin);
				attLocal.StringValueFeedback.LinkInputSig(trilist.StringInput[bridgeJoin]);
			}

			trilist.OnlineStatusChange += (sender, args) =>
			{
				if (!args.DeviceOnLine) return;

				foreach (var attribute in _serialAttributesFromFusion)
				{
					var attLocal = attribute.Value;
					var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
						? attLocal.JoinNumber + AttributeOffset
						: attLocal.JoinNumber + CustomAttributeOffset - FusionJoinOffset;

					if (!(sender is BasicTriList trilistLocal))
					{
						this.LogDebug("LinkSerialAttributesToApi trilistLocal is null");
						return;
					}

					trilistLocal.StringInput[bridgeJoin].StringValue = attLocal.StringValue;
				}
			};
		}

		private void _fusionSymbol_FusionAssetStateChange(FusionBase device, FusionAssetStateEventArgs args)
		{
			this.LogDebug("FusionAssetStateChange Device:{device} EventId:{eventId} Index:{index}",
				device, args.EventId, args.UserConfigurableAssetDetailIndex);

			if (AssetNumber != args.UserConfigurableAssetDetailIndex)
			{
				return;
			}

			switch (args.EventId)
			{
				case FusionAssetEventId.StaticAssetPowerOnReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (_digitalAttributesFromFusion.TryGetValue(_joinMap.PowerOn.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetPowerOffReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigDataFixedName;
						DynFusionDigitalAttribute output;
						if (_digitalAttributesFromFusion.TryGetValue(_joinMap.PowerOff.JoinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetBoolAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigData;
						var joinNumber = sigDetails.Number + FusionJoinOffset;
						DynFusionDigitalAttribute output;
						this.LogVerbose("StaticAsset Digital Join:{joinNumber} Name:{name} Value:{value}",
							joinNumber, sigDetails.Name, sigDetails.OutputSig.BoolValue);

						if (_digitalAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.BoolValue = sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetUshortAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
						var joinNumber = sigDetails.Number + FusionJoinOffset;
						DynFusionAnalogAttribute output;
						this.LogVerbose("DynFusion StaticAsset Analog Join:{joinNumber} Name:{name} Value:{value}",
							joinNumber, sigDetails.Name, sigDetails.OutputSig.UShortValue);

						if (_analogAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.UShortValue = sigDetails.OutputSig.UShortValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetStringAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as StringSigData;
						var joinNumber = sigDetails.Number + FusionJoinOffset;
						DynFusionSerialAttribute output;
						this.LogVerbose("DynFusion StaticAsset Serial Join:{joinNumber} Name:{name} Value:{value}",
							joinNumber, sigDetails.Name, sigDetails.OutputSig.StringValue);

						if (_serialAttributesFromFusion.TryGetValue(joinNumber, out output))
						{
							output.StringValue = sigDetails.OutputSig.StringValue;
						}
						break;
					}
			}
		}

		#region Nested type: DynFusionAssetsStaticMessage

		public class DynFusionAssetsStaticMessage
		{
			public bool SystemPowerOff;
			public bool SystemPowerOn;
		}

		#endregion

		private void CreateStandardAttributeJoin(JoinDataComplete join, BooleanSigDataFixedName sig)
		{
			this.LogVerbose("CreateStandardAttributeJoin: {name} ({ioMask}, {joinNumber}, {joinCapabilities})",
				sig.Name, sig.SigIoMask.ToString(), join.JoinNumber, join.Metadata.JoinCapabilities.ToString());

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				_digitalAttributesFromFusion.Add(join.JoinNumber,
					new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				_digitalAttributesToFusion.Add(join.JoinNumber,
					new DynFusionDigitalAttribute(join.Metadata.Description, join.JoinNumber));
				_digitalAttributesToFusion[join.JoinNumber].BoolValueFeedback.LinkInputSig(sig.InputSig);
			}

			switch (sig.Name)
			{
				case "PowerOn":
					{
						_asset.PowerOn.AddSigToRVIFile = true;
						break;
					}
				case "PowerOff":
					{
						_asset.PowerOff.AddSigToRVIFile = true;
						break;
					}
				case "Connected":
					{
						_asset.Connected.AddSigToRVIFile = true;
						break;
					}
			}
		}

		private void CreateStandardAttributeJoin(JoinDataComplete join, UShortSigDataFixedName sig)
		{
			this.LogVerbose("CreateStandardAttributeJoin: {name} ({ioMask}, {joinNumber}, {joinCapabilities})",
				sig.Name, sig.SigIoMask.ToString(), join.JoinNumber, join.Metadata.JoinCapabilities.ToString());

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				_analogAttributesFromFusion.Add(join.JoinNumber,
					new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				_analogAttributesToFusion.Add(join.JoinNumber,
					new DynFusionAnalogAttribute(join.Metadata.Description, join.JoinNumber));
				_analogAttributesToFusion[join.JoinNumber].UShortValueFeedback.LinkInputSig(sig.InputSig);
			}

			switch (sig.Name)
			{
				default:
					{
						break;
					}
			}
		}

		private void CreateStandardAttributeJoin(JoinDataComplete join, StringSigDataFixedName sig)
		{
			this.LogVerbose("CreateStandardAttributeJoin: {name} ({ioMask}, {joinNumber}, {joinCapabilities})",
				sig.Name, sig.SigIoMask.ToString(), join.JoinNumber, join.Metadata.JoinCapabilities.ToString());

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.ToSIMPL)
			{
				_serialAttributesFromFusion.Add(join.JoinNumber,
					new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
			}

			if (join.Metadata.JoinCapabilities == eJoinCapabilities.ToFromSIMPL ||
				join.Metadata.JoinCapabilities == eJoinCapabilities.FromSIMPL)
			{
				_serialAttributesToFusion.Add(join.JoinNumber,
					new DynFusionSerialAttribute(join.Metadata.Description, join.JoinNumber));
				_serialAttributesToFusion[join.JoinNumber].StringValueFeedback.LinkInputSig(sig.InputSig);
			}

			switch (sig.Name)
			{
				case "AssetUsage":
					{
						_asset.AssetUsage.AddSigToRVIFile = true;
						break;
					}
				case "AssetError":
					{
						_asset.AssetError.AddSigToRVIFile = true;
						break;
					}
			}
		}

		private void CreateCustomAttributeJoin(IEnumerable<DynFusionAttributeBase> attributes, uint offset, eSigType sigType)
		{
			foreach (var attribute in attributes)
			{
				var name = attribute.Name;
				var joinNumber = attribute.JoinNumber + offset;
				var linkDeviceKey = attribute.LinkDeviceKey;
				var linkDeviceMethod = attribute.LinkDeviceMethod;
				var linkDeviceFeedback = attribute.LinkDeviceFeedback;
				var rwType = attribute.RwType;
				var ioMask = DynFusionDevice.GetIOMask(rwType);

				this.LogDebug("CreateCustomAttributeJoin: {name} ({ioMask}, {joinNumber}, {joinCapabilities})",
					name, joinNumber, ioMask.ToString(), rwType.ToString());

				_fusionSymbol.AddSig(AssetNumber, sigType, joinNumber, name, ioMask);

				switch (sigType)
				{
					case eSigType.Bool:
						{
							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Read)
							{
								_digitalAttributesToFusion.Add(joinNumber, new DynFusionDigitalAttribute(
									name, joinNumber, linkDeviceKey, linkDeviceMethod, linkDeviceFeedback));

								_digitalAttributesToFusion[joinNumber].BoolValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetDigitalsAsset1.UserDefinedBooleanSigDetails[joinNumber].InputSig);
							}

							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Write)
							{
								_digitalAttributesFromFusion.Add(joinNumber, new DynFusionDigitalAttribute(name, joinNumber));
							}

							break;
						}
					case eSigType.UShort:
						{
							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Read)
							{

								_analogAttributesToFusion.Add(joinNumber, new DynFusionAnalogAttribute(name, joinNumber));

								_analogAttributesToFusion[joinNumber].UShortValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetAnalogsAsset2.UserDefinedUShortSigDetails[joinNumber].InputSig);
							}

							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Write)
							{
								_analogAttributesFromFusion.Add(joinNumber, new DynFusionAnalogAttribute(name, joinNumber));
							}

							break;
						}
					case eSigType.String:
						{
							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Read)
							{

								_serialAttributesToFusion.Add(joinNumber, new DynFusionSerialAttribute(name, joinNumber));

								_serialAttributesToFusion[joinNumber].StringValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetSerialsAsset3.UserDefinedStringSigDetails[joinNumber].InputSig);
							}

							if (rwType == eReadWrite.ReadWrite || rwType == eReadWrite.Write)
							{
								_serialAttributesFromFusion.Add(joinNumber, new DynFusionSerialAttribute(name, joinNumber));
							}

							break;
						}
				}
			}
		}
	}
}