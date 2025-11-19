using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Core.Logging;

namespace DynFusion
{
	public class DynFusionScheduleChangeEventArgs : EventArgs
	{
		private readonly string data;
		public DynFusionScheduleChangeEventArgs(string someString)
		{
			data = someString;
		}

	}
	public class DynFusionSchedule : EssentialsBridgeableDevice
	{
		public bool fusionOnline = false;

		public event EventHandler<DynFusionScheduleChangeEventArgs> ScheduleChanged;
		public event EventHandler UpdateRemainingTime;


		private readonly BoolWithFeedback enableMeetingReserve15 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingReserve30 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingReserve45 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingReserve60 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingReserve90 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingExtend15 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingExtend30 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingExtend45 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingExtend60 = new BoolWithFeedback();
		private readonly BoolWithFeedback enableMeetingExtend90 = new BoolWithFeedback();

		private readonly long schedulePullTimerTimeout = 300000;
		private readonly long schedulePushTimerTimeout = 90000;

		private DynFusionDevice _DynFusion;
		private CTimer schedulePullTimer = null;
		private CTimer schedulePushTimer;
		private CTimer UpdateRemainingTimeTimer = null;
		private CTimer getScheduleTimeOut = null;

		private readonly SchedulingConfig _Config;

		private RoomSchedule CurrentSchedule;
		public AvailableRooms AvailableRooms;

		private List<ScheduleResponse> RoomAvailabilityScheduleResponse = new List<ScheduleResponse>();

		private readonly BoolWithFeedback RegisterdForPush = new BoolWithFeedback();
		private readonly BoolWithFeedback ScheduleBusy = new BoolWithFeedback();


		public Event CurrentMeeting;
		private Event NextMeeting;
		private Event ThirdMeeting;
		private Event FourthMeeting;
		private Event FifthMeeting;
		private Event SixthMeeting;


