![state badge](https://badgen.net/badge/state/BETA/orange?icon=github&scale=2)

# epi-dynfusion
DynFusion provides the ability to dynamically create and interact with a Fusion symbol using PepperDash Essentials. 

# Essentials Simple Device Configuration
```           
{
    "key": "DynFusion01",
    "uid": 1,
    "name": "DynFusion",
    "type": "DynFusion",
    "group": "Fusion",
    "properties": 
    {
        "control": 
        {
            "ipid": "AA",
            "method": "ipidTcp",
            "tcpSshProperties": 
            {
                "address": "127.0.0.2",
                "port": 0
            }
        },
        "CustomAttributes":
        {
            "DigitalAttributes" :
            [
                {
                    "name": "PowerOn", 
                    "RwType": "RW", 
                    "JoinNumber": 51
                }
            ],
            "AnalogAttributes" :
            [
                {
                    "name": "testAttributeUShort", 
                    "joinNumber": 51, 
                    "RwType": "RW"
                }
            ],
            "SerialAttributes" :
            [
                {
                    "name": "testAttributeString", 
                    "joinNumber": 51, 
                    "RwType": "RW"
                }
            ]

        },
        "CustomProperties":
        {
            "DigitalProperties" :
            [
                {
                    "ID": "AdHocEnable", 
                    "joinNumber": 55
                }
            ],
            "AnalogProperties" :
            [
                {
                    "ID": "ForceOrgCheckInDuringRes", 
                    "joinNumber": 55
                }
            ],
            "SerialProperties" :
            [
                {
                    "ID": "BackgroundReserved", 
                    "joinNumber": 55
                }
            ]
        },
    },
```

# Essentials Device Bridge Configuration
```
{
    "key": "DynFusionBridge",
    "group": "api",
    "name": "eisc-Bridge",
    "properties": 
    {
        "control": 
        {
            "ipid": "AB",
            "method": "ipidTcp",
            "tcpSshProperties": 
            {
                "address": "127.0.0.2",
                "port": 0
            }
        },
        "devices": 
        [
            {
                "deviceKey": "DynFusion01",
                "joinStart": 1
            },
        ]
    },
    "type": "eiscApiAdvanced",
    "uid": 4
    },
}
``` 
### Digitals

| Input               | I/O     | Output                |
|---------------------|---------|-----------------------|
|                     | 1       | SymbolOnlineFB        |
|                     | 2       |                       |
| SystemPowerOn       | 3       | SystemPowerIsOn       |
| SystemPowerOff      | 4       | Input 1 Fb [HDMI 1]   |
| DisplayPowerOn      | 5       | DisplayPowerIsOn      |
| DispalyPowerOff     | 6       |                       |
| RESERVED            | 7-21    | RESERVED              |
| MsgBraodcastEnabled | 22      |                       |
| RESERVED            | 23-29   | RESERVED              |
|                     | 30      | AuthenticateSucceeded |
|                     | 31      | AuthenticateFailed    |
| RESERVED            | 32 - 49 | RESERVED              |

### Analogs 
| Input        | I/O | Output           |
|--------------|-----|------------------|
| DispalyUsage | 2   |                  |
|              | 22  | BraodcastMsgType |

### Serials


<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ILogStringsWithLevel
- ILogStrings
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsDevice
- JoinMapBaseAdvanced
- DynFusionAttributeBase
- EssentialsBridgeableDevice
- EventArgs
<!-- END Base Classes -->
<!-- START Supported Types -->
### Supported Types

- DynFusionSchedule
- DynFusion
<!-- END Supported Types -->
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.5.5
- 1.5.5
<!-- END Minimum Essentials Framework Versions -->
<!-- START Public Methods -->
### Public Methods

- public void StartDevice()
- public void StopDevice()
- public void sendFreeBusyStatusAvailableUntil(DateTime AvailableUntilTime)
- public void sendFreeBusyStatusAvailable()
- public void sendFreeBusyStatusNotAvailable()
- public void GetRoomList()
- public void getAvailableRooms()
- public void CallAction(bool value)
- public void CallAction(uint value)
- public void CallAction(string value)
- public void sendChange(string message)
- public void GetRoomConfig()
- public void SendToLog(IKeyed device, Debug.ErrorLogLevel level, string logMessage)
- public void SendToLog(IKeyed device, string logMessage)
- public void StartSchedPushTimer()
- public void ResetSchedPushTimer()
- public void StopSchedPushTimer()
- public void GetRoomSchedule()
- public void getRoomScheduleTimeOut(object unused)
- public void CreateDevice(uint deviceNumber, string type, string name)
- public void CreateDisplay(uint deviceNumber, string name)
- public void CreateSource(uint sourceNumber, string name, string type)
- public void StartStopDevice(ushort device, bool action)
- public void changeSource(ushort disp, ushort source)
- public void StartDevice(string key)
- public void StopDevice(string key)
- public void NameDevice(ushort deviceNumber, string name)
- public void SetupAsset(FusionStaticAssetConfig config)
<!-- END Public Methods -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 3 | R | GetSchedule |
| 1 | R | EndCurrentMeeting |
| 2 | R | CheckMeetings |
| 3 | R | ScheduleBusy |
| 4 | R | GetRoomInfo |
| 5 | R | GetRoomList |
| 2 | R | PushNotificationRegistered |
| 1 | R | GetSchedule |
| 11 | R | ExtendMeeting15Minutes |
| 12 | R | ExtendMeeting30Minutes |
| 13 | R | ExtendMeeting45Minutes |
| 14 | R | ExtendMeeting60Minutes |
| 15 | R | ExtendMeeting90Minutes |
| 21 | R | ReserveMeeting15Minutes |
| 22 | R | ReserveMeeting30Minutes |
| 23 | R | ReserveMeeting45Minutes |
| 24 | R | ReserveMeeting60Minutes |
| 25 | R | ReserveMeeting90Minutes |
| 35 | R | NextMeetingIsToday |
| 1 | R | Fusion static asset power on |
| 2 | R | Fusion static asset power off |
| 3 | R | Fusion static asset connected |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 2 | R | RoomID |
| 3 | R | RoomLocation |
| 21 | R | CurrentMeetingOrganizer |
| 22 | R | CurrentMeetingSubject |
| 23 | R | CurrentMeetingMeetingID |
| 24 | R | CurrentMeetingStartTime |
| 25 | R | CurrentMeetingStartDate |
| 26 | R | CurrentMeetingEndTime |
| 27 | R | CurrentMeetingEndDate |
| 28 | R | CurrentMeetingDuration |
| 29 | R | CurrentMeetingRemainingTime |
| 31 | R | NextMeetingOrganizer |
| 32 | R | NextMeetingSubject |
| 33 | R | NextMeetingMeetingID |
| 34 | R | NextMeetingStartTime |
| 35 | R | NextMeetingStartDate |
| 36 | R | NextMeetingEndTime |
| 37 | R | NextMeetingEndDate |
| 38 | R | NextMeetingDuration |
| 39 | R | NextMeetingRemainingTime |
| 41 | R | ThirdMeetingOrganizer |
| 42 | R | ThirdMeetingSubject |
| 43 | R | ThirdMeetingMeetingID |
| 44 | R | ThirdMeetingStartTime |
| 45 | R | ThirdMeetingStartDate |
| 46 | R | ThirdMeetingEndTime |
| 47 | R | ThirdMeetingEndDate |
| 48 | R | ThirdMeetingDuration |
| 49 | R | ThirdMeetingRemainingTime |
| 51 | R | FourthMeetingOrganizer |
| 52 | R | FourthMeetingSubject |
| 53 | R | FourthMeetingMeetingID |
| 54 | R | FourthMeetingStartTime |
| 55 | R | FourthMeetingStartDate |
| 56 | R | FourthMeetingEndTime |
| 57 | R | FourthMeetingEndDate |
| 58 | R | FourthMeetingDuration |
| 59 | R | FourthMeetingRemainingTime |
| 61 | R | FifthMeetingOrganizer |
| 62 | R | FifthMeetingSubject |
| 63 | R | FifthMeetingMeetingID |
| 64 | R | FifthMeetingStartTime |
| 65 | R | FifthMeetingStartDate |
| 66 | R | FifthMeetingEndTime |
| 67 | R | FifthMeetingEndDate |
| 68 | R | FifthMeetingDuration |
| 69 | R | FifthMeetingRemainingTime |
| 71 | R | SixthMeetingOrganizer |
| 72 | R | SixthMeetingSubject |
| 73 | R | SixthMeetingMeetingID |
| 74 | R | SixthMeetingStartTime |
| 75 | R | SixthMeetingStartDate |
| 76 | R | SixthMeetingEndTime |
| 77 | R | SixthMeetingEndDate |
| 78 | R | SixthMeetingDuration |
| 79 | R | SixthMeetingRemainingTime |
| 1 | R | Fusion static asset usage |
| 2 | R | Fusion static asset error |
<!-- END Join Maps -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "DynFusionSchedule",
    "group": "Group",
    "properties": {
        "name": "SampleString",
        "type": "SampleString",
        "attributeJoinOffset": "SampleValue",
        "customAttributeJoinOffset": "SampleValue",
        "Make": "SampleString",
        "Model": "SampleString",
        "attributes": {
            "digitalAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ],
            "analogAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ],
            "serialAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ]
        },
        "customAttributes": {
            "digitalAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ],
            "analogAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ],
            "serialAttributes": [
                {
                    "SignalType": "SampleValue",
                    "JoinNumber": "SampleValue",
                    "Name": "SampleString",
                    "RwType": "SampleValue",
                    "LinkDeviceKey": "SampleString",
                    "LinkDeviceMethod": "SampleString",
                    "LinkDeviceFeedback": "SampleString"
                }
            ]
        }
    }
}
```
<!-- END Config Example -->
