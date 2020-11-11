using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Fusion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Core;

namespace DynFusion.Assets
{
	public class DynFusionAssetOccupancySensor : EssentialsBridgeableDevice
	{
		private uint _assetNumber;
		private FusionRoom _fusionSymbol;
		private DynFusionAssetsOccupancySensorMessage messageObject;
		public DynFusionAssetOccupancySensor(string Key, string linkKey, FusionRoom symbol, uint assetNumber) : base(string.Format("{0}-OccAsset#{1}", symbol.Name, assetNumber))
		{
			_assetNumber = assetNumber;
			_fusionSymbol = symbol;
			// _fusionSymbol.FusionAssetStateChange += new FusionAssetStateEventHandler(_fusionSymbol_FusionAssetStateChange);

		}

		public void sendChange(string message)
		{
			Debug.Console(2, this, "OccupancySensor {0} recieved Message {1}", _assetNumber, message);

			if (message.StartsWith("<"))    //For XML string from Fusion SSI module
				((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).RoomOccupancyInfo.InputSig.StringValue = message;

			else if (message.StartsWith("{"))     //For JSON string from custom module (legacy)
			{
				messageObject = JsonConvert.DeserializeObject<DynFusionAssetsOccupancySensorMessage>(message);
				if (message.Contains("OccSensorEnabled"))
				{
					((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).EnableOccupancySensor.InputSig.BoolValue = messageObject.OccSensorEnabled;
				}
				if (message.Contains("RoomOccupied"))
				{
					((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).RoomOccupied.InputSig.BoolValue = messageObject.RoomOccupied;
				}
				else
				{
					((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).RoomOccupied.InputSig.BoolValue = false;
				}
				if (message.Contains("OccSensorTimeout"))
				{
					((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).OccupancySensorTimeout.InputSig.UShortValue = messageObject.OccSensorTimeout;
				}
			}
		}

	
		public override void  LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new DynFusionAssetOccupancySensorJoinMap(joinStart);

			_fusionSymbol.FusionAssetStateChange += (s, a) =>
			{
				Debug.Console(2, this, "OccupancySensor State Change {0} recieved EventID {1}", s, a.EventId);
				// Debug.Console(2, this, "OccupancySensor State Change {0} recieved EventID {1}", device, args.EventId);
				switch (a.EventId)
				{
					case FusionAssetEventId.DisableOccupancySensorReceivedEventId:
						{
							trilist.StringInput[joinMap.StringIO.JoinNumber].StringValue = "Disable\r";
							break;
						}
					case FusionAssetEventId.EnableOccupancySensorReceivedEventId:
						{
							trilist.StringInput[joinMap.StringIO.JoinNumber].StringValue = "Enable\r";
							break;
						}
					case FusionAssetEventId.OccupancySensorTimeoutReceivedEventId:
						{
							trilist.StringInput[joinMap.StringIO.JoinNumber].StringValue = string.Format("SetTimeout: {0}", ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[_assetNumber].Asset).OccupancySensorTimeout.OutputSig.UShortValue);
							break;
						}
				}
			};
				
			// TODO: this might be better to send with an online from Fusion 
			trilist.StringInput[joinMap.StringIO.JoinNumber].StringValue = "SendValues\r";
			trilist.SetStringSigAction(joinMap.StringIO.JoinNumber, (s) => sendChange(s));
		}
}
	public class DynFusionAssetsOccupancySensorMessage
	{
		public bool OccSensorEnabled;
		public bool RoomOccupied;
		public ushort OccSensorTimeout;
		public string RoomOccupancyInfo;
	}
}

