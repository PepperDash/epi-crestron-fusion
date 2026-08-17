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


<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.20.5
- 2.20.5
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "DynFusion",
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
<!-- START Supported Types -->
### Supported Types

- DynFusion
- DynFusionSchedule
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Fusion Online |
| 3 | R | SystemPowerOn |
| 4 | R | SystemPowerOff |
| 5 | R | DisplayPowerOn |
| 6 | R | DisplayPowerOoff |
| 21 | R | MsgBroadcastEnabled |
| 30 | R | AuthenticationSucceeded |
| 31 | R | AuthenticationFailed |
| 1 | R | Fusion static asset power on |
| 2 | R | Fusion static asset power off |
| 3 | R | Fusion static asset connected |
| 3 | R | GetSchedule |
| 1 | R | EndCurrentMeeting |
| 2 | R | CheckMeetings |
| 3 | R | ScheduleBusy |
| 4 | R | GetRoomInfo |
| 5 | R | GetRoomList |
| 2 | R | PushNotificationRegistered |
| 1 | R | MeetingInProgress |
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

#### Analogs

| Join | Type (RW) | Description |
| --- | --- | --- |
| 2 | R | DisplayUsage |
| 22 | R | BoradcasetMsgType |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Device Name |
| 1 | R | HelpMsg |
| 2 | R | ErrorMsg |
| 3 | R | LogText |
| 5 | R | DeviceUsage |
| 6 | R | TextMessage |
| 22 | R | BroadcastMsg |
| 23 | R | FreeBusyStatus |
| 31 | R | GroupMembership |
| 32 | R | SchedulingQuery |
| 33 | R | SchedulingCreate |
| 34 | R | SchedulingRemove |
| 21 | R | TimeClockQuery |
| 35 | R | ActionQuery |
| 14 | R | RoomConfigJoin |
| 1 | R | Fusion static asset usage |
| 2 | R | Fusion static asset error |
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
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ILogStringsWithLevel
- ILogStrings
- IKeyed
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsDevice
- EventArgs
- JoinMapBaseAdvanced
- EssentialsBridgeableDevice
- DynFusionAttributeBase
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void CreateDevice(uint deviceNumber, string type, string name)
- public void CreateDisplay(uint deviceNumber, string name)
- public void CreateSource(uint sourceNumber, string name, string type)
- public void StartStopDevice(ushort device, bool action)
- public void changeSource(ushort disp, ushort source)
- public void StartDevice(string key)
- public void StopDevice(string key)
- public void NameDevice(ushort deviceNumber, string name)
- public void StartSchedPushTimer()
- public void ResetSchedulePushTimer()
- public void StopSchedPushTimer()
- public void GetRoomSchedule()
- public void GetRoomScheduleTimeOut(object unused)
- public void GetRoomConfig()
- public void SendToLog(IKeyed device, Debug.ErrorLogLevel level, string logMessage)
- public void SendToLog(IKeyed device, string logMessage)
- public void SendFreeBusyStatusAvailableUntil(DateTime AvailableUntilTime)
- public void SendFreeBusyStatusAvailable()
- public void SendFreeBusyStatusNotAvailable()
- public void GetRoomList()
- public void GetAvailableRooms()
- public void sendChange(string message)
- public void StartDevice()
- public void StopDevice()
- public void SetupAsset(FusionStaticAssetConfig config)
- public void CallAction(bool value)
- public void CallAction(uint value)
- public void CallAction(string value)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- FusionOnlineFeedback
- BoolValueFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- UShortValueFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- StringValueFeedback
<!-- END String Feedbacks -->
