﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXml.Serialization;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharpPro.Fusion;
using Crestron.SimplSharpPro;



using PepperDash.Core;

namespace DynFusion
{

	public class Room
	{
		public string RoomID { get; set; }
		public string RoomName { get; set; }
		public string Location { get; set; }
		public bool OnlineStatus { get; set; }
		public double availableMinutes
		{
			get
			{
				double returnValue;
				if (this.FreeBusyStatus.Contains("T"))
				{
					DateTime tempTime = DateTime.Parse(FreeBusyStatus);
					//CrestronConsole.PrintLine(tempTime.ToString());
					if (tempTime > DateTime.Now)
					{
						returnValue = Math.Round(tempTime.Subtract(DateTime.Now).TotalMinutes);
					}
					else { returnValue = 0; }
				}

				else { returnValue = 0; }

				return returnValue;

			}
		}
		public string FreeBusyStatus { get; set; }

	}


	public class DynFusionScheduleAvailableRooms
	{
		public bool includeInAvailbleRooms;
		private CTimer getAvailableRoomsTimeOut;
		private bool _availableRoomStatus;
		public bool AvailableRoomStatus
		{
			set
			{
				this._availableRoomStatus = value;
				OnAvailableRoomsBusy(this, _availableRoomStatus);
				if (_availableRoomStatus) { getAvailableRoomsTimeOut = new CTimer(getAvailableRoomsTimeout, 6000); }
				else { getAvailableRoomsTimeOut.Stop(); }
			}
		}
		// I would love to poll and have data ready to go but we think it may be too heavy a server load. JTA 2017-12-06
		// CTimer updateAvailableRooms = null;
		private DynFusionDevice _DynFusion;
		public List<Room> RoomList = new List<Room>();
		public List<string> FilterList = new List<string>();
		public delegate void AvailableRoomUpdate(object sender, List<Room> RoomList);
		public event AvailableRoomUpdate OnAvailableRoomsUpdate;
		public delegate void AvailableRoomsBusy(object sender, bool busyStatus);
		public event AvailableRoomsBusy OnAvailableRoomsBusy;

		public DynFusionScheduleAvailableRooms(DynFusionDevice DynFusionInstance)
		{
			_DynFusion = DynFusionInstance;
			_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += new DeviceExtenderJoinChangeEventHandler(FusionRoomDataExtenderSigChange);
			// I would love to poll and have data ready to go but we think it may be too heavy a server load. JTA 2017-12-06
			// updateAvailableRooms = new CTimer(GetRoomList, null, 300000, 300000);
		}



