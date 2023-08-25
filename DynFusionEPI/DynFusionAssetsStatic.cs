using System;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.Fusion;

namespace DynFusion
{
	public class DynFusionAssetsStatic
	{
		private readonly EISCAPI _api;
		private readonly FusionStaticAsset _asset;
		private readonly string _assetName;
		private readonly uint _assetNumber;
		private readonly uint _customJoinOffset;
		private readonly PDT_Debug _debug;
		private readonly FusionRoom _fusionSymbol;
		private uint _powerOffJoin;
		private uint _powerOnJoin;

		public DynFusionAssetsStatic(uint assetNumber, FusionRoom symbol, PDT_Debug debug, EISCAPI api,
			StaticAsset config)
		{
			_assetNumber = assetNumber;
			_assetName = config.name;
			_fusionSymbol = symbol;
			_debug = debug;
			_api = api;
			_customJoinOffset = config.customAttributeJoinOffset;
			_fusionSymbol.FusionAssetStateChange += _fusionSymbol_FusionAssetStateChange;

			_powerOffJoin = 0;
			_powerOnJoin = 0;
			_asset = ((FusionStaticAsset)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset);

			_asset.PowerOn.AddSigToRVIFile = false;
			_asset.PowerOff.AddSigToRVIFile = false;
			_asset.Connected.AddSigToRVIFile = false;
			_asset.AssetError.AddSigToRVIFile = false;
			_asset.AssetUsage.AddSigToRVIFile = false;
			_asset.ParamMake.Value = !String.IsNullOrEmpty(config.make) ? config.make : String.Empty;
			_asset.ParamModel.Value = !String.IsNullOrEmpty(config.model) ? config.model : String.Empty;
		}

		private void _fusionSymbol_FusionAssetStateChange(FusionBase device, FusionAssetStateEventArgs args)
		{
			if (_assetNumber != args.UserConfigurableAssetDetailIndex)
			{
				return;
			}

			_debug.TraceEvent("Static Asset State Change {0} recieved EventID {1} Index {2}", device, args.EventId,
				args.UserConfigurableAssetDetailIndex);
			switch (args.EventId)
			{
				case FusionAssetEventId.StaticAssetPowerOffReceivedEventId:
					{
						if (_powerOffJoin > 0)
						{
							_api.EISC.BooleanInput[_powerOffJoin].BoolValue = _asset.PowerOff.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetPowerOnReceivedEventId:
					{
						if (_powerOnJoin > 0)
						{
							_api.EISC.BooleanInput[_powerOnJoin].BoolValue = _asset.PowerOn.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetBoolAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as BooleanSigData;
						if (sigDetails != null)
						{
							_debug.TraceEvent(string.Format("StaticAsset: {0} Bool Change Join:{1} Name:{2} Value:{3}",
								_asset.ParamAssetName, sigDetails.Number, sigDetails.Name, sigDetails.OutputSig.BoolValue));
							_api.EISC.BooleanInput[sigDetails.Number + _customJoinOffset].BoolValue =
								sigDetails.OutputSig.BoolValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetUshortAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as UShortSigData;
						if (sigDetails != null)
						{
							_debug.TraceEvent(string.Format("StaticAsset: {0} UShort Change Join:{1} Name:{2} Value:{3}",
								_asset.ParamAssetName, sigDetails.Number, sigDetails.Name, sigDetails.OutputSig.UShortValue));
							_api.EISC.UShortInput[sigDetails.Number + _customJoinOffset].UShortValue =
								sigDetails.OutputSig.UShortValue;
						}
						break;
					}
				case FusionAssetEventId.StaticAssetAssetStringAssetSigEventReceivedEventId:
					{
						var sigDetails = args.UserConfiguredSigDetail as StringSigData;
						if (sigDetails != null)
						{
							_debug.TraceEvent(string.Format("StaticAsset: {0} String Change Join:{1} Name:{2} Value:{3}",
								_asset.ParamAssetName, sigDetails.Number, sigDetails.Name, sigDetails.OutputSig.StringValue));
							_api.EISC.StringInput[sigDetails.Number + _customJoinOffset].StringValue =
								sigDetails.OutputSig.StringValue;
						}
						break;
					}
			}
		}

		public void AddAttribute(string name, uint eiscJoin)
		{
			_debug.TraceEvent(string.Format("Creating assetAttribute: {0}, {1}, {2}", _assetName, name, eiscJoin));

			switch (name)
			{
				case "PowerOn":
					{
						_api.DigitalActionDict[(ushort)eiscJoin] =
							args => _asset.PowerOn.InputSig.BoolValue = args.Sig.BoolValue;
						_powerOnJoin = eiscJoin;
						_asset.PowerOn.AddSigToRVIFile = true;
						break;
					}
				case "PowerOff":
					{
						_powerOffJoin = eiscJoin;
						_asset.PowerOff.AddSigToRVIFile = true;
						break;
					}
				case "Connected":
					{
						_api.DigitalActionDict[(ushort)eiscJoin] =
							args => _asset.Connected.InputSig.BoolValue = args.Sig.BoolValue;
						_asset.Connected.AddSigToRVIFile = true;
						break;
					}
				case "AssetUsage":
					{
						_api.StringActionDict[(ushort)eiscJoin] =
							args => _asset.AssetUsage.InputSig.StringValue = args.Sig.StringValue;
						_asset.AssetUsage.AddSigToRVIFile = true;
						break;
					}
				case "AssetError":
					{
						_api.StringActionDict[(ushort)eiscJoin] =
							args => _asset.AssetError.InputSig.StringValue = args.Sig.StringValue;
						_asset.AssetError.AddSigToRVIFile = true;
						break;
					}
			}
		}

		public void AddCustomAttribute(eSigType sigType, string name, eSigIoMask rwType, uint eiscJoin,
			ushort joinNumber)
		{
			_debug.TraceEvent(string.Format("Creating assetCustomAttribute: {0}, {1}, {2}, {3}, {4}", _assetName, name,
				sigType, rwType, eiscJoin));

			_fusionSymbol.AddSig(_assetNumber, sigType, joinNumber, name, rwType);
			//Create attribute with join based at 1
			int joinNumberOffset = joinNumber + Constants.FusionJoinOffset;
			//From now on use join offset by 49 (based at 50)

			//Create actions to send to Fusion if write or read/write
			if (rwType != eSigIoMask.InputSigOnly && rwType != eSigIoMask.InputOutputSig)
			{
				return;
			}

			switch (sigType)
			{
				case eSigType.Bool:
					{
						_api.DigitalActionDict[(ushort)eiscJoin] =
							args =>
								_asset.FusionGenericAssetDigitalsAsset1.BooleanInput[(ushort)joinNumberOffset].BoolValue =
									args.Sig.BoolValue;
						break;
					}
				case eSigType.UShort:
					{
						_api.AnalogActionDict[(ushort)eiscJoin] =
							args =>
								_asset.FusionGenericAssetAnalogsAsset2.UShortInput[(ushort)joinNumberOffset].UShortValue =
									args.Sig.UShortValue;
						break;
					}
				case eSigType.String:
					{
						_api.StringActionDict[(ushort)eiscJoin] =
							args =>
								_asset.FusionGenericAssetSerialsAsset3.StringInput[(ushort)joinNumberOffset].StringValue =
									args.Sig.StringValue;
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
	}
}