using Crestron.SimplSharpPro.Fusion;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace DynFusion
{
    public class DynFusionAssetOccupancySensor : EssentialsBridgeableDevice
    {
        private readonly FusionRoom _fusionSymbol;
        public uint AssetNumber { get; private set; }

        public DynFusionAssetOccupancySensor(string key, string linkKey, FusionRoom symbol, uint assetNumber)
            : base(string.Format("{0}-OccAsset#{1}", symbol.Name, assetNumber))
        {
            _fusionSymbol = symbol;
            AssetNumber = assetNumber;
        }

        public void sendChange(string message)
        {
            this.LogVerbose("OccupancySensor {assetNumber} recieved Message {message}", AssetNumber, message);

            if (message.StartsWith("<")) //For XML string from Fusion SSI module
                ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                    .RoomOccupancyInfo.InputSig.StringValue = message;

            else if (message.StartsWith("{")) //For JSON string from custom module (legacy)
            {
                var messageObject = JsonConvert.DeserializeObject<DynFusionAssetsOccupancySensorMessage>(message);
                if (message.Contains("OccSensorEnabled"))
                {
                    ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                        .EnableOccupancySensor.InputSig.BoolValue = messageObject.OccSensorEnabled;
                }
                if (message.Contains("RoomOccupied"))
                {
                    ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset).RoomOccupied
                        .InputSig.BoolValue = messageObject.RoomOccupied;
                }
                else
                {
                    ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset).RoomOccupied
                        .InputSig.BoolValue = false;
                }
                if (message.Contains("OccSensorTimeout"))
                {
                    ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                        .OccupancySensorTimeout.InputSig.UShortValue = messageObject.OccSensorTimeout;
                }
            }
        }


        public override void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart,
            string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new DynFusionAssetOccupancySensorJoinMap(joinStart);

            _fusionSymbol.FusionAssetStateChange += (s, a) =>
            {
                this.LogDebug("OccupancySensor State Change {sender} recieved EventID {eventId}", s, a.EventId);

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
                            trilist.StringInput[joinMap.StringIO.JoinNumber].StringValue = string.Format("SetTimeout: {0}",
                                ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                                    .OccupancySensorTimeout.OutputSig.UShortValue);
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