		public DynFusionSchedule(string key, string name, SchedulingConfig config)
			: base(key, name)
		{
			try
			{
				_Config = config;
			}
			catch (Exception e)
			{
				this.LogError("Exception in DynFusionSchedule constructor: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}


		public override bool CustomActivate()
		{

			if (_Config.DynFusionKey != null)
			{
				_DynFusion = (DynFusionDevice)DeviceManager.GetDeviceForKey(_Config.DynFusionKey);

			}
			else
			{
				this.LogError("DynFusionDeviceKey is not present in config file");
				return false;
			}
			if (_DynFusion == null)
			{
				this.LogError("Error getting DynFusionDevice for key {key}", _Config.DynFusionKey);
				return false;
			}
			_DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.Use();
			_DynFusion.FusionSymbol.OnlineStatusChange += new OnlineStatusChangeEventHandler(FusionSymbolStatusChange);
			_DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(FusionScheduleExtenderSigChange);
			_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(FusionRoomDataExtenderSigChange);
			AvailableRooms = new AvailableRooms(_DynFusion);
			return true;
		}

		public void StartSchedPushTimer()
		{
			if (schedulePushTimer != null)
			{
				return;
			}
			schedulePushTimer = new CTimer(new CTimerCallbackFunction(GetRoomSchedule), null, schedulePushTimerTimeout, schedulePushTimerTimeout);
		}

		public void ResetSchedulePushTimer()
		{
			if (schedulePushTimer == null || schedulePushTimer.Disposed)
			{
				return;
			}
			schedulePushTimer.Reset(schedulePushTimerTimeout, schedulePushTimerTimeout);
		}
		public void StopSchedPushTimer()
		{
			if (schedulePushTimer == null || schedulePushTimer.Disposed)
			{
				return;
			}

			schedulePullTimer.Stop();
			schedulePullTimer.Dispose();
		}

		private void FusionSymbolStatusChange(object o, OnlineOfflineEventArgs e)
		{
			this.LogDebug("FusionSymbolStatusChange {0}", e.DeviceOnLine);
			fusionOnline = e.DeviceOnLine;
			if (!fusionOnline)
			{
				return;
			}

			GetPushSchedule();
			StartUpdateRemainingTimeTimer();
		}

		private void GetPushSchedule()
		{
			try
			{
				if (fusionOnline)
				{
					string requestID = "InitialPushRequest";
					string fusionActionRequest = "";

					fusionActionRequest = string.Format("<RequestAction>\n<RequestID>{0}</RequestID>\n" +
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
												"</RequestAction>\n", requestID);

					_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery.StringValue = fusionActionRequest;
				}
			}
			catch (Exception e)
			{
				this.LogError("Get Push Schedule Error: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}

		private void GetRoomSchedule(object unused)
		{
			GetRoomSchedule();
		}

		public void GetRoomSchedule()
		{
			if (ScheduleBusy.Value)
			{
				return;
			}

			ScheduleBusy.Value = true;
			getScheduleTimeOut = new CTimer(GetRoomScheduleTimeOut, 6000);
			string roomID = _DynFusion.RoomInformation.ID;
			string requestType = "ScheduleRequest";
			GetFullRoomSchedule(roomID, requestType.ToString());
		}

		public void GetRoomScheduleTimeOut(object unused)
		{
			ScheduleBusy.Value = false;
			this.LogError("Error getRoomScheduleTimeOut");
		}


		private void GetFullRoomSchedule(string roomID, string requestType)
		{
			try
			{
				if (fusionOnline)
				{
					string fusionScheduleRequest = "";
					string RFCTime;

					RFCTime = string.Format("{0:s}", DateTime.Now);

					fusionScheduleRequest = string.Format("<RequestSchedule><RequestID>{0}</RequestID><RoomID>{1}</RoomID><Start>{2}</Start><HourSpan>24</HourSpan></RequestSchedule>", requestType, roomID, RFCTime.ToString());

					_DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.ScheduleQuery.StringValue = fusionScheduleRequest;
				}
			}
			catch (Exception e)
			{
				this.LogError("Get Full Schedule Error: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}

		private void ExtendMeeting(int extendTimeIn)
		{
			try
			{
				if (!fusionOnline || CurrentMeeting == null)
				{
					return;
				}

				if (!CurrentMeeting.isInProgress)
				{
					this.LogVerbose("No Meeting in Progress");
					return;
				}

				string fusionExtendMeetingRequest = "";
				string extendTime;
				string meetingID = CurrentMeeting.MeetingID;

				if (extendTimeIn != 0)
				{
					TimeSpan timeToExtend = CurrentMeeting.dtEnd.Subtract(DateTime.Now);
					ushort totalMeetingExtend = (ushort)(timeToExtend.TotalMinutes + extendTimeIn);
					extendTime = totalMeetingExtend.ToString();
				}
				else
					extendTime = extendTimeIn.ToString();


				fusionExtendMeetingRequest = string.Format("<RequestAction><RequestID>ExtendMeetingRequest</RequestID><ActionID>MeetingChange</ActionID><Parameters>" +
									 "<Parameter ID=\"MeetingID\" Value=\"{0}\" /><Parameter ID=\"EndTime\" Value=\"{1}\" /></Parameters></RequestAction>", meetingID, extendTime.ToString());

				this.LogVerbose("ExtendRequest: {request}", fusionExtendMeetingRequest);
				_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQuery.StringValue = fusionExtendMeetingRequest;

			}
			catch (Exception e)
			{
				this.LogError("Exception extending meeting: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}

		private void CreateMeeting(ushort meetingTimeIn)
		{
			try
			{
				if (!fusionOnline)
				{
					return;
				}

				if (CurrentMeeting != null)
				{
					this.LogVerbose("Meeting In Progress");
					return;
				}

				string fusionCreateMeetingRequest = "";
				string meetingTime = meetingTimeIn.ToString();
				string startTime;
				string endTime;

				TimeSpan meetingLength = new TimeSpan(0, (int)meetingTimeIn, 0);

				startTime = string.Format("{0:s}", DateTime.Now);
				endTime = string.Format("{0:s}", DateTime.Now.Add(meetingLength));

				this.LogVerbose("Start Time: {0}, End Time: {1}", startTime, endTime);

				fusionCreateMeetingRequest = string.Format("<CreateSchedule><RequestID>ExtendMeetingRequest</RequestID>" +
															"<Event><dtStart>{0}</dtStart><dtEnd>{1}</dtEnd><Subject>Ad-Hoc Meeting Created</Subject><Organizer>{2}</Organizer>" +
															"<WelcomeMsg>Meeting Created></WelcomeMsg></Event></CreateSchedule>", startTime, endTime, _DynFusion.RoomInformation.Name.ToString());

				this.LogVerbose("Create Meeting Request: {0}", fusionCreateMeetingRequest);
				_DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateMeeting.StringValue = fusionCreateMeetingRequest;
			}
			catch (Exception e)
			{
				this.LogError("Exception creating meeting: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}

		private void FusionRoomDataExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{

			try
			{
				string result = Regex.Replace(args.Sig.StringValue, "&(?!(amp|apos|quot|lt|gt);)", "&amp;");

				if (args.Sig == _DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.ActionQueryResponse && args.Sig.StringValue != null)
				{

					XmlDocument actionResponseXML = new XmlDocument();
					actionResponseXML.LoadXml(result);

					var actionResponse = actionResponseXML["ActionResponse"];

					if (actionResponse != null)
					{
						var requestID = actionResponse["RequestID"];

						if (requestID.InnerText == "InitialPushRequest")
						{
							if (actionResponse["ActionID"].InnerText == "RegisterPushModel")
							{
								var parameters = actionResponse["Parameters"];

								foreach (XmlElement parameter in parameters)
								{
									if (parameter.HasAttributes)
									{
										var attributes = parameter.Attributes;

										if (attributes["ID"].Value == "Registered")
										{
											var isRegistered = int.Parse(attributes["Value"].Value.ToString());

											if (isRegistered == 1)
											{
												RegisterdForPush.Value = true;

												// JTA EXTRA Logging
												this.LogVerbose("SchedulePush: {value}", RegisterdForPush.Value);

												StartSchedPushTimer();
											}

											else if (isRegistered == 0)
											{
												RegisterdForPush.Value = false;
												// JTA EXTRA Logging
												this.LogVerbose("SchedulePush: {value}", RegisterdForPush.Value);
												StopSchedPushTimer();

												schedulePullTimer = new CTimer(GetRoomSchedule, null, schedulePullTimerTimeout, schedulePullTimerTimeout);
											}
										}
									}
								}
							}


						}

						if (requestID.InnerText == "ExtendMeetingRequest")
						{

							if (actionResponse["ActionID"].InnerText == "MeetingChange")
							{
								GetRoomSchedule(null);
								var parameters = actionResponse["Parameters"];

								foreach (XmlElement parameter in parameters)
								{
									if (parameter.HasAttributes)
									{
										var attributes = parameter.Attributes;

										if (attributes["ID"].Value == "MeetingID")
										{
											if (attributes["Value"].Value != null)
											{
												string value = attributes["Value"].Value;
											}
										}
										else if (attributes["ID"].Value == "InstanceID")
										{
											if (attributes["Value"].Value != null)
											{
												string value = attributes["Value"].Value;
											}
										}
										else if (attributes["ID"].Value == "Status")
										{
											if (attributes["Value"].Value != null)
											{
												string value = attributes["Value"].Value;
											}
										}
									}

								}
							}
						}
					}
				}
				if (args.Sig == _DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateResponse)
				{
					GetRoomSchedule();
				}
				else if (args.Sig == _DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.RemoveMeeting)
				{
					GetRoomSchedule();
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception getting schedule: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}



		}


		private void FusionScheduleExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{
			try
			{
				this.LogVerbose("FusionScheduleExtenderSigChange args {value}", args.Sig.StringValue);
				if (args.Sig == _DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.ScheduleResponse)
				{
					XmlDocument scheduleXML = new XmlDocument();

					scheduleXML = scheduleXML.CustomEscapeDocument(args.Sig.StringValue.ToString());

					if (scheduleXML != null)
					{

						this.LogVerbose("Escaped XML {0}", scheduleXML.ToString());

						var response = scheduleXML["ScheduleResponse"];
						var responseEvent = scheduleXML.FirstChild.SelectSingleNode("Event");

						if (response != null)
						{
							ResetSchedulePushTimer();

							if (response["RequestID"].InnerText == "RVRequest")
							{
								var action = response["Action"];

								if (action.OuterXml.IndexOf("RequestSchedule") > -1)
								{
									GetRoomSchedule();
								}
							}
							#region ScheduleRequest
							else if (response["RequestID"].InnerText == "ScheduleRequest")
							{

								CurrentSchedule = new RoomSchedule();

								CurrentMeeting = null;
								NextMeeting = null;
								ThirdMeeting = null;
								FourthMeeting = null;
								FifthMeeting = null;
								SixthMeeting = null;

								ScheduleResponse scheduleResponse = new ScheduleResponse
								{
									RoomName = scheduleXML.FirstChild.SelectSingleNode("RoomName").InnerText,
									RequestID = scheduleXML.FirstChild.SelectSingleNode("RequestID").InnerText,
									RoomID = scheduleXML.FirstChild.SelectSingleNode("RoomID").InnerText
								};

								var eventStack = scheduleXML.FirstChild.SelectNodes("Event");
								this.LogVerbose("EventStack Count: {count}", eventStack.Count);

								if (eventStack.Count > 0)
								{
									Event tempEvent = new Event();
									tempEvent = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(0).OuterXml));
									scheduleResponse.Events.Add(tempEvent);
									if (tempEvent.isInProgress)
									{
										CurrentMeeting = new Event();
										CurrentMeeting = tempEvent;
										if (eventStack.Count > 1) { NextMeeting = new Event(); NextMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(1).OuterXml)); }
										if (eventStack.Count > 2) { ThirdMeeting = new Event(); ThirdMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml)); }
										if (eventStack.Count > 3) { FourthMeeting = new Event(); FourthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml)); }
										if (eventStack.Count > 4) { FifthMeeting = new Event(); FifthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml)); }
										if (eventStack.Count > 5) { SixthMeeting = new Event(); SixthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(3).OuterXml)); }

										AvailableRooms.SendFreeBusyStatusNotAvailable();
									}
									else
									{
										NextMeeting = new Event(); NextMeeting = tempEvent;
										if (eventStack.Count > 1) { ThirdMeeting = new Event(); ThirdMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(1).OuterXml)); }
										if (eventStack.Count > 2) { FourthMeeting = new Event(); FourthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml)); }
										if (eventStack.Count > 3) { FifthMeeting = new Event(); FifthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml)); }
										if (eventStack.Count > 4) { SixthMeeting = new Event(); SixthMeeting = CrestronXMLSerialization.DeSerializeObject<Event>(new XmlReader(eventStack.Item(2).OuterXml)); }
										AvailableRooms.SendFreeBusyStatusAvailableUntil(NextMeeting.dtStart);
									}
								}
								else
								{
									AvailableRooms.SendFreeBusyStatusAvailable();
								}
								if (CurrentMeeting != null) { this.LogVerbose("Current Meeting {subject}", CurrentMeeting.Subject); }
								if (NextMeeting != null) { this.LogVerbose("Next Meeting {subject}", NextMeeting.Subject); }
								if (ThirdMeeting != null) { this.LogVerbose("Later Meeting {subject}", ThirdMeeting.Subject); }
								if (FourthMeeting != null) { this.LogVerbose("Latest Meeting {subject}", FourthMeeting.Subject); }
								if (FifthMeeting != null) { this.LogVerbose("Fifth Meeting {subject}", FifthMeeting.Subject); }
								if (SixthMeeting != null) { this.LogVerbose("Sixth Meeting {subject}", SixthMeeting.Subject); }

								getScheduleTimeOut.Stop();
								var handler = ScheduleChanged;
								if (handler != null)
								{
									this.LogVerbose("Schedule Changed Firing Event!");
									handler(this, new DynFusionScheduleChangeEventArgs(string.Empty));
								}

								ScheduleBusy.Value = false;
							}
							#endregion
							else if (response["RequestID"].InnerText == "PushNotification")
							{
								GetRoomSchedule(null);
								this.LogVerbose("Got a Push Notification!");

							}
							#region RoomListScheduleRequest
							else if (response["RequestID"].InnerText == "AvailableRoomSchedule")
							{
								if (responseEvent != null)
								{
									RoomAvailabilityScheduleResponse = null;

									foreach (XmlElement element in scheduleXML.FirstChild.ChildNodes)
									{
										ScheduleResponse AvailibleSchedule = new ScheduleResponse();

										if (element.Name == "RequestID")
										{
											AvailibleSchedule.RequestID = element.InnerText;
										}
										else if (element.Name == "RoomID")
										{
											AvailibleSchedule.RoomID = element.InnerText;
										}
										else if (element.Name == "RoomName")
										{
											AvailibleSchedule.RoomName = element.InnerText;
										}
										else if (element.Name == "Event")
										{
											XmlReader readerXML = new XmlReader(element.OuterXml);

											Event RoomAvailabilityScheduleEvent = new Event();

											RoomAvailabilityScheduleEvent = CrestronXMLSerialization.DeSerializeObject<Event>(readerXML);

											AvailibleSchedule.Events.Add(RoomAvailabilityScheduleEvent);

										}

										RoomAvailabilityScheduleResponse.Add(AvailibleSchedule);
									}
								}
							}
							#endregion
						}
					}
				}
				if (args.Sig == _DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.CreateResponse)
				{
					GetRoomSchedule();
				}
				else if (args.Sig == _DynFusion.FusionSymbol.ExtenderRoomViewSchedulingDataReservedSigs.RemoveMeeting)
				{
					GetRoomSchedule();
				}
			}

