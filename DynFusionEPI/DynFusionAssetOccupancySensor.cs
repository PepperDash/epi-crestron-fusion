using Crestron.SimplSharpPro.Fusion;
using Newtonsoft.Json;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace DynFusion
{
    public class DynFusionAssetOccupancySensor : EssentialsBridgeableDevice
    {
        private readonly FusionRoom _fusionSymbol;
        public uint AssetNumber { get; private set; }
        public readonly string LinkKey;
        private GlsOccupancySensorBaseController Sensor { get; set; }
        private GlsOdtOccupancySensorController UsSensor { get; set; }
        private bool HasUltrasonic { get; set; }
        public BoolFeedback SensorEnable { get; set; }

        public DynFusionAssetOccupancySensor(string key, string linkKey, FusionRoom symbol, uint assetNumber, IKeyed parent)
            : base(string.Format("{0}-OccAsset-{1}", parent.Key, key))
        {
            Key = key;
            LinkKey = linkKey;
            _fusionSymbol = symbol;
            AssetNumber = assetNumber;
            AddPostActivationAction(GetSensor);
        }

        private void GetSensor()
        {
            if (string.IsNullOrEmpty(LinkKey)) return;
            var sensor = DeviceManager.GetDeviceForKey(LinkKey) as GlsOccupancySensorBaseController;
            if (sensor == null) return;
            Sensor = sensor;
            UsSensor = Sensor as GlsOdtOccupancySensorController;
            if (UsSensor != null)
            {
                HasUltrasonic = true;
                SensorEnable =
                    new BoolFeedback(
                        () =>
                            UsSensor.PirSensorEnabledFeedback.BoolValue     || 
                            UsSensor.UltrasonicAEnabledFeedback.BoolValue   ||
                            UsSensor.UltrasonicBEnabledFeedback.BoolValue);
            }
            else
            {
                SensorEnable = new BoolFeedback(() => Sensor.PirSensorEnabledFeedback.BoolValue);
            }
            HasUltrasonic = (Sensor is GlsOdtOccupancySensorController);

            LinkSensor();
        }

        private void DisableSensor()
        {
            if (HasUltrasonic)
            {
                UsSensor.SetUsAEnable(false);
                UsSensor.SetUsBEnable(false);
            }
            Sensor.SetPirEnable(false);
        }

        private void EnableSensor()
        {
            if (HasUltrasonic)
            {
                UsSensor.SetUsAEnable(true);
                UsSensor.SetUsBEnable(true);
            }
            Sensor.SetPirEnable(true);
        }

        private void LinkSensor()
        {
            if (Sensor == null) return;
            Sensor.CurrentTimeoutFeedback.OutputChange += (s, a) =>
            {
                ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                .OccupancySensorTimeout.InputSig.UShortValue = Sensor.CurrentTimeoutFeedback.UShortValue;
            };
            SensorEnable.OutputChange += (s, a) =>
            {
                ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                        .EnableOccupancySensor.InputSig.BoolValue = SensorEnable.BoolValue;
            };
            Sensor.RoomIsOccupiedFeedback.OutputChange += (s, a) =>
            {
                ((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset).RoomOccupied
                .InputSig.BoolValue = Sensor.RoomIsOccupiedFeedback.BoolValue;
            };

            _fusionSymbol.FusionAssetStateChange += (s, a) =>
            {
                Debug.Console(2, this, "OccupancySensor State Change {0} recieved EventID {1}", s, a.EventId);
                // Debug.Console(2, this, "OccupancySensor State Change {0} recieved EventID {1}", device, args.EventId);
                switch (a.EventId)
                {
                    case FusionAssetEventId.DisableOccupancySensorReceivedEventId:
                    {
                        DisableSensor();
                            break;
                        }
                    case FusionAssetEventId.EnableOccupancySensorReceivedEventId:
                        {
                            EnableSensor();
                            break;
                        }
                    case FusionAssetEventId.OccupancySensorTimeoutReceivedEventId:
                        {
                            Sensor.SetRemoteTimeout(((FusionOccupancySensor)_fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                                    .OccupancySensorTimeout.OutputSig.UShortValue);
                            break;
                        }
                }
            };
        }

        public void SendChange(string message)
        {
            Debug.Console(2, this, "OccupancySensor {0} recieved Message {1}", AssetNumber, message);

            if (message.StartsWith("<")) //For XML string from Fusion SSI module
                ((FusionOccupancySensor) _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                    .RoomOccupancyInfo.InputSig.StringValue = message;

            else if (message.StartsWith("{")) //For JSON string from custom module (legacy)
            {
                var messageObject = JsonConvert.DeserializeObject<DynFusionAssetsOccupancySensorMessage>(message);
                if (message.Contains("OccSensorEnabled"))
                {
                    ((FusionOccupancySensor) _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                        .EnableOccupancySensor.InputSig.BoolValue = messageObject.OccSensorEnabled;
                }
                if (message.Contains("OccSensorTimeout"))
                {
                    ((FusionOccupancySensor) _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                        .OccupancySensorTimeout.InputSig.UShortValue = messageObject.OccSensorTimeout;
                }
                ((FusionOccupancySensor) _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset).RoomOccupied
                    .InputSig.BoolValue = message.Contains("RoomOccupied") && messageObject.RoomOccupied;

            }
        }

        public override void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart,
            string joinMapKey, EiscApiAdvanced bridge)
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
                        trilist.StringInput[joinMap.StringIo.JoinNumber].StringValue = "Disable\r";
                        break;
                    }
                    case FusionAssetEventId.EnableOccupancySensorReceivedEventId:
                    {
                        trilist.StringInput[joinMap.StringIo.JoinNumber].StringValue = "Enable\r";
                        break;
                    }
                    case FusionAssetEventId.OccupancySensorTimeoutReceivedEventId:
                    {
                        trilist.StringInput[joinMap.StringIo.JoinNumber].StringValue = string.Format("SetTimeout: {0}",
                            ((FusionOccupancySensor) _fusionSymbol.UserConfigurableAssetDetails[AssetNumber].Asset)
                                .OccupancySensorTimeout.OutputSig.UShortValue);
                        break;
                    }
                }
            };

            // TODO: this might be better to send with an online from Fusion 
            trilist.StringInput[joinMap.StringIo.JoinNumber].StringValue = "SendValues\r";
            trilist.SetStringSigAction(joinMap.StringIo.JoinNumber, SendChange);
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