		private void getAvailableRoomsTimeout(object unused)
		{
			Debug.ConsoleWithLog(2, "Error getAvailableRoomsTimeout", 3);
			this.AvailableRoomStatus = false;
		}
		public void sendFreeBusyStatusAvailableUntil(DateTime AvailableUntilTime)
		{
			// "2017-12-09T00:00:00"
			_DynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("{0}", AvailableUntilTime.ToString("yyyy-MM-ddTHH:mm:00"));
			Debug.Console(2, string.Format("Sending FreeBusyStatus {0}", AvailableUntilTime.ToString("yyyy-MM-ddTHH:mm:00")));
		}
		public void sendFreeBusyStatusAvailable()
		{
			_DynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("{0}", DateTime.Now.AddDays(5).ToString("yyyy-MM-ddT00:00:00"));
			Debug.Console(2, "Sending FreeBusyStatus {0}", DateTime.Now.AddDays(5).ToString("yyyy-MM-ddT00:00:00"));
		}
		public void sendFreeBusyStatusNotAvailable()
		{
			_DynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("-");
			Debug.Console(2, string.Format("-"));
		}
		private void GetRoomList(object unused)
		{
			GetRoomList();
		}
		public void GetRoomList()
		{
			try
			{
				if (_DynFusion.FusionSymbol.IsOnline)
				{
					this.AvailableRoomStatus = true;

					string fusionRoomListRequest = "";
					//TODO This needs to be implemented 
				//	fusionRoomListRequest = String.Format("<RequestRoomList><RequestID>RoomListRequest</RequestID><Property>Location</Property><Value>{0}</Value></RequestRoomList>", _DynFusion.FusionRoomInfo.roomInformation.Location);
					Debug.Console(2, String.Format("RoomList Request: {0}", fusionRoomListRequest));

					_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomListQuery.StringValue = fusionRoomListRequest;
				}
			}
			catch (Exception e)
			{
				Debug.Console(2, String.Format("Error Requesting Room List: {0}", e.ToString()));
			}
		}
		public void getAvailableRooms()
		{
			try
			{
				if (_DynFusion.FusionSymbol.IsOnline)
				{
					string messageHeader = String.Format("<RequestRoomAttributeList><RequestID>RoomAvailabilityRequest:{0}</RequestID>", Guid.NewGuid().ToString());
					string messageBody = "";
					foreach (Room r in RoomList)
					{
						messageBody = String.Format("{0}<Room><RoomID>{1}</RoomID><Read><Attributes><Attribute><Join>a0</Join></Attribute><Attribute><Join>s23</Join></Attribute></Attributes></Read></Room>", messageBody, r.RoomID);
					}
					string messageFooter = String.Format("</RequestRoomAttributeList>");
					_DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomAttributeQuery.StringValue = string.Format("{0}{1}{2}", messageHeader, messageBody, messageFooter);
					Debug.Console(2, String.Format("RequestRoomAttributeList {0}{1}{2}", messageHeader, messageBody, messageFooter));
					/* 
					 * if (_DynFusion.FusionSchedule.isRegisteredForSchedulePushNotifications) {
						schedulePushTimer.Stop();
						}
					 */
				}
			}
			catch (Exception ex)
			{
				Debug.Console(2, String.Format("getAvailableRooms Error: {0}", ex.Message));
				Debug.ConsoleWithLog(2, ex.ToString());
			}
		}
		void FusionRoomDataExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
		{
			#region Attribute Query Response Example
			/*
			<RoomAttributeListResponse>
			<RequestID>RoomAvailabilityRequest:41d0c78e-a1c9-40e7-b028-c4502af2fe0a</RequestID>
			<Room>
				<RoomID>56a0fc1c-ef0d-408d-a23e-702abe87dcf8</RoomID>
				<Attribute>
					<Name>Online Status</Name>
					<Join>a0</Join>
					<Value>2</Value>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
				<Attribute>
					<Name>Free Busy Status</Name>
					<Join>s23</Join>
					<Value>2017-12-11T00:00:00</Value>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
			</Room>
			<Room>
				<RoomID>7e9a5a9a-e1dc-4214-900d-046c3c8742ee</RoomID>
				<Attribute>
					<Name>Online Status</Name>
					<Join>a0</Join>
					<Value>2</Value>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
				<Attribute>
					<Name>Free Busy Status</Name>
					<Join>s23</Join>
					<Value>2017-12-10T00:00:00</Value>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
			</Room>
			<Room>
				<RoomID>8f198937-c405-4b29-bf69-104dd732fb66</RoomID>
				<Attribute>
					<Name>Online Status</Name>
					<Join>a0</Join>
					<Value>0</Value>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
				<Attribute>
					<Name>Free Busy Status</Name>
					<Join>s23</Join>
					<Value/>
					<IOMask>Read</IOMask>
					<Error/>
				</Attribute>
			</Room>
		</RoomAttributeListResponse>
			 */
			#endregion
			#region Attribute Query Response
			if (args.Sig == _DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomAttributeResponse)
			{
				Debug.Console(2, String.Format("RoomAttributeResponse Args: {0}", args.Sig.StringValue));
				XmlDocument availableRoomResponseXML = new XmlDocument();
				availableRoomResponseXML.LoadXml(args.Sig.StringValue);

				var availableRoomResponse = availableRoomResponseXML["RoomAttributeListResponse"];
				Debug.Console(2, String.Format("RequestID Args: {0}", availableRoomResponse));
				if (availableRoomResponse != null)
				{
					// It may be more efficent to get rid of this foreach JTA 2017-12-06
					foreach (XmlElement responseElement in availableRoomResponse)
					{
						Debug.Console(2, String.Format("RoomAttributeListResponseElement: {0} Value: {1}", responseElement.Name, responseElement.InnerXml));
						if (responseElement.Name == "Room")
						{
							XmlNodeList roomIDXml = responseElement.GetElementsByTagName("RoomID");
							Debug.Console(2, String.Format("RoomID: {0}", roomIDXml.Item(0).InnerXml));
							int index = RoomList.FindIndex(x => x.RoomID == roomIDXml.Item(0).InnerXml);

							XmlNodeList roomAttributes = responseElement.GetElementsByTagName("Attribute");

							foreach (XmlElement attributeElement in roomAttributes)
							{
								Debug.Console(2, String.Format("RoomAttributeListResponseRoomElement: {0} Value: {1}", attributeElement.Name, attributeElement.InnerXml));
								string attributeType = attributeElement.GetElementsByTagName("Name").Item(0).InnerXml;
								string attributeValue = attributeElement.GetElementsByTagName("Value").Item(0).InnerXml;
								Debug.Console(2, String.Format("RoomAttributeListResponseRoomAttribute Type: {0} Value: {1}", attributeType, attributeValue));
								switch (attributeType)
								{
									case ("Online Status"): { if (attributeValue == "2") { RoomList[index].OnlineStatus = true; } else { RoomList[index].OnlineStatus = false; } break; }
									case ("Free Busy Status"): { RoomList[index].FreeBusyStatus = attributeValue; break; }
								}
							}
						}
					}
				}
				OnAvailableRoomsUpdate(this, RoomList);
				this.AvailableRoomStatus = false;
			}



			#endregion

			#region Room List Response Example
			/*
			
			 * 
			 * <RoomListResponse>
				<RequestID>RoomListRequest</RequestID>
				<Room>
					<RoomID>56a0fc1c-ef0d-408d-a23e-702abe87dcf8</RoomID>
					<RoomName>IMF.HQ2.SLN200.Test (Delete Post 01/01/18)</RoomName>
					<Location>RoomLoc</Location>
					<OnlineStatus>Fully_Connected</OnlineStatus>
				</Room>
				<Room>
					<RoomID>7e9a5a9a-e1dc-4214-900d-046c3c8742ee</RoomID>
					<RoomName>IMF.HQ2.SLN200.TestRoom03 (Delete Post 01/01/18)</RoomName>
					<Location>RoomLoc</Location>
					<OnlineStatus>Fully_Connected</OnlineStatus>
				</Room>
				<Room>
					<RoomID>8f198937-c405-4b29-bf69-104dd732fb66</RoomID>
					<RoomName>IMF.HQ2.SLN200.TestRoom2 (Delete Post 01/01/18)</RoomName>
					<Location>RoomLoc</Location>
					<OnlineStatus>Not_Connected</OnlineStatus>
				</Room>
			</RoomListResponse>
			 * 
			 * 
			 * 
			 * 
			 * 
			 */


			#endregion
			#region Room List Response
			if (args.Sig == _DynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomListResponse)
			{
				Debug.Console(2, String.Format("RoomList Response: {0}", args.Sig.StringValue));

				try
				{
					XmlDocument roomListResponseXML = new XmlDocument();

					roomListResponseXML.LoadXml(args.Sig.StringValue);

					var roomListResponse = roomListResponseXML["RoomListResponse"];

					if (roomListResponse != null)
					{

						var requestID = roomListResponse["RequestID"];
						RoomList.Clear();
						if (requestID.InnerText == "RoomListRequest")
						{
							XmlNodeList rooms = roomListResponse.GetElementsByTagName("Room");
							foreach (XmlElement element in rooms)
							{
								Room roomToAdd = new Room();
								roomToAdd.RoomName = element.GetElementsByTagName("RoomName").Item(0).InnerXml;
								roomToAdd.RoomID = element.GetElementsByTagName("RoomID").Item(0).InnerXml;
								roomToAdd.Location = element.GetElementsByTagName("Location").Item(0).InnerXml;
								RoomList.Add(roomToAdd);
								Debug.Console(2, String.Format("RoomAdded Name:{0} ID:{1} Location: {2}", roomToAdd.RoomName, roomToAdd.RoomID, roomToAdd.Location));
								/*
								if (element.Name == "Room") {
									XmlReader reader = new XmlReader(element.OuterXml);
									Debug.Console(2, String.Format("OuterXML {0}", element.OuterXml));
									Room roomListRoom = new Room();

									roomListRoom = CrestronXMLSerialization.DeSerializeObject<Room>(reader);
									Debug.Console(2, String.Format("Var is: {0}, Type: {1}", roomListRoom.RoomName, roomListRoom.GetType()));

									// AvailableRooms.RoomList.Add(roomAvailable); 
									RoomList.Add(roomListRoom);
									Debug.Console(2, String.Format("Room added to RoomList"));

									//RoomAvailibility.Add(AvailableRooms);
									}
								 */

							}
							getAvailableRooms();
						}
					}
				}
				catch (Exception e)
				{
					Debug.Console(2, String.Format("Error: {0}, {1}, {2}", e.Message, e.InnerException, e.StackTrace));
					Debug.Console(2, String.Format("E: {0}", e));
				}

				//getAvailableRooms();
			}
			#endregion
		}
	}
}