using PepperDash.Essentials.Core;

namespace PDTDynFusionEPI
{
	public class DynFusionJoinMap : JoinMapBaseAdvanced
	{
	    public JoinDataComplete DeviceName = new JoinDataComplete(new JoinData {JoinNumber = 1, JoinSpan = 1}, new JoinMetadata{Label = "Device Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial});

		// Bools 
		public JoinDataComplete Online = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Label = "Fusion Online", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete SystemPowerOn = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Label = "SystemPowerOn", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete SystemPowerOff = new JoinDataComplete(new JoinData { JoinNumber = 4, JoinSpan = 1 }, new JoinMetadata { Label = "SystemPowerOff", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete DisplayPowerOn = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, new JoinMetadata { Label = "DisplayPowerOn", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete DisplayPowerOff = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 }, new JoinMetadata { Label = "DisplayPowerOoff", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete MsgBroadcastEnabled = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Label = "MsgBroadcastEnabled", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete AuthenticationSucceeded = new JoinDataComplete(new JoinData { JoinNumber = 30, JoinSpan = 1 }, new JoinMetadata { Label = "AuthenticationSucceeded", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		public JoinDataComplete AuthenticationFailed = new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 1 }, new JoinMetadata { Label = "AuthenticationFailed", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		public JoinDataComplete DisplayUsage = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Label = "DisplayUsage", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Analog });
		public JoinDataComplete BoradcasetMsgType = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Label = "BoradcasetMsgType", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Analog });

		public JoinDataComplete HelpMsg = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Label = "HelpMsg", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete ErrorMsg = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Label = "ErrorMsg", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete LogText = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Label = "LogText", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete DeviceUsage = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, new JoinMetadata { Label = "DeviceUsage", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete TextMessage = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 }, new JoinMetadata { Label = "TextMessage", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete BroadcastMsg = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Label = "BroadcastMsg", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete FreeBusyStatus = new JoinDataComplete(new JoinData { JoinNumber = 23, JoinSpan = 1 }, new JoinMetadata { Label = "FreeBusyStatus", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete GroupMembership = new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 1 }, new JoinMetadata { Label = "GroupMembership", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		// Scheduling Data Extender
		public JoinDataComplete SchedulingQuerey = new JoinDataComplete(new JoinData { JoinNumber = 32, JoinSpan = 1 }, new JoinMetadata { Label = "SchedulingQuerey", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete SchedulingCreate = new JoinDataComplete(new JoinData { JoinNumber = 33, JoinSpan = 1 }, new JoinMetadata { Label = "SchedulingCreate", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete SchedulingRemove = new JoinDataComplete(new JoinData { JoinNumber = 34, JoinSpan = 1 }, new JoinMetadata { Label = "SchedulingRemove", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		
		// Room Data Extender
		public JoinDataComplete TimeClockQuery = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Label = "TimeClockQuery", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete ActionQuery = new JoinDataComplete(new JoinData { JoinNumber = 35, JoinSpan = 1 }, new JoinMetadata { Label = "ActionQuery", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		public JoinDataComplete RoomConfigJoin = new JoinDataComplete(new JoinData { JoinNumber = 14, JoinSpan = 1 }, new JoinMetadata { Label = "RoomConfigJoin", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });

		/*
		public const ushort CurrentTime = 41;
		public const ushort CurrentTimeFormat = 41;
		public const ushort CurrentDate = 40;
		public const ushort CurrentDateFormat = 40;
		public const ushort ScheduleGet = 42;
		*/

		public DynFusionJoinMap(uint joinStart) 
            :base(joinStart)
		{
			
		}
		

	}
}