			catch (Exception e)
			{
				this.LogError("FusionScheduleExtenderSigChange Exception: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}
		}

		private void StartUpdateRemainingTimeTimer()
		{
			UpdateRemainingTimeTimer = new CTimer(UpdateRemainingTimeHandler, null, Crestron.SimplSharp.Timeout.Infinite, 60000);
		}

		private void UpdateRemainingTimeHandler(object o)
		{
			UpdateRemainingTime?.Invoke(this, new EventArgs());
		}

		private void CheckMeetingExtend()
		{
			try
			{
				if (CurrentMeeting != null)
				{
					if (NextMeeting != null)
					{
						TimeSpan timeToNext = NextMeeting.dtStart.Subtract(CurrentMeeting.dtEnd);

						if (timeToNext.TotalMinutes >= 90)
						{
							enableMeetingExtend15.Value = true;
							enableMeetingExtend30.Value = true;
							enableMeetingExtend45.Value = true;
							enableMeetingExtend60.Value = true;
							enableMeetingExtend90.Value = true;
						}
						else if (timeToNext.TotalMinutes >= 60)
						{
							enableMeetingExtend15.Value = true;
							enableMeetingExtend30.Value = true;
							enableMeetingExtend45.Value = true;
							enableMeetingExtend60.Value = true;
							enableMeetingExtend90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 45)
						{
							enableMeetingExtend15.Value = true;
							enableMeetingExtend30.Value = true;
							enableMeetingExtend45.Value = true;
							enableMeetingExtend60.Value = false;
							enableMeetingExtend90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 30)
						{
							enableMeetingExtend15.Value = true;
							enableMeetingExtend30.Value = true;
							enableMeetingExtend45.Value = false;
							enableMeetingExtend60.Value = false;
							enableMeetingExtend90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 15)
						{
							enableMeetingExtend15.Value = true;
							enableMeetingExtend30.Value = false;
							enableMeetingExtend45.Value = false;
							enableMeetingExtend60.Value = false;
							enableMeetingExtend90.Value = false;
						}
						else
						{
							enableMeetingExtend15.Value = false;
							enableMeetingExtend30.Value = false;
							enableMeetingExtend45.Value = false;
							enableMeetingExtend60.Value = false;
							enableMeetingExtend90.Value = false;
						}
					}
					else
					{
						enableMeetingExtend15.Value = true;
						enableMeetingExtend30.Value = true;
						enableMeetingExtend45.Value = true;
						enableMeetingExtend60.Value = true;
						enableMeetingExtend90.Value = true;
					}
				}
				else
				{
					enableMeetingExtend15.Value = false;
					enableMeetingExtend30.Value = false;
					enableMeetingExtend45.Value = false;
					enableMeetingExtend60.Value = false;
					enableMeetingExtend90.Value = false;
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in CheckMeetingExtend: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}



		}

		private void CheckMeetingReserve()
		{
			try
			{
				if (CurrentMeeting == null)
				{
					if (NextMeeting != null)
					{
						TimeSpan timeToNext = NextMeeting.dtStart.Subtract(DateTime.Now);

						if (timeToNext.TotalMinutes >= 90)
						{
							enableMeetingReserve15.Value = true;
							enableMeetingReserve30.Value = true;
							enableMeetingReserve45.Value = true;
							enableMeetingReserve60.Value = true;
							enableMeetingReserve90.Value = true;
						}
						else if (timeToNext.TotalMinutes >= 60)
						{
							enableMeetingReserve15.Value = true;
							enableMeetingReserve30.Value = true;
							enableMeetingReserve45.Value = true;
							enableMeetingReserve60.Value = true;
							enableMeetingReserve90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 45)
						{
							enableMeetingReserve15.Value = true;
							enableMeetingReserve30.Value = true;
							enableMeetingReserve45.Value = true;
							enableMeetingReserve60.Value = false;
							enableMeetingReserve90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 30)
						{
							enableMeetingReserve15.Value = true;
							enableMeetingReserve30.Value = true;
							enableMeetingReserve45.Value = false;
							enableMeetingReserve60.Value = false;
							enableMeetingReserve90.Value = false;
						}
						else if (timeToNext.TotalMinutes >= 15)
						{
							enableMeetingReserve15.Value = true;
							enableMeetingReserve30.Value = false;
							enableMeetingReserve45.Value = false;
							enableMeetingReserve60.Value = false;
							enableMeetingReserve90.Value = false;
						}
						else
						{
							enableMeetingReserve15.Value = false;
							enableMeetingReserve30.Value = false;
							enableMeetingReserve45.Value = false;
							enableMeetingReserve60.Value = false;
							enableMeetingReserve90.Value = false;
						}
					}
					else
					{
						enableMeetingReserve15.Value = true;
						enableMeetingReserve30.Value = true;
						enableMeetingReserve45.Value = true;
						enableMeetingReserve60.Value = true;
						enableMeetingReserve90.Value = true;
					}
				}
				else
				{
					enableMeetingReserve15.Value = false;
					enableMeetingReserve30.Value = false;
					enableMeetingReserve45.Value = false;
					enableMeetingReserve60.Value = false;
					enableMeetingReserve90.Value = false;
				}
			}
			catch (Exception e)
			{
				this.LogError("Exception in CheckMeetingReserve: {message}", e.Message);
				this.LogDebug(e, "Stack Trace: ");
			}


		}

		public override void LinkToApi(Crestron.SimplSharpPro.DeviceSupport.BasicTriList trilist, uint joinStart, string joinMapKey, PepperDash.Essentials.Core.Bridges.EiscApiAdvanced bridge)
		{
			try
			{

				var joinMap = new SchedulingJoinMap(joinStart);
				ScheduleBusy.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ScheduleBusy.JoinNumber]);



				trilist.SetSigTrueAction(joinMap.GetSchedule.JoinNumber, () => GetRoomSchedule());
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

				RegisterdForPush.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.PushNotificationRegistered.JoinNumber]);
				enableMeetingExtend15.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting15Minutes.JoinNumber]);
				enableMeetingExtend30.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting30Minutes.JoinNumber]);
				enableMeetingExtend45.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting45Minutes.JoinNumber]);
				enableMeetingExtend60.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting60Minutes.JoinNumber]);
				enableMeetingExtend90.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ExtendMeeting90Minutes.JoinNumber]);
				enableMeetingReserve15.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting15Minutes.JoinNumber]);
				enableMeetingReserve30.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting30Minutes.JoinNumber]);
				enableMeetingReserve45.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting45Minutes.JoinNumber]);
				enableMeetingReserve60.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting60Minutes.JoinNumber]);
				enableMeetingReserve90.Feedback.LinkInputSig(trilist.BooleanInput[joinMap.ReserveMeeting90Minutes.JoinNumber]);



				UpdateRemainingTime += new EventHandler((s, e) =>
				{
					CheckMeetingExtend();
					CheckMeetingReserve();
					if (CurrentMeeting != null && !CurrentMeeting.isInProgress) { GetRoomSchedule(); }
					if (NextMeeting != null && NextMeeting.isInProgress) { GetRoomSchedule(); }

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
					if (NextMeeting != null)
					{
						trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = NextMeeting.TimeRemainingString;
						trilist.UShortInput[joinMap.NextMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(NextMeeting.TimeRemainingInMin);

						DateTime date = DateTime.Parse(NextMeeting.StartDate.ToString());
						if (date.Date == DateTime.Today.Date)
							trilist.BooleanInput[joinMap.NextMeetingIsToday.JoinNumber].BoolValue = true;
						else
							trilist.BooleanInput[joinMap.NextMeetingIsToday.JoinNumber].BoolValue = false;

					}
					if (ThirdMeeting != null)
					{
						trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = ThirdMeeting.TimeRemainingString;
						trilist.UShortInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(ThirdMeeting.TimeRemainingInMin);
					}
					if (FourthMeeting != null)
					{
						trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = FourthMeeting.TimeRemainingString;
						trilist.UShortInput[joinMap.FourthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(FourthMeeting.TimeRemainingInMin);
					}
					if (FifthMeeting != null)
					{
						trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = FifthMeeting.TimeRemainingString;
						trilist.UShortInput[joinMap.FifthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(FifthMeeting.TimeRemainingInMin);
					}
					if (SixthMeeting != null)
					{
						trilist.StringInput[joinMap.SixthMeetingRemainingTime.JoinNumber].StringValue = SixthMeeting.TimeRemainingString;
						trilist.UShortInput[joinMap.SixthMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(SixthMeeting.TimeRemainingInMin);
					}
				});

				ScheduleChanged += ((s, e) =>
				{
					try
					{
						if (CurrentMeeting != null)
						{
							trilist.StringInput[joinMap.CurrentMeetingOrganizer.JoinNumber].StringValue = CurrentMeeting.Organizer;
							trilist.StringInput[joinMap.CurrentMeetingSubject.JoinNumber].StringValue = CurrentMeeting.Subject;
							trilist.StringInput[joinMap.CurrentMeetingMeetingID.JoinNumber].StringValue = CurrentMeeting.MeetingID;
							trilist.StringInput[joinMap.CurrentMeetingStartTime.JoinNumber].StringValue = CurrentMeeting.StartTime;
							trilist.StringInput[joinMap.CurrentMeetingStartDate.JoinNumber].StringValue = CurrentMeeting.StartDate;
							trilist.StringInput[joinMap.CurrentMeetingEndTime.JoinNumber].StringValue = CurrentMeeting.EndTime;
							trilist.StringInput[joinMap.CurrentMeetingEndDate.JoinNumber].StringValue = CurrentMeeting.EndDate;
							trilist.StringInput[joinMap.CurrentMeetingDuration.JoinNumber].StringValue = CurrentMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = CurrentMeeting.TimeRemainingString;
							trilist.UShortInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].UShortValue = Convert.ToUInt16(CurrentMeeting.TimeRemainingInMin);
							trilist.BooleanInput[joinMap.MeetingInProgress.JoinNumber].BoolValue = CurrentMeeting.isInProgress;
						}
						else
						{
							trilist.StringInput[joinMap.CurrentMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingMeetingID.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.CurrentMeetingRemainingTime.JoinNumber].StringValue = "";
							trilist.BooleanInput[joinMap.MeetingInProgress.JoinNumber].BoolValue = false;
						}

						if (NextMeeting != null)
						{
							trilist.StringInput[joinMap.NextMeetingOrganizer.JoinNumber].StringValue = NextMeeting.Organizer;
							trilist.StringInput[joinMap.NextMeetingSubject.JoinNumber].StringValue = NextMeeting.Subject;
							trilist.StringInput[joinMap.NextMeetingMeetingID.JoinNumber].StringValue = NextMeeting.MeetingID;
							trilist.StringInput[joinMap.NextMeetingStartTime.JoinNumber].StringValue = NextMeeting.StartTime;
							trilist.StringInput[joinMap.NextMeetingStartDate.JoinNumber].StringValue = NextMeeting.StartDate;
							trilist.StringInput[joinMap.NextMeetingEndTime.JoinNumber].StringValue = NextMeeting.EndTime;
							trilist.StringInput[joinMap.NextMeetingEndDate.JoinNumber].StringValue = NextMeeting.EndDate;
							trilist.StringInput[joinMap.NextMeetingDuration.JoinNumber].StringValue = NextMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = NextMeeting.TimeRemainingString;


						}
						else
						{
							trilist.StringInput[joinMap.NextMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingMeetingID.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.NextMeetingRemainingTime.JoinNumber].StringValue = "";
							trilist.BooleanInput[joinMap.NextMeetingIsToday.JoinNumber].BoolValue = false;
						}

						if (ThirdMeeting != null)
						{
							trilist.StringInput[joinMap.ThirdMeetingOrganizer.JoinNumber].StringValue = ThirdMeeting.Organizer;
							trilist.StringInput[joinMap.ThirdMeetingSubject.JoinNumber].StringValue = ThirdMeeting.Subject;
							trilist.StringInput[joinMap.ThirdMeetingMeetingID.JoinNumber].StringValue = ThirdMeeting.MeetingID;
							trilist.StringInput[joinMap.ThirdMeetingStartTime.JoinNumber].StringValue = ThirdMeeting.StartTime;
							trilist.StringInput[joinMap.ThirdMeetingStartDate.JoinNumber].StringValue = ThirdMeeting.StartDate;
							trilist.StringInput[joinMap.ThirdMeetingEndTime.JoinNumber].StringValue = ThirdMeeting.EndTime;
							trilist.StringInput[joinMap.ThirdMeetingEndDate.JoinNumber].StringValue = ThirdMeeting.EndDate;
							trilist.StringInput[joinMap.ThirdMeetingDuration.JoinNumber].StringValue = ThirdMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = ThirdMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.ThirdMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingMeetingID.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.ThirdMeetingRemainingTime.JoinNumber].StringValue = "";
						}

						if (FourthMeeting != null)
						{
							trilist.StringInput[joinMap.FourthMeetingOrganizer.JoinNumber].StringValue = FourthMeeting.Organizer;
							trilist.StringInput[joinMap.FourthMeetingSubject.JoinNumber].StringValue = FourthMeeting.Subject;
							trilist.StringInput[joinMap.FourthMeetingMeetingID.JoinNumber].StringValue = FourthMeeting.MeetingID;
							trilist.StringInput[joinMap.FourthMeetingStartTime.JoinNumber].StringValue = FourthMeeting.StartTime;
							trilist.StringInput[joinMap.FourthMeetingStartDate.JoinNumber].StringValue = FourthMeeting.StartDate;
							trilist.StringInput[joinMap.FourthMeetingEndTime.JoinNumber].StringValue = FourthMeeting.EndTime;
							trilist.StringInput[joinMap.FourthMeetingEndDate.JoinNumber].StringValue = FourthMeeting.EndDate;
							trilist.StringInput[joinMap.FourthMeetingDuration.JoinNumber].StringValue = FourthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = FourthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.FourthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingMeetingID.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FourthMeetingRemainingTime.JoinNumber].StringValue = "";
						}
						if (FifthMeeting != null)
						{
							trilist.StringInput[joinMap.FifthMeetingOrganizer.JoinNumber].StringValue = FifthMeeting.Organizer;
							trilist.StringInput[joinMap.FifthMeetingSubject.JoinNumber].StringValue = FifthMeeting.Subject;
							trilist.StringInput[joinMap.FifthMeetingMeetingID.JoinNumber].StringValue = FifthMeeting.MeetingID;
							trilist.StringInput[joinMap.FifthMeetingStartTime.JoinNumber].StringValue = FifthMeeting.StartTime;
							trilist.StringInput[joinMap.FifthMeetingStartDate.JoinNumber].StringValue = FifthMeeting.StartDate;
							trilist.StringInput[joinMap.FifthMeetingEndTime.JoinNumber].StringValue = FifthMeeting.EndTime;
							trilist.StringInput[joinMap.FifthMeetingEndDate.JoinNumber].StringValue = FifthMeeting.EndDate;
							trilist.StringInput[joinMap.FifthMeetingDuration.JoinNumber].StringValue = FifthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = FifthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.FifthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingMeetingID.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingStartTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingStartDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingEndTime.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingEndDate.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingDuration.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.FifthMeetingRemainingTime.JoinNumber].StringValue = "";
						}
						if (SixthMeeting != null)
						{
							trilist.StringInput[joinMap.SixthMeetingOrganizer.JoinNumber].StringValue = SixthMeeting.Organizer;
							trilist.StringInput[joinMap.SixthMeetingSubject.JoinNumber].StringValue = SixthMeeting.Subject;
							trilist.StringInput[joinMap.SixthMeetingMeetingID.JoinNumber].StringValue = SixthMeeting.MeetingID;
							trilist.StringInput[joinMap.SixthMeetingStartTime.JoinNumber].StringValue = SixthMeeting.StartTime;
							trilist.StringInput[joinMap.SixthMeetingStartDate.JoinNumber].StringValue = SixthMeeting.StartDate;
							trilist.StringInput[joinMap.SixthMeetingEndTime.JoinNumber].StringValue = SixthMeeting.EndTime;
							trilist.StringInput[joinMap.SixthMeetingEndDate.JoinNumber].StringValue = SixthMeeting.EndDate;
							trilist.StringInput[joinMap.SixthMeetingDuration.JoinNumber].StringValue = SixthMeeting.DurationInMinutes;
							trilist.StringInput[joinMap.SixthMeetingRemainingTime.JoinNumber].StringValue = SixthMeeting.TimeRemainingString;
						}
						else
						{
							trilist.StringInput[joinMap.SixthMeetingOrganizer.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingSubject.JoinNumber].StringValue = "";
							trilist.StringInput[joinMap.SixthMeetingMeetingID.JoinNumber].StringValue = "";
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
						this.LogError("LinkToApi ScheduleChanged: {message}", ex.Message);
						this.LogDebug(ex, "Stack Trace: ");
					}
				});

			}
			catch (Exception ex)
			{
				this.LogError("LinkToApi: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
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
		public string RequestID { get; set; }
	}

	public class RequestSchedule
	{
		public string RequestID { get; set; }
		public string RoomID { get; set; }
		public DateTime Start { get; set; }
		public double HourSpan { get; set; }

		public RequestSchedule(string requestID, string roomID)
		{
			RequestID = requestID;
			RoomID = roomID;
			Start = DateTime.Now;
			HourSpan = 24;
		}
	}

	public class RequestAction
	{
		public string RequestID { get; set; }
		public string RoomID { get; set; }
		public string ActionID { get; set; }
		public List<Parameter> Parameters { get; set; }

		public RequestAction(string roomID, string actionID, List<Parameter> parameters)
		{
			RoomID = roomID;
			ActionID = actionID;
			Parameters = parameters;
		}
	}

	public class ActionResponse
	{
		public string RequsetID { get; set; }
		public string ActionID { get; set; }
		public List<Parameter> Parameters { get; set; }
	}

	public class Parameter
	{
		public string ID { get; set; }
		public string Value { get; set; }
	}

	public class ScheduleResponse
	{
		public string RequestID { get; set; }
		public string RoomID { get; set; }
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
		public string MeetingID { get; set; }
		public string RVMeetingID { get; set; }
		public DateTime dtStart { get; set; }
		public DateTime dtEnd { get; set; }
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
		public string InstanceID { get; set; }

		public Event()
		{

		}

		public string StartTime
		{
			get
			{
				string startTimeShort;

				startTimeShort = dtStart.ToShortTimeString();

				return startTimeShort;

			}
		}

		public string StartDate
		{
			get
			{
				string startDateShort;

				startDateShort = dtStart.ToShortDateString();

				return startDateShort;

			}
		}

		public string EndTime
		{
			get
			{
				string endTimeShort;

				endTimeShort = dtEnd.ToShortTimeString();

				return endTimeShort;

			}
		}

		public string EndDate
		{
			get
			{
				string endDateShort;

				endDateShort = dtEnd.ToShortDateString();

				return endDateShort;

			}
		}

		public string DurationInMinutes
		{
			get
			{
				string duration;

				var timeSpan = dtEnd.Subtract(dtStart);
				int hours = timeSpan.Hours;
				double minutes = timeSpan.Minutes;
				double minutesRounded = Math.Round(minutes);
				if (hours > 0)
				{
					duration = string.Format("{0} Hours {1} Minutes", hours, minutesRounded);
				}
				else
				{
					duration = string.Format("{0} Minutes", minutesRounded);
				}

				return duration;
			}
		}
		public double TimeRemainingInMin
		{

			get
			{
				DateTime timeMarker = new DateTime();
				if (dtStart <= DateTime.Now) { timeMarker = dtEnd; }
				else { timeMarker = dtStart; }

				double totalMinutes = timeMarker.Subtract(DateTime.Now).TotalMinutes;
				if (totalMinutes >= 0)
					return Math.Round(totalMinutes);
				else
					return 0;
			}
		}
		public string TimeRemainingString
		{
			get
			{
				var now = DateTime.Now;
				string remainingTimeString;

				DateTime timeMarker = new DateTime();
				if (GetInProgress()) { timeMarker = dtEnd; }
				else { timeMarker = dtStart; }

				var hourTag = "";
				var minTag = "";
				int hours = timeMarker.Subtract(DateTime.Now).Hours;
				int minutes = timeMarker.Subtract(DateTime.Now).Minutes;
				if (hours > 1) { hourTag = "Hours"; }
				else if (hours == 1) { hourTag = "Hour"; }
				if (minutes == 1) { minTag = "Minute"; }
				else { minTag = "Minutes"; }

				if (hourTag.Length == 0) { remainingTimeString = string.Format("{0} {1}", minutes, minTag); }
				else { remainingTimeString = string.Format("{0} {1} {2} {3}", hours, hourTag, minutes, minTag); }

				return remainingTimeString;

			}
		}

		public bool isInProgress
		{
			get
			{
				return GetInProgress();
			}
		}

		private bool GetInProgress()
		{
			var now = DateTime.Now;

			if (now > dtStart && now < dtEnd)
			{
				return true;
			}
			else
			{
				return false;
			}
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
		public string ID { get; set; }
		public string Value { get; set; }
	}

	public class MeetingTypes
	{
		public List<MeetingType> MeetingType { get; set; }
	}

	public class LiveMeeting
	{
		public string URL { get; set; }
		public string ID { get; set; }
		public string Key { get; set; }
		public string Subject { get; set; }
	}

	public class LiveMeetingURL
	{
		public LiveMeeting LiveMeeting { get; set; }
	}
}