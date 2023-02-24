using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class SchedulingJoinMap : JoinMapBaseAdvanced
	{
		[JoinName("GetSchedule")]
		public JoinDataComplete GetSchedule = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Description = "GetSchedule", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("EndCurrentMeeting")]
		public JoinDataComplete EndCurrentMeeting = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Description = "EndCurrentMeeting", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("CheckMeetings")]
		public JoinDataComplete CheckMeetings = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Description = "CheckMeetings", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });

		[JoinName("ScheduleBusy")]
		public JoinDataComplete ScheduleBusy = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Description = "ScheduleBusy", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("GetRoomInfo")]
		public JoinDataComplete GetRoomInfo = new JoinDataComplete(new JoinData { JoinNumber = 4, JoinSpan = 1 }, new JoinMetadata { Description = "GetRoomInfo", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("GetRoomList")]
		public JoinDataComplete GetRoomList = new JoinDataComplete(new JoinData { JoinNumber = 5, JoinSpan = 1 }, new JoinMetadata { Description = "GetRoomList", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("PushNotificationRegistered")]
		public JoinDataComplete PushNotificationRegistered = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Description = "PushNotificationRegistered", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("MeetingInProgress")]
		public JoinDataComplete MeetingInProgress = new JoinDataComplete(new JoinData { JoinNumber = 1, JoinSpan = 1 }, new JoinMetadata { Description = "GetSchedule", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ExtendMeeting15Minutes")]
		public JoinDataComplete ExtendMeeting15Minutes = new JoinDataComplete(new JoinData { JoinNumber = 11, JoinSpan = 1 }, new JoinMetadata { Description = "ExtendMeeting15Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ExtendMeeting30Minutes")]
		public JoinDataComplete ExtendMeeting30Minutes = new JoinDataComplete(new JoinData { JoinNumber = 12, JoinSpan = 1 }, new JoinMetadata { Description = "ExtendMeeting30Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ExtendMeeting45Minutes")]
		public JoinDataComplete ExtendMeeting45Minutes = new JoinDataComplete(new JoinData { JoinNumber = 13, JoinSpan = 1 }, new JoinMetadata { Description = "ExtendMeeting45Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ExtendMeeting60Minutes")]
		public JoinDataComplete ExtendMeeting60Minutes = new JoinDataComplete(new JoinData { JoinNumber = 14, JoinSpan = 1 }, new JoinMetadata { Description = "ExtendMeeting60Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ExtendMeeting90Minutes")]
		public JoinDataComplete ExtendMeeting90Minutes = new JoinDataComplete(new JoinData { JoinNumber = 15, JoinSpan = 1 }, new JoinMetadata { Description = "ExtendMeeting90Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ReserveMeeting15Minutes")]
		public JoinDataComplete ReserveMeeting15Minutes = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Description = "ReserveMeeting15Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ReserveMeeting30Minutes")]
		public JoinDataComplete ReserveMeeting30Minutes = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Description = "ReserveMeeting30Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ReserveMeeting45Minutes")]
		public JoinDataComplete ReserveMeeting45Minutes = new JoinDataComplete(new JoinData { JoinNumber = 23, JoinSpan = 1 }, new JoinMetadata { Description = "ReserveMeeting45Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ReserveMeeting60Minutes")]
		public JoinDataComplete ReserveMeeting60Minutes = new JoinDataComplete(new JoinData { JoinNumber = 24, JoinSpan = 1 }, new JoinMetadata { Description = "ReserveMeeting60Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ReserveMeeting90Minutes")]
		public JoinDataComplete ReserveMeeting90Minutes = new JoinDataComplete(new JoinData { JoinNumber = 25, JoinSpan = 1 }, new JoinMetadata { Description = "ReserveMeeting90Minutes", JoinCapabilities = eJoinCapabilities.FromSIMPL, JoinType = eJoinType.Digital });


		[JoinName("RoomID")]
		public JoinDataComplete RoomId = new JoinDataComplete(new JoinData { JoinNumber = 2, JoinSpan = 1 }, new JoinMetadata { Description = "RoomID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("RoomLocation")]
		public JoinDataComplete RoomLocation = new JoinDataComplete(new JoinData { JoinNumber = 3, JoinSpan = 1 }, new JoinMetadata { Description = "RoomLocation", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingOrganizer")]
		public JoinDataComplete CurrentMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 21, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingSubject")]
		public JoinDataComplete CurrentMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 22, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingMeetingID")]
		public JoinDataComplete CurrentMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 23, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingStartTime")]
		public JoinDataComplete CurrentMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 24, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingStartDate")]
		public JoinDataComplete CurrentMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 25, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingEndTime")]
		public JoinDataComplete CurrentMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 26, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingEndDate")]
		public JoinDataComplete CurrentMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 27, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingDuration")]
		public JoinDataComplete CurrentMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 28, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("CurrentMeetingRemainingTime")]
		public JoinDataComplete CurrentMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 29, JoinSpan = 1 }, new JoinMetadata { Description = "CurrentMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingOrganizer")]
		public JoinDataComplete NextMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 31, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingSubject")]
		public JoinDataComplete NextMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 32, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingMeetingID")]
		public JoinDataComplete NextMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 33, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingStartTime")]
		public JoinDataComplete NextMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 34, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingStartDate")]
		public JoinDataComplete NextMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 35, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingEndTime")]
		public JoinDataComplete NextMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 36, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingEndDate")]
		public JoinDataComplete NextMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 37, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingDuration")]
		public JoinDataComplete NextMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 38, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingRemainingTime")]
		public JoinDataComplete NextMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 39, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("NextMeetingIsToday")]
		public JoinDataComplete NextMeetingIsToday = new JoinDataComplete(new JoinData { JoinNumber = 35, JoinSpan = 1 }, new JoinMetadata { Description = "NextMeetingIsToday", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Digital });
		[JoinName("ThirdMeetingOrganizer")]
		public JoinDataComplete ThirdMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 41, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingSubject")]
		public JoinDataComplete ThirdMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 42, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingMeetingID")]
		public JoinDataComplete ThirdMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 43, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingStartTime")]
		public JoinDataComplete ThirdMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 44, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingStartDate")]
		public JoinDataComplete ThirdMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 45, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingEndTime")]
		public JoinDataComplete ThirdMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 46, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingEndDate")]
		public JoinDataComplete ThirdMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 47, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingDuration")]
		public JoinDataComplete ThirdMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 48, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("ThirdMeetingRemainingTime")]
		public JoinDataComplete ThirdMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 49, JoinSpan = 1 }, new JoinMetadata { Description = "ThirdMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingOrganizer")]
		public JoinDataComplete FourthMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 51, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingSubject")]
		public JoinDataComplete FourthMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 52, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingMeetingID")]
		public JoinDataComplete FourthMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 53, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingStartTime")]
		public JoinDataComplete FourthMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 54, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingStartDate")]
		public JoinDataComplete FourthMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 55, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingEndTime")]
		public JoinDataComplete FourthMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 56, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingEndDate")]
		public JoinDataComplete FourthMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 57, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingDuration")]
		public JoinDataComplete FourthMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 58, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FourthMeetingRemainingTime")]
		public JoinDataComplete FourthMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 59, JoinSpan = 1 }, new JoinMetadata { Description = "FourthMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingOrganizer")]
		public JoinDataComplete FifthMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 61, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingSubject")]
		public JoinDataComplete FifthMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 62, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingMeetingID")]
		public JoinDataComplete FifthMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 63, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingStartTime")]
		public JoinDataComplete FifthMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 64, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingStartDate")]
		public JoinDataComplete FifthMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 65, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingEndTime")]
		public JoinDataComplete FifthMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 66, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingEndDate")]
		public JoinDataComplete FifthMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 67, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingDuration")]
		public JoinDataComplete FifthMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 68, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("FifthMeetingRemainingTime")]
		public JoinDataComplete FifthMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 69, JoinSpan = 1 }, new JoinMetadata { Description = "FifthMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingOrganizer")]
		public JoinDataComplete SixthMeetingOrganizer = new JoinDataComplete(new JoinData { JoinNumber = 71, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingOrganizer", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingSubject")]
		public JoinDataComplete SixthMeetingSubject = new JoinDataComplete(new JoinData { JoinNumber = 72, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingSubject", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingMeetingID")]
		public JoinDataComplete SixthMeetingMeetingId = new JoinDataComplete(new JoinData { JoinNumber = 73, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingMeetingID", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingStartTime")]
		public JoinDataComplete SixthMeetingStartTime = new JoinDataComplete(new JoinData { JoinNumber = 74, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingStartTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingStartDate")]
		public JoinDataComplete SixthMeetingStartDate = new JoinDataComplete(new JoinData { JoinNumber = 75, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingStartDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingEndTime")]
		public JoinDataComplete SixthMeetingEndTime = new JoinDataComplete(new JoinData { JoinNumber = 76, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingEndTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingEndDate")]
		public JoinDataComplete SixthMeetingEndDate = new JoinDataComplete(new JoinData { JoinNumber = 77, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingEndDate", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingDuration")]
		public JoinDataComplete SixthMeetingDuration = new JoinDataComplete(new JoinData { JoinNumber = 78, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingDuration", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		[JoinName("SixthMeetingRemainingTime")]
		public JoinDataComplete SixthMeetingRemainingTime = new JoinDataComplete(new JoinData { JoinNumber = 79, JoinSpan = 1 }, new JoinMetadata { Description = "SixthMeetingRemainingTime", JoinCapabilities = eJoinCapabilities.ToSIMPL, JoinType = eJoinType.Serial });
		
		public SchedulingJoinMap(uint joinStart)
			: this(joinStart, typeof(SchedulingJoinMap))
        {
        }

        /// <summary>
        /// Constructor to use when extending this Join map
        /// </summary>
        /// <param name="joinStart">Join this join map will start at</param>
        /// <param name="type">Type of the child join map</param>
		protected SchedulingJoinMap(uint joinStart, System.Type type)
			: base(joinStart, type)
        {
        }

	}
}