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
| DisplayPowerOff     | 6       |                       |
| RESERVED            | 7-21    | RESERVED              |
| MsgBraodcastEnabled | 22      |                       |
| RESERVED            | 23-29   | RESERVED              |
|                     | 30      | AuthenticateSucceeded |
|                     | 31      | AuthenticateFailed    |
| RESERVED            | 32 - 49 | RESERVED              |

### Analogs 
| Input        | I/O | Output           |
|--------------|-----|------------------|
| DisplayUsage | 2   |                  |
|              | 22  | BraodcastMsgType |

### Serials


