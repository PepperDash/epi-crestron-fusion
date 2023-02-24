using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharpPro;



using PepperDash.Core;

namespace DynFusion
{

	public class Room
	{
		public string RoomId { get; set; }
		public string RoomName { get; set; }
		public string Location { get; set; }
		public bool OnlineStatus { get; set; }
		public double AvailableMinutes
		{
			get
			{
				double returnValue;
				if (FreeBusyStatus.Contains("T"))
				{
				    var tempTime = DateTime.Parse(FreeBusyStatus);
					//CrestronConsole.PrintLine(tempTime.ToString());
				    returnValue = tempTime > DateTime.Now ? Math.Round(tempTime.Subtract(DateTime.Now).TotalMinutes) : 0;
				}

				else { returnValue = 0; }

				return returnValue;

			}
		}
		public string FreeBusyStatus { get; set; }

	}


	public class DynFusionScheduleAvailableRooms
	{
		public bool IncludeInAvailbleRooms;
		private CTimer _getAvailableRoomsTimeOut;
		private bool _availableRoomStatus;
		public bool AvailableRoomStatus
		{
			set
			{
				_availableRoomStatus = value;
				OnAvailableRoomsBusy(this, _availableRoomStatus);
				if (_availableRoomStatus) { _getAvailableRoomsTimeOut = new CTimer(GetAvailableRoomsTimeout, 6000); }
				else { _getAvailableRoomsTimeOut.Stop(); }
			}
		}
		// I would love to poll and have data ready to go but we think it may be too heavy a server load. JTA 2017-12-06
		// CTimer updateAvailableRooms = null;
		private readonly DynFusionDevice _dynFusion;
		public List<Room> RoomList = new List<Room>();
		public List<string> FilterList = new List<string>();
		public delegate void AvailableRoomUpdate(object sender, List<Room> roomList);
		public event AvailableRoomUpdate OnAvailableRoomsUpdate;
		public delegate void AvailableRoomsBusy(object sender, bool busyStatus);
		public event AvailableRoomsBusy OnAvailableRoomsBusy;

		public DynFusionScheduleAvailableRooms(DynFusionDevice dynFusionInstance)
		{
			_dynFusion = dynFusionInstance;
			_dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.DeviceExtenderSigChange += FusionRoomDataExtenderSigChange;
			// I would love to poll and have data ready to go but we think it may be too heavy a server load. JTA 2017-12-06
			// updateAvailableRooms = new CTimer(GetRoomList, null, 300000, 300000);
		}



		private void GetAvailableRoomsTimeout(object unused)
		{
			Debug.ConsoleWithLog(2, "Error getAvailableRoomsTimeout", 3);
			AvailableRoomStatus = false;
		}
		public void SendFreeBusyStatusAvailableUntil(DateTime availableUntilTime)
		{
			// "2017-12-09T00:00:00"
			_dynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("{0}", availableUntilTime.ToString("yyyy-MM-ddTHH:mm:00"));
			Debug.Console(2, string.Format("Sending FreeBusyStatus {0}", availableUntilTime.ToString("yyyy-MM-ddTHH:mm:00")));
		}
		public void SendFreeBusyStatusAvailable()
		{
			_dynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("{0}", DateTime.Now.AddDays(5).ToString("yyyy-MM-ddT00:00:00"));
			Debug.Console(2, "Sending FreeBusyStatus {0}", DateTime.Now.AddDays(5).ToString("yyyy-MM-ddT00:00:00"));
		}
		public void SendFreeBusyStatusNotAvailable()
		{
			_dynFusion.FusionSymbol.FreeBusyStatusToRoom.InputSig.StringValue = string.Format("-");
			Debug.Console(2, string.Format("-"));
		}

	    public void GetRoomList()
		{
			try
			{
			    if (!_dynFusion.FusionSymbol.IsOnline) return;
			    AvailableRoomStatus = true;

                // ReSharper disable once ConvertToConstant.Local
			    //TODO This needs to be implemented 
                var fusionRoomListRequest = "";

			    //	fusionRoomListRequest = String.Format("<RequestRoomList><RequestID>RoomListRequest</RequestID><Property>Location</Property><Value>{0}</Value></RequestRoomList>", _DynFusion.FusionRoomInfo.roomInformation.Location);
			    Debug.Console(2, String.Format("RoomList Request: {0}", fusionRoomListRequest));

			    _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomListQuery.StringValue = fusionRoomListRequest;
			}
			catch (Exception e)
			{
				Debug.Console(2, String.Format("Error Requesting Room List: {0}", e.ToString()));
			}
		}
		public void GetAvailableRooms()
		{
			try
			{
			    if (!_dynFusion.FusionSymbol.IsOnline) return;
			    var messageHeader = String.Format("<RequestRoomAttributeList><RequestID>RoomAvailabilityRequest:{0}</RequestID>", Guid.NewGuid().ToString());
			    var messageBody = RoomList.Aggregate("", (current, r) => String.Format("{0}<Room><RoomID>{1}</RoomID><Read><Attributes><Attribute><Join>a0</Join></Attribute><Attribute><Join>s23</Join></Attribute></Attributes></Read></Room>", current, r.RoomId));
			    var messageFooter = String.Format("</RequestRoomAttributeList>");
			    _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomAttributeQuery.StringValue = string.Format("{0}{1}{2}", messageHeader, messageBody, messageFooter);
			    Debug.Console(2, String.Format("RequestRoomAttributeList {0}{1}{2}", messageHeader, messageBody, messageFooter));
			    /* 
					 * if (_DynFusion.FusionSchedule.isRegisteredForSchedulePushNotifications) {
						schedulePushTimer.Stop();
						}
					 */
			}
			catch (Exception ex)
			{
				Debug.Console(2, String.Format("getAvailableRooms Error: {0}", ex.Message));
				Debug.ConsoleWithLog(2, ex.ToString());
			}
		}

