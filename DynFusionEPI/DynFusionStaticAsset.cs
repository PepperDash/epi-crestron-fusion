using System;
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
		private readonly Dictionary<UInt32, DynFusionDigitalAttribute> _digitalAttributesToFusion;
		private readonly Dictionary<UInt32, DynFusionAnalogAttribute> _analogAttributesToFusion;
		private readonly Dictionary<UInt32, DynFusionSerialAttribute> _serialAttributesToFusion;
		private readonly Dictionary<UInt32, DynFusionDigitalAttribute> _digitalAttributesFromFusion;
		private readonly Dictionary<UInt32, DynFusionAnalogAttribute> _analogAttributesFromFusion;
		private readonly Dictionary<UInt32, DynFusionSerialAttribute> _serialAttributesFromFusion;

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

			_digitalAttributesToFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
			_analogAttributesToFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
			_serialAttributesToFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();
			_digitalAttributesFromFusion = new Dictionary<UInt32, DynFusionDigitalAttribute>();
			_analogAttributesFromFusion = new Dictionary<UInt32, DynFusionAnalogAttribute>();
			_serialAttributesFromFusion = new Dictionary<UInt32, DynFusionSerialAttribute>();

			_joinMap = new DynFusionStaticAssetJoinMap(config.AttributeJoinOffset + 1);

			Debug.Console(DebugExtensions.Warn, this, "Adding StaticAsset");

			SetupAsset(config);

			_fusionSymbol = symbol;
			_fusionSymbol.AddAsset(eAssetType.StaticAsset, AssetNumber, AssetName, AssetType, Guid.NewGuid().ToString());
			_fusionSymbol.FusionAssetStateChange += _fusionSymbol_FusionAssetStateChange;

			_asset = _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset as FusionStaticAsset;
		}

		public void SetupAsset(FusionStaticAssetConfig config)
		{
			Debug.Console(DebugExtensions.Warn, this, "StaticAsset is {0}", _asset == null ? "null, setup failed" : "checking config for setup");
			if (_asset == null) return;

			Debug.Console(DebugExtensions.Warn, this, "StaticAsset config is {0}", config == null ? "null, setup failed" : "running setup");
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

				Debug.Console(DebugExtensions.Warn, this, "Preparing CreateCustomAttributeJoin... digitalAttributes.Count:{0}",
					config.CustomAttributes.DigitalAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.DigitalAttributes, FusionJoinOffset, eSigType.Bool);

				Debug.Console(DebugExtensions.Warn, this, "Preparing CreateCustomAttributeJoin... analogAttributes.Count:{0}",
					config.CustomAttributes.AnalogAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.AnalogAttributes, FusionJoinOffset, eSigType.UShort);

				Debug.Console(DebugExtensions.Warn, this, "Preparing CreateCustomAttributeJoin... serialAttributes.Count:{0}",
					config.CustomAttributes.SerialAttributes.Count());
				CreateCustomAttributeJoin(config.CustomAttributes.SerialAttributes, FusionJoinOffset, eSigType.String);
			}
			catch (Exception ex)
			{
				Debug.Console(DebugExtensions.Trace, this, "DynFusionStaticAsset Exception Message: {0}", ex.Message);
				Debug.Console(DebugExtensions.Warn, this, "DynFusionStaticAsset Exception StackTrace: {0}", ex.StackTrace);
				if (ex.InnerException != null)
					Debug.Console(DebugExtensions.Warn, this, "DynFusionStaticAsset Exception InnerException: {0}", ex.InnerException);
			}
		}

		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			Debug.Console(DebugExtensions.Warn, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(DebugExtensions.Warn, this, "Linking to Bridge AssetType {0}", GetType().Name);
			var joinMap = new DynFusionStaticAssetJoinMap(joinStart + AttributeOffset);

			LinkDigitalAttributesToApi(trilist, joinMap);
			LinkAnalogAttributesToApi(trilist, joinMap);
			LinkSerialAttributesToApi(trilist, joinMap);
		}

		private void LinkDigitalAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			Debug.Console(DebugExtensions.Warn, this, "Linking DigitalAttributes to Trilist '{0}'", trilist.ID.ToString("X"));

			foreach (var attribute in _digitalAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset) 
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking SetBoolSigAction-{0}:{1}", attLocal.Name, bridgeJoin);
				trilist.SetBoolSigAction(bridgeJoin, (b) => { attLocal.BoolValue = b; });
			}

			foreach (var attribute in _digitalAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking BoolValueFeedback-{0}:{1}", attLocal.Name, bridgeJoin);
				attLocal.BoolValueFeedback.LinkInputSig(trilist.BooleanInput[bridgeJoin]);
			}
		}

		private void LinkAnalogAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			Debug.Console(DebugExtensions.Warn, this, "Linking AnalogAttributes to Trilist '{0}'", trilist.ID.ToString("X"));

			foreach (var attribute in _analogAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking SetUshortSigAction-{0}:{1}", attLocal.Name, bridgeJoin);
				trilist.SetUShortSigAction(bridgeJoin, (a) => { attLocal.UShortValue = a; });
			}

			foreach (var attribute in _analogAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking UShortValueFeedback-{0}:{1}", attLocal.Name, bridgeJoin);
				attLocal.UShortValueFeedback.LinkInputSig(trilist.UShortInput[bridgeJoin]);
			}
		}

		private void LinkSerialAttributesToApi(BasicTriList trilist, DynFusionStaticAssetJoinMap joinMap)
		{
			Debug.Console(DebugExtensions.Warn, this, "Linking SerialAttributes to Trilist '{0}'", trilist.ID.ToString("X"));

			foreach (var attribute in _serialAttributesToFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking SetStringSigAction-{0}:{1}", attLocal.Name, bridgeJoin);
				trilist.SetStringSigAction(bridgeJoin, (s) => { attLocal.StringValue = s; });
			}

			foreach (var attribute in _serialAttributesFromFusion)
			{
				var attLocal = attribute.Value;
				var bridgeJoin = (attLocal.JoinNumber < FusionJoinOffset)
					? attLocal.JoinNumber + AttributeOffset
					: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;

				Debug.Console(DebugExtensions.Warn, this, "Linking StringValueFeedback-{0}:{1}", attLocal.Name, bridgeJoin);
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
						: (attLocal.JoinNumber + CustomAttributeOffset) - FusionJoinOffset;
					var trilistLocal = sender as BasicTriList;

					if (trilistLocal == null)
					{
						Debug.Console(DebugExtensions.Warn, this, "LinkSerialAttributesToApi trilistLocal is null");
						return;
					}

					trilistLocal.StringInput[bridgeJoin].StringValue = attLocal.StringValue;
				}
			};
		}

		private void _fusionSymbol_FusionAssetStateChange(FusionBase device, FusionAssetStateEventArgs args)
		{
			Debug.Console(DebugExtensions.Warn, this, "FusionAssetStateChange Device:{0} EventId:{1} Index:{2}",
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
						var joinNumber = (sigDetails.Number + FusionJoinOffset);
						DynFusionDigitalAttribute output;
						Debug.Console(DebugExtensions.Verbose, this, "StaticAsset Digital Join:{0} Name:{1} Value:{2}",
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
						var joinNumber = (sigDetails.Number + FusionJoinOffset);
						DynFusionAnalogAttribute output;
						Debug.Console(DebugExtensions.Verbose, this, "DynFusion StaticAsset Analog Join:{0} Name:{1} Value:{2}",
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
						var joinNumber = (sigDetails.Number + FusionJoinOffset);
						DynFusionSerialAttribute output;
						Debug.Console(DebugExtensions.Verbose, this, "DynFusion StaticAsset Serial Join:{0} Name:{1} Value:{2}",
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
			Debug.Console(DebugExtensions.Verbose, this, "CreateStandardAttributeJoin: {0} ({1}, {2}, {3})",
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
		}

		private void CreateStandardAttributeJoin(JoinDataComplete join, UShortSigDataFixedName sig)
		{
			Debug.Console(DebugExtensions.Verbose, this, "CreateStandardAttributeJoin: {0} ({1}, {2}, {3})",
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
		}

		private void CreateStandardAttributeJoin(JoinDataComplete join, StringSigDataFixedName sig)
		{
			Debug.Console(DebugExtensions.Verbose, this, "CreateStandardAttributeJoin: {0} ({1}, {2}, {3})",
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
		}

		private void CreateCustomAttributeJoin(IEnumerable<DynFusionAttributeBase> attributes, uint offset, eSigType sigType)
		{
			foreach (var attribute in attributes)
			{
				var attributeJoinNumber = attribute.JoinNumber + offset;
				var rwType = attribute.RwType;
				var ioMask = DynFusionDevice.GetIOMask(rwType);

				Debug.Console(DebugExtensions.Verbose, this, "CreateCustomAttributeJoin: {0} ({1}, {2}, {3})",
					attribute.Name, attributeJoinNumber, ioMask.ToString(), rwType.ToString());

				_fusionSymbol.AddSig(AssetNumber, sigType, attributeJoinNumber, AssetName, ioMask);

				switch (sigType)
				{
					case (eSigType.Bool):
						{
							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Read)
							{
								_digitalAttributesToFusion.Add(attributeJoinNumber, new DynFusionDigitalAttribute(
									attribute.Name, attributeJoinNumber, attribute.LinkDeviceKey, attribute.LinkDeviceMethod, attribute.LinkDeviceFeedback));

								_digitalAttributesToFusion[attributeJoinNumber].BoolValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetDigitalsAsset1.UserDefinedBooleanSigDetails[attributeJoinNumber].InputSig);
							}

							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Write)
							{
								_digitalAttributesFromFusion.Add(attributeJoinNumber, new DynFusionDigitalAttribute(attribute.Name, attributeJoinNumber));
							}

							break;
						}
					case (eSigType.UShort):
						{
							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Read)
							{

								_analogAttributesToFusion.Add(attributeJoinNumber, new DynFusionAnalogAttribute(attribute.Name, attributeJoinNumber));

								_analogAttributesToFusion[attributeJoinNumber].UShortValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetAnalogsAsset2.UserDefinedUShortSigDetails[attributeJoinNumber].InputSig);
							}

							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Write)
							{
								_analogAttributesFromFusion.Add(attributeJoinNumber, new DynFusionAnalogAttribute(attribute.Name, attributeJoinNumber));
							}

							break;
						}
					case (eSigType.String):
						{
							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Read)
							{

								_serialAttributesToFusion.Add(attributeJoinNumber, new DynFusionSerialAttribute(attribute.Name, attributeJoinNumber));

								_serialAttributesToFusion[attributeJoinNumber].StringValueFeedback.LinkInputSig(
									_asset.FusionGenericAssetSerialsAsset3.UserDefinedStringSigDetails[attributeJoinNumber].InputSig);
							}

							if (attribute.RwType == eReadWrite.ReadWrite || attribute.RwType == eReadWrite.Write)
							{
								_serialAttributesFromFusion.Add(attributeJoinNumber, new DynFusionSerialAttribute(attribute.Name, attributeJoinNumber));
							}

							break;
						}
				}
			}
		}
	}
}