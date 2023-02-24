using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace DynFusion
{
	public class DynFusionScheduleChangeEventArgs : EventArgs
	{
	    public DynFusionScheduleChangeEventArgs(string someString)
		{
			Data = someString;
		}

	    public string Data { get; set; }
	}
	public class DynFusionSchedule : EssentialsBridgeableDevice	
	{
		public bool FusionOnline = false;

		//public event EventHandler<EventArgs> ScheduleChanged;
		public event EventHandler<DynFusionScheduleChangeEventArgs> ScheduleChanged;
		public event EventHandler UpdateRemainingTime;


	    readonly BoolWithFeedback _enableMeetingReserve15 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingReserve30 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingReserve45 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingReserve60 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingReserve90 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingExtend15 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingExtend30 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingExtend45 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingExtend60 = new BoolWithFeedback();
        readonly BoolWithFeedback _enableMeetingExtend90 = new BoolWithFeedback();

	    private const long SchedulePullTimerTimeout = 300000;
	    private const long SchedulePushTimerTimeout = 90000;

	    DynFusionDevice _dynFusion;
		CTimer _schedulePullTimer;
		CTimer _schedulePushTimer;
		CTimer _getScheduleTimeOut;

	    readonly SchedulingConfig _config;

	    public DynFusionScheduleAvailableRooms AvailableRooms;

		List<ScheduleResponse> _roomAvailabilityScheduleResponse = new List<ScheduleResponse>();

		private readonly BoolWithFeedback _registerdForPush = new BoolWithFeedback();
		private readonly BoolWithFeedback _scheduleBusy = new BoolWithFeedback();

		
		public Event CurrentMeeting;
		Event _nextMeeting;
		Event _thirdMeeting;
		Event _fourthMeeting;
		Event _fifthMeeting;
		Event _sixthMeeting;


		public DynFusionSchedule(string key, string name, SchedulingConfig config)
			: base(key, name)
		{
			try
			{
				_config = config;
			}
			catch (Exception e)
			{
				Debug.Console(2, this, String.Format("Get Schedule Error: {0}", e.Message));
				Debug.ConsoleWithLog(2, this, e.ToString());
			}
		}

	    public RoomSchedule CurrentSchedule { get; set; }

	    public CTimer UpdateRemainingTimeTimer { get; set; }


	    public override bool CustomActivate()
		{

			if (_config.DynFusionKey != null)
			{
				_dynFusion = (DynFusionDevice)DeviceManager.GetDeviceForKey(_config.DynFusionKey);
				
			}
			else
			{
				Debug.Console(0, Debug.ErrorLogLevel.Error, "DynFusionDeviceKey is not present in config file");
				return false; 
			}
			if (_dynFusion == null)
			{
				Debug.Console(0, Debug.ErrorLogLevel.Error, "Error getting DynFusionDevice for key {0}", _config.DynFusionKey);
				return false;
			}
			_dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.Use();
			_dynFusion.FusionSymbol.OnlineStatusChange += FusionSymbolStatusChange;
			_dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.DeviceExtenderSigChange += FusionScheduleExtenderSigChange;
			_dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += FusionRoomDataExtenderSigChange;
			AvailableRooms = new DynFusionScheduleAvailableRooms(_dynFusion);
			return true;
		}

		public void StartSchedPushTimer()
		{
			//Debug.ConsoleWithLog(2, this, "StartSchedPushTimer", 1);
		    if (_schedulePushTimer != null) return;
		    Debug.ConsoleWithLog(2, this, "StartSchedPushTimer START", 1);
		    _schedulePushTimer = new CTimer(GetRoomSchedule, null, SchedulePushTimerTimeout, SchedulePushTimerTimeout);
		}

		public void ResetSchedPushTimer()
		{
			Debug.Console(2, this, "ResetSchedPushTimer", 1);
		    if (_schedulePushTimer == null || _schedulePushTimer.Disposed) return;
		    Debug.ConsoleWithLog(2, this, "ResetSchedPushTimer RESET", 1);
		    _schedulePushTimer.Reset(SchedulePushTimerTimeout, SchedulePushTimerTimeout);
		}
		public void StopSchedPushTimer()
		{
			//Debug.ConsoleWithLog(2, this, "StopSchedPushTimer", 1);
		    if (_schedulePushTimer == null || _schedulePushTimer.Disposed) return;
		    Debug.ConsoleWithLog(2, this, "StopSchedPushTimer STOP", 1);
		    _schedulePullTimer.Stop();
		    _schedulePullTimer.Dispose();
		}



/*
		void AvailableRooms_OnAvailableRoomsUpdate()
		{
			throw new NotImplementedException();
		}
*/

		void FusionSymbolStatusChange(object o, OnlineOfflineEventArgs e)
		{
			Debug.Console(2, this, "FusionSymbolStatusChange {0}", e.DeviceOnLine);
			FusionOnline = e.DeviceOnLine;
		    if (!FusionOnline) return;
		    // GetRoomSchedule();
		    GetPushSchedule();
		    StartUpdateRemainingTimeTimer();
		}

		void GetPushSchedule()
		{
			try
			{
			    if (!FusionOnline) return;
			    const string requestId = "InitialPushRequest";

			    var fusionActionRequest = String.Format("<RequestAction>\n<RequestID>{0}</RequestID>\n" +
			                                        "<ActionID>RegisterPushModel</ActionID>\n" +
			                                        "<Parameters>\n" +
			                                        "<Parameter ID=\"Enabled\" Value=\"1\" />\n" +
			                                        "<Parameter ID=\"RequestID\" Value=\"PushNotification\" />\n" +
			                                        "<Parameter ID=\"Start\" Value=\"00:00:00\" />\n" +
			                                        "<Parameter ID=\"HourSpan\" Value=\"24\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"MeetingID\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"RVMeetingID\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"InstanceID\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"dtStart\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"dtEnd\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"Subject\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"Organizer\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"IsEvent\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"IsPrivate\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"IsExchangePrivate\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"LiveMeeting\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"ShareDocPath\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"PhoneNo\" />\n" +
			                                        "<Parameter ID=\"Field\" Value=\"ParticipantCode\" />\n" +
			                                        "</Parameters>\n" +
			                                        "</RequestAction>\n", requestId);

			    _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery.StringValue = fusionActionRequest;
			}
			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, String.Format("Get Push Schedule Error: {0}", e.Message), 3);
			}
		}

		void GetRoomSchedule(object unused)
		{
			GetRoomSchedule();
		}

		public void GetRoomSchedule()
		{
		    if (_scheduleBusy.Value) return;
		    _scheduleBusy.Value = true;
		    _getScheduleTimeOut = new CTimer(GetRoomScheduleTimeOut, 6000);
		    Debug.Console(2, this, String.Format("Get RoomSchedule"));
		    var roomId = _dynFusion.RoomInformation.Id;
		    const string requestType = "ScheduleRequest";
		    GetFullRoomSchedule(roomId, requestType);
		}

		public void GetRoomScheduleTimeOut(object unused)
		{
			_scheduleBusy.Value = false;
			Debug.ConsoleWithLog(2, this, "Error getRoomScheduleTimeOut");
		}


		void GetFullRoomSchedule(string roomId, string requestType)
		{
			try
			{
			    if (!FusionOnline) return;

			    var rfcTime = String.Format("{0:s}", DateTime.Now);

			    var fusionScheduleRequest = String.Format("<RequestSchedule><RequestID>{0}</RequestID><RoomID>{1}</RoomID><Start>{2}</Start><HourSpan>24</HourSpan></RequestSchedule>", requestType, roomId, rfcTime);

			    _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.ScheduleQuery.StringValue = fusionScheduleRequest;

			    //if (isRegisteredForSchedulePushNotifications)
			    //schedulePushTimer.Stop();                   
			}
			catch (Exception e)
			{
				Debug.Console(2, this, String.Format("Get Full Schedule Error: {0}", e.Message));
				Debug.ConsoleWithLog(2, this, e.ToString());
			}
		}

		void ExtendMeeting(int extendTimeIn)
		{
			try
			{
			    if (!FusionOnline) return;
			    if (CurrentMeeting == null) return;
			    if (CurrentMeeting.IsInProgress)
			    {
			        string extendTime;
			        var meetingId = CurrentMeeting.MeetingId;

			        if (extendTimeIn != 0)
			        {
			            var timeToExtend = CurrentMeeting.DtEnd.Subtract(DateTime.Now);
			            var totalMeetingExtend = (ushort)(timeToExtend.TotalMinutes + extendTimeIn);
			            extendTime = totalMeetingExtend.ToString(CultureInfo.InvariantCulture);
			        }
			        else
			            extendTime = extendTimeIn.ToString(CultureInfo.InvariantCulture);


			        var fusionExtendMeetingRequest = String.Format("<RequestAction><RequestID>ExtendMeetingRequest</RequestID><ActionID>MeetingChange</ActionID><Parameters>" +
			                                                          "<Parameter ID=\"MeetingID\" Value=\"{0}\" /><Parameter ID=\"EndTime\" Value=\"{1}\" /></Parameters></RequestAction>", meetingId, extendTime);

			        Debug.Console(2, this, String.Format("ExtendRequest: {0}", fusionExtendMeetingRequest));
			        _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery.StringValue = fusionExtendMeetingRequest;
			    }
			    else
			    {
			        Debug.Console(2, this, String.Format("No Meeting in Progress"));
			    }
			}
			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, e.ToString());
			}
		}

		void CreateMeeting(ushort meetingTimeIn)
		{
		    try
		    {
		        if (!FusionOnline) return;
		        if (CurrentMeeting != null)
		        {
		            Debug.Console(2, this, String.Format("Meeting In Progress"));
		            return;
		        }

		        var meetingLength = new TimeSpan(0, meetingTimeIn, 0);

		        var startTime = String.Format("{0:s}", DateTime.Now);
		        var endTime = String.Format("{0:s}", DateTime.Now.Add(meetingLength));

		        Debug.Console(2, this, String.Format("Start Time: {0}, End Time: {1}", startTime, endTime));

		        string fusionCreateMeetingRequest = String.Format(
		            "<CreateSchedule><RequestID>ExtendMeetingRequest</RequestID>" +
		            "<Event><dtStart>{0}</dtStart><dtEnd>{1}</dtEnd><Subject>Ad-Hoc Meeting Created</Subject><Organizer>{2}</Organizer>" +
		            "<WelcomeMsg>Meeting Created></WelcomeMsg></Event></CreateSchedule>",
		            startTime, endTime, _dynFusion.RoomInformation.Name);

		        Debug.Console(2, this, String.Format("Create Meeting Request: {0}", fusionCreateMeetingRequest));
		        _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateMeeting.StringValue =
		            fusionCreateMeetingRequest;
		    }
		    catch (Exception e)
		    {
		        Debug.ConsoleWithLog(2, this, e.ToString());
		    }
		}


	    void FusionRoomDataExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{

			try
			{
				var result = Regex.Replace(args.Sig.StringValue, "&(?!(amp|apos|quot|lt|gt);)", "&amp;");

				Debug.Console(2, this, String.Format("Args: {0}", result));
				if (args.Sig == _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQueryResponse && args.Sig.StringValue != null)
				{

					var actionResponseXml = new XmlDocument();
					actionResponseXml.LoadXml(result);

					var actionResponse = actionResponseXml["ActionResponse"];

					if (actionResponse != null)
					{
						var requestId = actionResponse["RequestID"];

					    if (requestId.InnerText == "InitialPushRequest" && actionResponse["ActionID"].InnerText == "RegisterPushModel")
					    {
					        var parameters = actionResponse["Parameters"];

					        foreach (var isRegsitered in from XmlElement parameter in parameters
					            where parameter.HasAttributes
					            select parameter.Attributes
					            into attributes
					            where attributes["ID"].Value == "Registered"
					            select Int32.Parse(attributes["Value"].Value))
					        {
					            switch (isRegsitered)
					            {
					                case 1:
					                    _registerdForPush.Value = true;
					                    Debug.ConsoleWithLog(2, this, string.Format("SchedulePush: {0}", _registerdForPush.Value), 1);
					                    StartSchedPushTimer();
					                    break;
					                case 0:
					                    _registerdForPush.Value = false;
					                    Debug.ConsoleWithLog(2, this, string.Format("SchedulePush: {0}", _registerdForPush.Value), 1);
					                    StopSchedPushTimer();
					                    _schedulePullTimer = new CTimer(GetRoomSchedule, null, SchedulePullTimerTimeout,
					                        SchedulePullTimerTimeout);
					                    break;
					            }
					        }
					    }

					    if (requestId.InnerText == "ExtendMeetingRequest")
						{

							if (actionResponse["ActionID"].InnerText == "MeetingChange")
							{
								GetRoomSchedule(null);
								var parameters = actionResponse["Parameters"];

							    foreach (var attributes in from XmlElement parameter
							        in parameters
							        where parameter.HasAttributes
							        select parameter.Attributes)
							    {
							        switch (attributes["ID"].Value)
							        {
							            case "MeetingID":
							                if (attributes["Value"].Value != null)
							                {
                                                //What is this for?
                                                // ReSharper disable once UnusedVariable
							                    var value = attributes["Value"].Value;
							                }
							                break;
							            case "InstanceID":
							                if (attributes["Value"].Value != null)
							                {
                                                //What is this for?
                                                // ReSharper disable once UnusedVariable
							                    var value = attributes["Value"].Value;
							                }
							                break;
							            case "Status":
							                if (attributes["Value"].Value != null)
							                {
                                                //What is this for?
                                                // ReSharper disable once UnusedVariable
                                                var value = attributes["Value"].Value;
							                }
							                break;
							        }
							    }
							}
						}
					}
				}
				if (args.Sig == _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateResponse)
				{
					GetRoomSchedule();
				}
				else if (args.Sig == _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.RemoveMeeting)
				{
					GetRoomSchedule();
				}
			}
			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, e.ToString());
			}

			//PrintTodaysSchedule();

		}


		void FusionScheduleExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{
			try
			{
				Debug.Console(2, this, string.Format("FusionScheduleExtenderSigChange args {0}", args.Sig.StringValue));
				if (args.Sig == _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.ScheduleResponse)
				{
					var scheduleXml = new XmlDocument();

					scheduleXml = scheduleXml.CustomEscapeDocument(args.Sig.StringValue);

					if (scheduleXml != null)
					{

						Debug.Console(2, this, string.Format("Escaped XML {0}", scheduleXml.ToString()));

						var response = scheduleXml["ScheduleResponse"];
						var responseEvent = scheduleXml.FirstChild.SelectSingleNode("Event");

						if (response != null)
						{
						    ResetSchedPushTimer();


						    switch (response["RequestID"].InnerText)
						    {
						        case "RVRequest":
						        {
						            ProcessRvRequestResponse(response);
						            break;
						        }
						        case "ScheduleRequest":
						        {
						            ProcessScheduleRequestResponse(scheduleXml);
						            break;
						        }
						        case "PushNotification":
						            GetRoomSchedule(null);
						            Debug.Console(2, this, String.Format("Got a Push Notification!"));
						            break;
						        case "AvailableRoomSchedule":
						            ProcessAvailableRoomScheduleResponse(responseEvent, scheduleXml);
						            break;
						    }

						}
					}
				}
				if (args.Sig == _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateResponse)
				{
					GetRoomSchedule();
				}
				else if (args.Sig == _dynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.RemoveMeeting)
				{
					GetRoomSchedule();
				}
			}

			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, "{0}\n{1}\n{2}", e.InnerException, e.Message, e.StackTrace);
			}

		}

	    private void ProcessAvailableRoomScheduleResponse(XmlNode responseEvent, XmlDocument scheduleXml)
	    {
	        if (responseEvent == null) return;
	        _roomAvailabilityScheduleResponse = null;

	        foreach (XmlElement element in scheduleXml.FirstChild.ChildNodes)
	        {
	            var availibleSchedule = new ScheduleResponse();

	            switch (element.Name)
	            {
	                case "RequestID":
	                    availibleSchedule.RequestId = element.InnerText;
	                    break;
	                case "RoomID":
	                    availibleSchedule.RoomId = element.InnerText;
	                    break;
	                case "RoomName":
	                    availibleSchedule.RoomName = element.InnerText;
	                    break;
	                case "Event":
	                {
	                    var readerXml = new XmlReader(element.OuterXml);

	                    var roomAvailabilityScheduleEvent = CrestronXMLSerialization.DeSerializeObject<Event>(readerXml);

	                    availibleSchedule.Events.Add(roomAvailabilityScheduleEvent);
	                }
	                    break;
	            }

	            if (_roomAvailabilityScheduleResponse != null)
	                _roomAvailabilityScheduleResponse.Add(availibleSchedule);
	        }
	    }

	    private void ProcessScheduleRequestResponse(XmlDocument scheduleXml)
	    {
	        CurrentSchedule = new RoomSchedule();

	        CurrentMeeting = null;
	        _nextMeeting = null;
	        _thirdMeeting = null;
	        _fourthMeeting = null;
	        _fifthMeeting = null;
	        _sixthMeeting = null;

	        var scheduleResponse = new ScheduleResponse
	        {
	            RoomName = scheduleXml.FirstChild.SelectSingleNode("RoomName").InnerText,
	            RequestId = scheduleXml.FirstChild.SelectSingleNode("RequestID").InnerText,
	            RoomId = scheduleXml.FirstChild.SelectSingleNode("RoomID").InnerText
	        };

	        var eventStack = scheduleXml.FirstChild.SelectNodes("Event");
	        Debug.Console(2, this, String.Format("EventStack Count: {0}", eventStack.Count));

	        if (eventStack.Count > 0)
	        {
	            ProcessEventStack(eventStack, scheduleResponse);
	        }
	        else
	        {
	            AvailableRooms.SendFreeBusyStatusAvailable();
	        }
	        if (CurrentMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Current Meeting {0}", CurrentMeeting.Subject));
	        }
	        if (_nextMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Next Meeting {0}", _nextMeeting.Subject));
	        }
	        if (_thirdMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Later Meeting {0}", _thirdMeeting.Subject));
	        }
	        if (_fourthMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Latest Meeting {0}", _fourthMeeting.Subject));
	        }
	        if (_fifthMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Fifth Meeting {0}", _fifthMeeting.Subject));
	        }
	        if (_sixthMeeting != null)
	        {
	            Debug.Console(2, this, String.Format("Sixth Meeting {0}", _sixthMeeting.Subject));
	        }


	        _getScheduleTimeOut.Stop();
	        var handler = ScheduleChanged;
	        if (handler != null)
	        {
	            Debug.Console(2, this, String.Format("Schedule Changed Firing Event!"));
	            handler(this, new DynFusionScheduleChangeEventArgs("BAM!"));
	        }


	        _scheduleBusy.Value = false;
	    }

	    private void ProcessEventStack(XmlNodeList eventStack, ScheduleResponse scheduleResponse)
	    {
	        var tempEvent = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(0).OuterXml));
	        scheduleResponse.Events.Add(tempEvent);
	        if (tempEvent.IsInProgress)
	        {
	            CurrentMeeting = new Event();
	            CurrentMeeting = tempEvent;
	            if (eventStack.Count > 1)
	            {
	                _nextMeeting = new Event();
	                _nextMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(1).OuterXml));
	            }
	            if (eventStack.Count > 2)
	            {
	                _thirdMeeting = new Event();
	                _thirdMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml));
	            }
	            if (eventStack.Count > 3)
	            {
	                _fourthMeeting = new Event();
	                _fourthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml));
	            }
	            if (eventStack.Count > 4)
	            {
	                _fifthMeeting = new Event();
	                _fifthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml));
	            }
	            if (eventStack.Count > 5)
	            {
	                _sixthMeeting = new Event();
	                _sixthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml));
	            }

	            AvailableRooms.SendFreeBusyStatusNotAvailable();
	        }
	        else
	        {
	            _nextMeeting = new Event();
	            _nextMeeting = tempEvent;
	            if (eventStack.Count > 1)
	            {
	                _thirdMeeting = new Event();
	                _thirdMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(1).OuterXml));
	            }
	            if (eventStack.Count > 2)
	            {
	                _fourthMeeting = new Event();
	                _fourthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml));
	            }
	            if (eventStack.Count > 3)
	            {
	                _fifthMeeting = new Event();
	                _fifthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml));
	            }
	            if (eventStack.Count > 4)
	            {
	                _sixthMeeting = new Event();
	                _sixthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml));
	            }
	            AvailableRooms.SendFreeBusyStatusAvailableUntil(_nextMeeting.DtStart);
	        }
	    }

	    private void ProcessRvRequestResponse(XmlElement response)
	    {
	        var action = response["Action"];

	        if (action.OuterXml.IndexOf("RequestSchedule", StringComparison.Ordinal) > -1)
	        {
	            GetRoomSchedule();
	        }
	    }

	    void StartUpdateRemainingTimeTimer()
		{
			UpdateRemainingTimeTimer = new CTimer(_UpdateRemainingTime, null, Timeout.Infinite, 60000);
		}



		void _UpdateRemainingTime(object o)
		{
			var handler = UpdateRemainingTime;
			if (handler != null)
			{
				handler(this, new EventArgs());
			}

		}


		// TODO: Eventually we should move this to an event and have the API update based on an event change. Encapsulation. JTA 2018-04-04