	    private void FusionRoomDataExtenderSigChange(DeviceExtender currentDeviceExtender, SigEventArgs args)
	    {
	        try
	        {

	            if (args.Sig == _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomAttributeResponse)
	            {
	                Debug.Console(2, String.Format("RoomAttributeResponse Args: {0}", args.Sig.StringValue));
	                var availableRoomResponseXml = new XmlDocument();
	                availableRoomResponseXml.LoadXml(args.Sig.StringValue);

	                var availableRoomResponse = availableRoomResponseXml["RoomAttributeListResponse"];
	                Debug.Console(2, String.Format("RequestID Args: {0}", availableRoomResponse));
	                if (availableRoomResponse != null)
	                {
	                    // It may be more efficent to get rid of this foreach JTA 2017-12-06
	                    ProcessAvaiableRoomResponse(availableRoomResponse);
	                }
	                OnAvailableRoomsUpdate(this, RoomList);
	                AvailableRoomStatus = false;
	            }
	            if (args.Sig != _dynFusion.FusionSymbol.ExtenderFusionRoomDataReservedSigs.RoomListResponse) return;
	            Debug.Console(2, String.Format("RoomList Response: {0}", args.Sig.StringValue));

	            var roomListResponseXml = new XmlDocument();

	            roomListResponseXml.LoadXml(args.Sig.StringValue);

	            var roomListResponse = roomListResponseXml["RoomListResponse"];

	            if (roomListResponse == null) return;
	            var requestId = roomListResponse["RequestID"];
	            RoomList.Clear();
	            if (requestId.InnerText != "RoomListRequest") return;
	            var rooms = roomListResponse.GetElementsByTagName("Room");
	            foreach (var roomToAdd in from XmlElement element in rooms
	                select new Room
	                {
	                    RoomName = element.GetElementsByTagName("RoomName").Item(0).InnerXml,
	                    RoomId = element.GetElementsByTagName("RoomID").Item(0).InnerXml,
	                    Location = element.GetElementsByTagName("Location").Item(0).InnerXml
	                })
	            {
	                RoomList.Add(roomToAdd);
	                Debug.Console(2,
	                    String.Format("RoomAdded Name:{0} ID:{1} Location: {2}", roomToAdd.RoomName, roomToAdd.RoomId,
	                        roomToAdd.Location));
	            }
	            GetAvailableRooms();
	        }
	        catch (Exception e)
	        {
	            Debug.Console(2, String.Format("Error: {0}, {1}, {2}", e.Message, e.InnerException, e.StackTrace));
	            Debug.Console(2, String.Format("E: {0}", e));
	        }


	    }

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

	    private void ProcessAvaiableRoomResponse(XmlElement availableRoomResponse)
	    {
	        foreach (XmlElement responseElement in availableRoomResponse)
	        {
	            Debug.Console(2,
	                String.Format("RoomAttributeListResponseElement: {0} Value: {1}", responseElement.Name,
	                    responseElement.InnerXml));
	            if (responseElement.Name != "Room") continue;
	            var roomIdXml = responseElement.GetElementsByTagName("RoomID");
	            Debug.Console(2, String.Format("RoomID: {0}", roomIdXml.Item(0).InnerXml));
	            var index = RoomList.FindIndex(x => x.RoomId == roomIdXml.Item(0).InnerXml);

	            var roomAttributes = responseElement.GetElementsByTagName("Attribute");

	            foreach (XmlElement attributeElement in roomAttributes)
	            {
	                Debug.Console(2,
	                    String.Format("RoomAttributeListResponseRoomElement: {0} Value: {1}", attributeElement.Name,
	                        attributeElement.InnerXml));
	                var attributeType = attributeElement.GetElementsByTagName("Name").Item(0).InnerXml;
	                var attributeValue = attributeElement.GetElementsByTagName("Value").Item(0).InnerXml;
	                Debug.Console(2,
	                    String.Format("RoomAttributeListResponseRoomAttribute Type: {0} Value: {1}", attributeType,
	                        attributeValue));
	                switch (attributeType)
	                {
	                    case ("Online Status"):
	                    {
	                        RoomList[index].OnlineStatus = attributeValue == "2";
	                        break;
	                    }
	                    case ("Free Busy Status"):
	                    {
	                        RoomList[index].FreeBusyStatus = attributeValue;
	                        break;
	                    }
	                }
	            }
	        }
	    }


	}
}