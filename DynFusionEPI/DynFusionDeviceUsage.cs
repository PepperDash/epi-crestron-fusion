using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using PepperDash.Core;
using PepperDash.Essentials.Core;
namespace DynFusion
{
	public class DynFusionDeviceUsage : EssentialsDevice 
	{

		public Dictionary<uint, UsageInfo> deviceUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<uint, UsageInfo> displayUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<uint, UsageInfo> sourceUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<string, UsageInfo> usageInfoDict = new Dictionary<string, UsageInfo>();


		public int usageMinThreshold = 1;
		private DynFusionDevice _DynFusionDevice;

		public DynFusionDeviceUsage(string key, DynFusionDevice DynFusionInstance)
			: base(key, key)
		{
			try
			{
				_DynFusionDevice = DynFusionInstance;
			}
			catch (Exception ex)
			{
				Debug.Console(0, this, "{0}", ex);
			}
		}
		public void CreateDevice(uint deviceNumber, string type, string name)
		{
			try
			{
				var NewDev = new UsageInfo();
				NewDev.name = name;
				NewDev.type = type;
				NewDev.usageType = UsageType.Device;
				NewDev.joinNumber = (ushort)deviceNumber;
				var key = string.Format("DEV:{0}", deviceNumber);
				usageInfoDict.Add(key, NewDev);
				
				Debug.Console(1,this, string.Format("DynFusionDeviceUsage Created Device key: {0}", key));
			}
			catch (Exception ex)
			{
				Debug.Console(0, this, "{0}", ex);
			}
		}
		public void CreateDisplay(uint deviceNumber, string name)
		{
			try
			{
				var NewDisp = new UsageInfo();
				NewDisp.name = name;
				NewDisp.type = "Display";
				NewDisp.sourceNumber = 0;
				NewDisp.usageType = UsageType.Display;
				NewDisp.joinNumber = (ushort)deviceNumber;
				var key = string.Format("DISP:{0}", deviceNumber);
				usageInfoDict.Add(key, NewDisp);
				Debug.Console(1,this, string.Format("DynFusionDeviceUsage Created Display key: {0}", key));
			}
			catch (Exception x)
			{
				Debug.Console(1,this, "{0}", x);
			}
		}
		public void CreateSource(uint sourceNumber, string name, string type)
		{
			try
			{
				var NewSource = new UsageInfo();
				NewSource.name = name;
				NewSource.type = type;
				NewSource.sourceNumber = sourceNumber;
				NewSource.usageType = UsageType.Source;
				var key = string.Format("SRC:{0}", sourceNumber);
				usageInfoDict.Add(key, NewSource);
				Debug.Console(1,this, "DynFusionDeviceUsage Created Source key: {0}", key);
			}
			catch (Exception ex)
			{
				Debug.Console(0, this, "{0}", ex);
			}
		}
		public void StartStopDevice(ushort device, bool action)
		{
			var key = string.Format("DEV:{0}", device);
			if (action == true)
			{
				StartDevice(key);
			}
			else
			{
				StopDevice(key);
			}
		}
		public void changeSource(ushort disp, ushort source)
		{
			try
			{
				var dispKey = string.Format("DISP:{0}", disp);
				Debug.Console(1, this, "DynFusionDeviceUsage Change Source {0}", dispKey);
				if (usageInfoDict.ContainsKey(dispKey))
				{
					Debug.Console(1, this, "DynFusionDeviceUsage Change Source dispKey: {0}, LastSource: {1} New Source: {2}", dispKey, usageInfoDict[dispKey].sourceNumber, source);
					// get last source
					var lastSourceNumber = usageInfoDict[dispKey].sourceNumber;

					// Start new Device
					if (lastSourceNumber > 0 && source > 0)
					{
						var newSourceKey = string.Format("SRC:{0}", source);
						StartDevice(newSourceKey);
						usageInfoDict[dispKey].sourceNumber = (uint)source;
					}
					//Start new device && display
					else if (lastSourceNumber == 0 && source > 0)
					{
						var newSourceKey = string.Format("SRC:{0}", source);
						StartDevice(dispKey);
						StartDevice(newSourceKey);
						usageInfoDict[dispKey].sourceNumber = (uint)source;
					}
					// Stop display
					else if (lastSourceNumber > 0 && source == 0)
					{
						usageInfoDict[dispKey].sourceNumber = (uint)source;
						StopDevice(dispKey);
					}
					if (lastSourceNumber > 0)
					{

						var onlySource = true;
						foreach (KeyValuePair<string, UsageInfo> entry in usageInfoDict)
						{
							//Debug.Console(1,this, "DynFusionDeviceUsage Change Source dictEntry: {0}", entry.Key);
							if (entry.Key.Contains("DISP"))
							{
								//Debug.Console(1,this, "DynFusionDeviceUsage Change Source dictEntry Display - Source #: {0}", entry.Value.sourceNumber);
								if (entry.Value.sourceNumber == lastSourceNumber)
								{
									onlySource = false;
									break;
								}
							}
						}
						if (onlySource)
						{
							var lastSourceKey = string.Format("SRC:{0}", lastSourceNumber);
							StopDevice(lastSourceKey);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.Console(0, this, "{0}", ex);
			}


		}
		public void StartDevice(string key)
		{
			if (usageInfoDict.ContainsKey(key))
			{
				usageInfoDict[key].startTime = DateTime.Now;
			}
			else
			{
				Debug.Console(1,this, "DynFusionDeviceUsage no device number {0}", key);
			}
		}
		public void StopDevice(string key)
		{
			if (usageInfoDict.ContainsKey(key))
			{
				if (usageInfoDict[key].startTime != null)
				{
					var minUsed = (int)(DateTime.Now - usageInfoDict[key].startTime).TotalMinutes;
					if (minUsed >= usageMinThreshold)
					{
						var usageString = string.Format("USAGE||{0}||{1}||TIME||{2}||{3}||-||{4}||-||{5}||{6}||",
											DateTime.Now.ToString("yyyy-MM-dd"),
											DateTime.Now.ToString("HH:mm:ss"),
											usageInfoDict[key].type,
											usageInfoDict[key].name,
											minUsed,
											"",
											"");
						_DynFusionDevice.FusionSymbol.DeviceUsage.InputSig.StringValue = usageString;
						Debug.Console(1,this, "DynFusionDeviceUsage message \n{0}", usageString);
					}
					else
					{
						Debug.Console(1,this, "DynFusionDeviceUsage did not pass threshord {0}", key);
					}
				}
				else
				{
					Debug.Console(1,this, "DynFusionDeviceUsage no device number with Start time {0}", key);
				}
			}
		}
		public void NameDevice(ushort deviceNumber, string name)
		{
			if (deviceUsageInfo.ContainsKey(deviceNumber))
			{
				deviceUsageInfo[deviceNumber].name = name;
			}
			else
			{
				Debug.Console(1,this, "DynFusionDeviceUsage no device number {0}", deviceNumber);
			}
		}
		public class UsageInfo
		{
			public DateTime startTime;
			public string name;
			public string type;
			public uint sourceNumber;
			public UsageType usageType;
			public ushort joinNumber;
		}
		public enum UsageType : int
		{
			Device = 0,
			Display = 1,
			Source = 2
		};
	}
}