/*
		void UpdateMeetingInfo()
		{
			try
			{
				Debug.Console(2, this, "UpdateMeetingInfo");

				_updateRemainingTimeTimer.Reset(0, 60000);

			}
			catch (Exception ex)
			{
				Debug.ConsoleWithLog(2, this, ex.ToString());
			}
		}
*/

		void CheckMeetingExtend()
		{
			try
			{
				if (CurrentMeeting != null)
				{
					if (_nextMeeting != null)
					{
						var timeToNext = _nextMeeting.DtStart.Subtract(CurrentMeeting.DtEnd);

					    _enableMeetingExtend15.Value = timeToNext.TotalMinutes >= 15;
                        _enableMeetingExtend30.Value = timeToNext.TotalMinutes >= 30;
                        _enableMeetingExtend45.Value = timeToNext.TotalMinutes >= 45;
                        _enableMeetingExtend60.Value = timeToNext.TotalMinutes >= 60;
                        _enableMeetingExtend90.Value = timeToNext.TotalMinutes >= 90;


					}
					else
					{
						_enableMeetingExtend15.Value = true;
						_enableMeetingExtend30.Value = true;
						_enableMeetingExtend45.Value = true;
						_enableMeetingExtend60.Value = true;
						_enableMeetingExtend90.Value = true;
					}
				}
				else
				{
					_enableMeetingExtend15.Value = false;
					_enableMeetingExtend30.Value = false;
					_enableMeetingExtend45.Value = false;
					_enableMeetingExtend60.Value = false;
					_enableMeetingExtend90.Value = false;
				}
			}
			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, e.ToString());
			}



		}

		void CheckMeetingReserve()
		{
			try
			{
				if (CurrentMeeting == null)
				{
					if (_nextMeeting != null)
					{
						var timeToNext = _nextMeeting.DtStart.Subtract(DateTime.Now);

                        _enableMeetingReserve15.Value = timeToNext.TotalMinutes >= 15;
                        _enableMeetingReserve30.Value = timeToNext.TotalMinutes >= 30;
                        _enableMeetingReserve45.Value = timeToNext.TotalMinutes >= 45;
                        _enableMeetingReserve60.Value = timeToNext.TotalMinutes >= 60;
                        _enableMeetingReserve90.Value = timeToNext.TotalMinutes >= 90;

					}
					else
					{
						_enableMeetingReserve15.Value = true;
						_enableMeetingReserve30.Value = true;
						_enableMeetingReserve45.Value = true;
						_enableMeetingReserve60.Value = true;
						_enableMeetingReserve90.Value = true;
					}
				}
				else
				{
					_enableMeetingReserve15.Value = false;
					_enableMeetingReserve30.Value = false;
					_enableMeetingReserve45.Value = false;
					_enableMeetingReserve60.Value = false;
					_enableMeetingReserve90.Value = false;
				}
			}
			catch (Exception e)
			{
				Debug.ConsoleWithLog(2, this, e.ToString());
			}


		}

		public override void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey, PepperDash.Essentials.Core.Bridges.EiscApiAdvanced bridge)
		{
			try
			{

				var joinMap = new SchedulingJoinMap(joinStart);
				_scheduleBusy.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ScheduleBusy.JoinNumber]);



				trilist.SetSigTrueAction(joinMap.GetSchedule.JoinNumber, GetRoomSchedule);
				trilist.SetSigTrueAction(joinMap.GetRoomList.JoinNumber, () => AvailableRooms.GetRoomList());
				trilist.SetSigTrueAction(joinMap.CheckMeetings.JoinNumber, () => { CheckMeetingReserve(); CheckMeetingExtend(); });
				trilist.SetSigTrueAction(joinMap.ExtendMeeting15Minutes.JoinNumber, () => ExtendMeeting(15));
				trilist.SetSigTrueAction(joinMap.ExtendMeeting30Minutes.JoinNumber, () => ExtendMeeting(30));
				trilist.SetSigTrueAction(joinMap.ExtendMeeting45Minutes.JoinNumber, () => ExtendMeeting(45));
				trilist.SetSigTrueAction(joinMap.ExtendMeeting60Minutes.JoinNumber, () => ExtendMeeting(60));
				trilist.SetSigTrueAction(joinMap.ExtendMeeting90Minutes.JoinNumber, () => ExtendMeeting(90));
				trilist.SetSigTrueAction(joinMap.EndCurrentMeeting.JoinNumber, () => ExtendMeeting(0));
				trilist.SetSigTrueAction(joinMap.ReserveMeeting15Minutes.JoinNumber, () => CreateMeeting(15));
				trilist.SetSigTrueAction(joinMap.ReserveMeeting30Minutes.JoinNumber, () => CreateMeeting(30));
				trilist.SetSigTrueAction(joinMap.ReserveMeeting45Minutes.JoinNumber, () => CreateMeeting(45));
				trilist.SetSigTrueAction(joinMap.ReserveMeeting60Minutes.JoinNumber, () => CreateMeeting(60));
				trilist.SetSigTrueAction(joinMap.ReserveMeeting90Minutes.JoinNumber, () => CreateMeeting(90));

				_registerdForPush.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.PushNotificationRegistered.JoinNumber]);
				_enableMeetingExtend15.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting15Minutes.JoinNumber]);
				_enableMeetingExtend30.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting30Minutes.JoinNumber]);
				_enableMeetingExtend45.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting45Minutes.JoinNumber]);
				_enableMeetingExtend60.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting60Minutes.JoinNumber]);
				_enableMeetingExtend90.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting90Minutes.JoinNumber]);
				_enableMeetingReserve15.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting15Minutes.JoinNumber]);
				_enableMeetingReserve30.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting30Minutes.JoinNumber]);
				_enableMeetingReserve45.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting45Minutes.JoinNumber]);
				_enableMeetingReserve60.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting60Minutes.JoinNumber]);
				_enableMeetingReserve90.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting90Minutes.JoinNumber]);



				UpdateRemainingTime += (s, e) =>
				{
				    CheckMeetingExtend();
				    CheckMeetingReserve();
				    if (CurrentMeeting != null && !CurrentMeeting.IsInProgress) { GetRoomSchedule(); }
				    if (_nextMeeting != null && _nextMeeting.IsInProgress) { GetRoomSchedule(); }
				    Debug.Console(2, this, "UpdateRemainingTime");
				    if (CurrentMeeting != null)
				    {
				        trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = CurrentMeeting.TimeRemainingString;
				        trilist.UShortInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(CurrentMeeting.TimeRemainingInMin);
				    }
				    else
				    {
				        trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = "";
				        trilist.UShortInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].UShortValue = 0;
				    }
				    if (_nextMeeting != null)
				    {
				        trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = _nextMeeting.TimeRemainingString;
				        trilist.UShortInput[joinMap.NextMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(_nextMeeting.TimeRemainingInMin);

				        var date = DateTime.Parse(_nextMeeting.StartDate.ToString(CultureInfo.InvariantCulture));
				        trilist.BooleanInput[joinMap.NextMeetingIsToday.JoinNumber].BoolValue = date.Date == DateTime.Today.Date;

				    }
				    if (_thirdMeeting != null)
				    {
				        trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = _thirdMeeting.TimeRemainingString;
				        trilist.UShortInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(_thirdMeeting.TimeRemainingInMin);
				    }
				    if (_fourthMeeting != null)
				    {
				        trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = _fourthMeeting.TimeRemainingString;	
				        trilist.UShortInput[joinMap.FourthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(_fourthMeeting.TimeRemainingInMin);
				    }
				    if (_fifthMeeting != null)
				    {
				        trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = _fifthMeeting.TimeRemainingString;
				        trilist.UShortInput[joinMap.FifthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(_fifthMeeting.TimeRemainingInMin);
				    }
				    if (_sixthMeeting != null)
				    {
				        trilist.StringInput[joinMap.SixthMeetingRemainingTime.JoinNumber].StringValue = _sixthMeeting.TimeRemainingString;
				        trilist.UShortInput[joinMap.SixthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(_sixthMeeting.TimeRemainingInMin);
				    }
				};

				ScheduleChanged += ((s, e) =>
				{
					try
					{
						Debug.Console(2, this, "ScheduleChanged");
						if (CurrentMeeting != null)
						{
							trilist.StringInput[joinMap.CurrentMeetingOrganizer.JoinNumber].StringValue = CurrentMeeting.Organizer;
							trilist.StringInput[joinMap.CurrentMeetingSubject.JoinNumber].StringValue = CurrentMeeting.Subject;
							trilist.StringInput[joinMap.CurrentMeetingMeetingId.JoinNumber].StringValue = CurrentMeeting.MeetingId;
							trilist.StringInput[joinMap.CurrentMeetingStartTime.JoinNumber].StringValue = CurrentMeeting.StartTime;
							trilist.StringInput[joinMap.CurrentMeetingStartDate.JoinNumber].StringValue = CurrentMeeting.StartDate;
							trilist.StringInput[joinMap.CurrentMeetingEndTime.JoinNumber].StringValue = CurrentMeeting.EndTime;
							trilist.StringInput[joinMap.CurrentMeetingEndDate.JoinNumber].StringValue = CurrentMeeting.EndDate;
							trilist.StringInput[joinMap.CurrentMeetingDuration.JoinNumber].StringValue = CurrentMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = CurrentMeeting.TimeRemainingString;
							trilist.UShortInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(CurrentMeeting.TimeRemainingInMin);
							trilist.BooleanInput[joinMap.MeetingInProgress.JoinNumber].BoolValue = CurrentMeeting.IsInProgress;
						}
						else
						{
							trilist.StringInput[joinMap.CurrentMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = "";
							trilist.BooleanInput[joinMap.MeetingInProgress.JoinNumber].BoolValue = false;
						}

						if (_nextMeeting != null)
						{
							trilist.StringInput[joinMap.NextMeetingOrganizer.JoinNumber].StringValue = _nextMeeting.Organizer;
							trilist.StringInput[joinMap.NextMeetingSubject.JoinNumber].StringValue = _nextMeeting.Subject;
							trilist.StringInput[joinMap.NextMeetingMeetingId.JoinNumber].StringValue = _nextMeeting.MeetingId;
							trilist.StringInput[joinMap.NextMeetingStartTime.JoinNumber].StringValue = _nextMeeting.StartTime;
							trilist.StringInput[joinMap.NextMeetingStartDate.JoinNumber].StringValue = _nextMeeting.StartDate;
							trilist.StringInput[joinMap.NextMeetingEndTime.JoinNumber].StringValue = _nextMeeting.EndTime;
							trilist.StringInput[joinMap.NextMeetingEndDate.JoinNumber].StringValue = _nextMeeting.EndDate;
							trilist.StringInput[joinMap.NextMeetingDuration.JoinNumber].StringValue = _nextMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = _nextMeeting.TimeRemainingString;


						}
						else
						{
							trilist.StringInput[joinMap.NextMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = "";
							trilist.BooleanInput[joinMap.NextMeetingIsToday.JoinNumber].BoolValue = false;
						}

						if (_thirdMeeting != null)
						{
							trilist.StringInput[joinMap.ThirdMeetingOrganizer.JoinNumber].StringValue = _thirdMeeting.Organizer;
							trilist.StringInput[joinMap.ThirdMeetingSubject.JoinNumber].StringValue = _thirdMeeting.Subject;
							trilist.StringInput[joinMap.ThirdMeetingMeetingId.JoinNumber].StringValue = _thirdMeeting.MeetingId;
							trilist.StringInput[joinMap.ThirdMeetingStartTime.JoinNumber].StringValue = _thirdMeeting.StartTime;
							trilist.StringInput[joinMap.ThirdMeetingStartDate.JoinNumber].StringValue = _thirdMeeting.StartDate;
							trilist.StringInput[joinMap.ThirdMeetingEndTime.JoinNumber].StringValue = _thirdMeeting.EndTime;
							trilist.StringInput[joinMap.ThirdMeetingEndDate.JoinNumber].StringValue = _thirdMeeting.EndDate;
							trilist.StringInput[joinMap.ThirdMeetingDuration.JoinNumber].StringValue = _thirdMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = _thirdMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.ThirdMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = "";
						}

						if (_fourthMeeting != null)
						{
							trilist.StringInput[joinMap.FourthMeetingOrganizer.JoinNumber].StringValue = _fourthMeeting.Organizer;
							trilist.StringInput[joinMap.FourthMeetingSubject.JoinNumber].StringValue = _fourthMeeting.Subject;
							trilist.StringInput[joinMap.FourthMeetingMeetingId.JoinNumber].StringValue = _fourthMeeting.MeetingId;
							trilist.StringInput[joinMap.FourthMeetingStartTime.JoinNumber].StringValue = _fourthMeeting.StartTime;
							trilist.StringInput[joinMap.FourthMeetingStartDate.JoinNumber].StringValue = _fourthMeeting.StartDate;
							trilist.StringInput[joinMap.FourthMeetingEndTime.JoinNumber].StringValue = _fourthMeeting.EndTime;
							trilist.StringInput[joinMap.FourthMeetingEndDate.JoinNumber].StringValue = _fourthMeeting.EndDate;
							trilist.StringInput[joinMap.FourthMeetingDuration.JoinNumber].StringValue = _fourthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = _fourthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.FourthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = "";
						}
						if (_fifthMeeting != null)
						{
							trilist.StringInput[joinMap.FifthMeetingOrganizer.JoinNumber].StringValue = _fifthMeeting.Organizer;
							trilist.StringInput[joinMap.FifthMeetingSubject.JoinNumber].StringValue = _fifthMeeting.Subject;
							trilist.StringInput[joinMap.FifthMeetingMeetingId.JoinNumber].StringValue = _fifthMeeting.MeetingId;
							trilist.StringInput[joinMap.FifthMeetingStartTime.JoinNumber].StringValue = _fifthMeeting.StartTime;
							trilist.StringInput[joinMap.FifthMeetingStartDate.JoinNumber].StringValue = _fifthMeeting.StartDate;
							trilist.StringInput[joinMap.FifthMeetingEndTime.JoinNumber].StringValue = _fifthMeeting.EndTime;
							trilist.StringInput[joinMap.FifthMeetingEndDate.JoinNumber].StringValue = _fifthMeeting.EndDate;
							trilist.StringInput[joinMap.FifthMeetingDuration.JoinNumber].StringValue = _fifthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = _fifthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.FifthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = "";
						}
						if (_sixthMeeting != null)
						{
							trilist.StringInput[joinMap.SixthMeetingOrganizer.JoinNumber].StringValue = _sixthMeeting.Organizer;
							trilist.StringInput[joinMap.SixthMeetingSubject.JoinNumber].StringValue = _sixthMeeting.Subject;
							trilist.StringInput[joinMap.SixthMeetingMeetingId.JoinNumber].StringValue = _sixthMeeting.MeetingId;
							trilist.StringInput[joinMap.SixthMeetingStartTime.JoinNumber].StringValue = _sixthMeeting.StartTime;
							trilist.StringInput[joinMap.SixthMeetingStartDate.JoinNumber].StringValue = _sixthMeeting.StartDate;
							trilist.StringInput[joinMap.SixthMeetingEndTime.JoinNumber].StringValue = _sixthMeeting.EndTime;
							trilist.StringInput[joinMap.SixthMeetingEndDate.JoinNumber].StringValue = _sixthMeeting.EndDate;
							trilist.StringInput[joinMap.SixthMeetingDuration.JoinNumber].StringValue = _sixthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.SixthMeetingRemainingTime.JoinNumber].StringValue = _sixthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.SixthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingMeetingId.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingRemainingTime.JoinNumber].StringValue = "";
						}
					}
					catch (Exception ex)
					{
						Debug.Console(0, this, Debug.ErrorLogLevel.Error, ex.Message);
					}
				});

			}
			catch (Exception ex)
			{
				Debug.Console(0, this, Debug.ErrorLogLevel.Error, ex.Message);
			}
		}
	
	
		}
	


	//************************************************************************************************************************************ 

	public class RoomSchedule
	{
		public List<Event> Meetings { get; set; }

		public RoomSchedule()
		{
			Meetings = new List<Event>();
		}
	}

	//************************************************************************************************************************************
	// Helper Classes
	public class LocalTimeRequest
	{
		public string RequestId { get; set; }
	}

	public class RequestSchedule
	{
		public string RequestId { get; set; }
		public string RoomId { get; set; }
		public DateTime Start { get; set; }
		public double HourSpan { get; set; }

		public RequestSchedule(string requestId, string roomId)
		{
			RequestId = requestId;
			RoomId = roomId;
			Start = DateTime.Now;
			HourSpan = 24;
		}
	}

	public class RequestAction
	{
		public string RequestId { get; set; }
		public string RoomId { get; set; }
		public string ActionId { get; set; }
		public List<Parameter> Parameters { get; set; }

		public RequestAction(string roomId, string actionId, List<Parameter> parameters)
		{
			RoomId = roomId;
			ActionId = actionId;
			Parameters = parameters;
		}
	}

	public class ActionResponse
	{
		public string RequsetId { get; set; }
		public string ActionId { get; set; }
		public List<Parameter> Parameters { get; set; }
	}

	public class Parameter
	{
		public string Id { get; set; }
		public string Value { get; set; }
	}

	public class ScheduleResponse
	{
		public string RequestId { get; set; }
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public List<Event> Events { get; set; }

		public ScheduleResponse()
		{
			Events = new List<Event>();
		}
	}

	public class Event
	{
		public string Recurring { get; set; }
		public string MeetingId { get; set; }
		public string RvMeetingId { get; set; }
		public DateTime DtStart { get; set; }
		public DateTime DtEnd { get; set; }
		public string Organizer { get; set; }
		public string Subject { get; set; }
		public string IsPrivate { get; set; }
		public string IsExchangePrivate { get; set; }
		public Attendees Attendees { get; set; }
		// public Resources Resources { get; set; }
		public string IsEvent { get; set; }
		public string IsRoomViewMeeting { get; set; }
		public MeetingTypes MeetingTypes { get; set; }
		public LiveMeeting LiveMeeting { get; set; }
		public string WelcomeMsg { get; set; }
		public string Body { get; set; }
		public string Location { get; set; }
		public string ShareDocPath { get; set; }
		public string ParticipantCode { get; set; }
		public string PhoneNo { get; set; }
		public string InstanceId { get; set; }

	    public string StartTime
		{
			get
			{
				var startTimeShort = DtStart.ToShortTimeString();

				return startTimeShort;

			}
		}

		public string StartDate
		{
			get
			{
			    var startDateShort = DtStart.ToShortDateString();

			    return startDateShort;
			}
		}

		public string EndTime
		{
			get
			{
				var endTimeShort = DtEnd.ToShortTimeString();

				return endTimeShort;

			}
		}

		public string EndDate
		{
			get
			{
				var endDateShort = DtEnd.ToShortDateString();

				return endDateShort;

			}
		}

		public string DurationInMinutes
		{
			get
			{
			    var timeSpan = DtEnd.Subtract(DtStart);
				var hours = timeSpan.Hours;
				double minutes = timeSpan.Minutes;
				var minutesRounded = Math.Round(minutes);
				var duration = hours > 0 ? String.Format("{0} Hour{2} {1} Minutes", hours, minutesRounded, hours > 1 ? "s" : string.Empty) : String.Format("{0} Minutes", minutesRounded);

				return duration;
			}
		}
		public double TimeRemainingInMin
		{

			get
			{
			    var timeMarker = DtStart <= DateTime.Now ? DtEnd : DtStart;

				var totalMinutes = timeMarker.Subtract(DateTime.Now).TotalMinutes;
				return totalMinutes >= 0 ? Math.Round(totalMinutes) : 0;
			}
		}
		public string TimeRemainingString
		{
			get
			{
			    var timeMarker = GetInProgress() ? DtEnd : DtStart;

			    var hours = timeMarker.Subtract(DateTime.Now).Hours;
				var minutes = timeMarker.Subtract(DateTime.Now).Minutes;

			    var hourTag = hours > 1 ? "Hours" : "Hour";
				var minTag = minutes == 1 ? "Minute" : "Minutes";

				var remainingTimeString = hourTag.Length == 0 ? string.Format("{0} {1}", minutes, minTag) : string.Format("{0} {1} {2} {3}", hours, hourTag, minutes, minTag);

				return remainingTimeString;

			}
		}

		public bool IsInProgress
		{
			get
			{
				return GetInProgress();
			}
		}

		bool GetInProgress()
		{
		    var now = DateTime.Now;

		    return now > DtStart && now < DtEnd;
		}
	}

	public class Attendees
	{
		public Required Required { get; set; }
		public Optional Optional { get; set; }
	}

	public class Required
	{
		public List<string> Atendee { get; set; }
	}

	public class Optional
	{
		public List<string> Atendee { get; set; }
	}
	/*
	public class Resources
	{
		public Rooms Rooms { get; set; }
	}
	*/






	public class MeetingType
	{
		public string Id { get; set; }
		public string Value { get; set; }
	}

	public class MeetingTypes
	{
		public List<MeetingType> MeetingType { get; set; }
	}

	public class LiveMeeting
	{
		public string Url { get; set; }
		public string Id { get; set; }
		public string Key { get; set; }
		public string Subject { get; set; }
	}

	public class LiveMeetingUrl
	{
		public LiveMeeting LiveMeeting { get; set; }
	}
}