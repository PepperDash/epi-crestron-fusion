using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class DynFusionJoinMap : JoinMapBaseAdvanced
	{
		[JoinName("DeviceName")]
		public JoinDataComplete DeviceName = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Description = "Device Name", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		// Bools 
		[JoinName("Online")]
		public JoinDataComplete Online = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Description = "Fusion Online", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("SystemPowerOn")]
		public JoinDataComplete SystemPowerOn = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Description = "SystemPowerOn", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("SystemPowerOff")]
		public JoinDataComplete SystemPowerOff = new JoinDataComplete(new JoinData { JoinNumber = 4, JoinSpan = 1 }, new JoinMetadata { Description = "SystemPowerOff", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("DisplayPowerOn")]
		public JoinDataComplete DisplayPowerOn = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, new JoinMetadata { Description = "DisplayPowerOn", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("DisplayPowerOff")]
		public JoinDataComplete DisplayPowerOff = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 }, new JoinMetadata { Description = "DisplayPowerOoff", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("MsgBroadcastEnabled")]
		public JoinDataComplete MsgBroadcastEnabled = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Description = "MsgBroadcastEnabled", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("AuthenticationSucceeded")]
		public JoinDataComplete AuthenticationSucceeded = new JoinDataComplete(new JoinData { JoinNumber = 30, JoinSpan = 1 }, new JoinMetadata { Description = "AuthenticationSucceeded", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("AuthenticationFailed")]
		public JoinDataComplete AuthenticationFailed = new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 1 }, new JoinMetadata { Description = "AuthenticationFailed", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });

		[JoinName("DisplayUsage")]
		public JoinDataComplete DisplayUsage = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Description = "DisplayUsage", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Analog });
		[JoinName("BoradcasetMsgType")]
		public JoinDataComplete BoradcasetMsgType = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Description = "BoradcasetMsgType", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Analog });

		[JoinName("HelpMsg")]
		public JoinDataComplete HelpMsg = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Description = "HelpMsg", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ErrorMsg")]
		public JoinDataComplete ErrorMsg = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Description = "ErrorMsg", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("LogText")]
		public JoinDataComplete LogText = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Description = "LogText", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("DeviceUsage")]
		public JoinDataComplete DeviceUsage = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, new JoinMetadata { Description = "DeviceUsage", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("TextMessage")]
		public JoinDataComplete TextMessage = new JoinDataComplete(new JoinData { JoinNumber = 6, JoinSpan = 1 }, new JoinMetadata { Description = "TextMessage", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("BroadcastMsg")]
		public JoinDataComplete BroadcastMsg = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Description = "BroadcastMsg", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FreeBusyStatus")]
		public JoinDataComplete FreeBusyStatus = new JoinDataComplete(new JoinData { JoinNumber = 23, JoinSpan = 1 }, new JoinMetadata { Description = "FreeBusyStatus", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("GroupMembership")]
		public JoinDataComplete GroupMembership = new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 1 }, new JoinMetadata { Description = "GroupMembership", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });

		// Scheduling Data Extender
		[JoinName("SchedulingQuery")]
		public JoinDataComplete SchedulingQuery = new JoinDataComplete(new JoinData { JoinNumber = 32, JoinSpan = 1 }, new JoinMetadata { Description = "SchedulingQuery", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SchedulingCreate")]
		public JoinDataComplete SchedulingCreate = new JoinDataComplete(new JoinData { JoinNumber = 33, JoinSpan = 1 }, new JoinMetadata { Description = "SchedulingCreate", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SchedulingRemove")]
		public JoinDataComplete SchedulingRemove = new JoinDataComplete(new JoinData { JoinNumber = 34, JoinSpan = 1 }, new JoinMetadata { Description = "SchedulingRemove", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });

		// Room Data Extender
		[JoinName("TimeClockQuery")]
		public JoinDataComplete TimeClockQuery = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Description = "TimeClockQuery", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ActionQuery")]
		public JoinDataComplete ActionQuery = new JoinDataComplete(new JoinData { JoinNumber = 35, JoinSpan = 1 }, new JoinMetadata { Description = "ActionQuery", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });
		[JoinName("RoomConfig")]
		public JoinDataComplete RoomConfig = new JoinDataComplete(new JoinData { JoinNumber = 14, JoinSpan = 1 }, new JoinMetadata { Description = "RoomConfigJoin", JoinCapabilities = eJoinCapabilities.ToFromSIMPL, JoinType = eJoinType.Serial });

		/*
		public const ushort CurrentTime = 41;
		public const ushort CurrentTimeFormat = 41;
		public const ushort CurrentDate = 40;
		public const ushort CurrentDateFormat = 40;
		public const ushort ScheduleGet = 42;
		*/

		public DynFusionJoinMap(uint joinStart)
						: this(joinStart, typeof(DynFusionJoinMap))
		{

		}

		protected DynFusionJoinMap(uint joinStart, System.Type type)
			: base(joinStart, type)
		{
		}


	}
}