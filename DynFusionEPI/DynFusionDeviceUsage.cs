using System;
using System.Collections.Generic;
using System.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
namespace DynFusion
{
	public class DynFusionDeviceUsage : EssentialsDevice 
	{

		public Dictionary<uint, UsageInfo> DeviceUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<uint, UsageInfo> DisplayUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<uint, UsageInfo> SourceUsageInfo = new Dictionary<uint, UsageInfo>();
		public Dictionary<string, UsageInfo> UsageInfoDict = new Dictionary<string, UsageInfo>();


		public int UsageMinThreshold = 1;
		private readonly DynFusionDevice _dynFusionDevice;

		public DynFusionDeviceUsage(string key, DynFusionDevice dynFusionInstance)
			: base(key, key)
		{
			try
			{
				_dynFusionDevice = dynFusionInstance;
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
				var newDev = new UsageInfo
				{
				    name = name,
				    type = type,
				    usageType = UsageType.Device,
				    joinNumber = (ushort) deviceNumber
				};
			    var key = string.Format("DEV:{0}", deviceNumber);
				UsageInfoDict.Add(key, newDev);
				
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
				var newDisp = new UsageInfo
				{
				    name = name,
				    type = "Display",
				    sourceNumber = 0,
				    usageType = UsageType.Display,
				    joinNumber = (ushort) deviceNumber
				};
			    var key = string.Format("DISP:{0}", deviceNumber);
				UsageInfoDict.Add(key, newDisp);
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
				var newSource = new UsageInfo {name = name, type = type, sourceNumber = sourceNumber, usageType = UsageType.Source};
			    var key = string.Format("SRC:{0}", sourceNumber);
				UsageInfoDict.Add(key, newSource);
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
			if (action)
			{
				StartDevice(key);
			}
			else
			{
				StopDevice(key);
			}
		}
		public void ChangeSource(ushort disp, ushort source)
		{
			try
			{
				var dispKey = string.Format("DISP:{0}", disp);
				Debug.Console(1, this, "DynFusionDeviceUsage Change Source {0}", dispKey);
			    if (!UsageInfoDict.ContainsKey(dispKey)) return;
			    Debug.Console(1, this, "DynFusionDeviceUsage Change Source dispKey: {0}, LastSource: {1} New Source: {2}", dispKey, UsageInfoDict[dispKey].sourceNumber, source);
			    // get last source
			    var lastSourceNumber = UsageInfoDict[dispKey].sourceNumber;

			    // Start new Device
			    if (lastSourceNumber > 0 && source > 0)
			    {
			        var newSourceKey = string.Format("SRC:{0}", source);
			        StartDevice(newSourceKey);
			        UsageInfoDict[dispKey].sourceNumber = source;
			    }
			        //Start new device && display
			    else if (lastSourceNumber == 0 && source > 0)
			    {
			        var newSourceKey = string.Format("SRC:{0}", source);
			        StartDevice(dispKey);
			        StartDevice(newSourceKey);
			        UsageInfoDict[dispKey].sourceNumber = source;
			    }
			        // Stop display
			    else if (lastSourceNumber > 0 && source == 0)
			    {
			        UsageInfoDict[dispKey].sourceNumber = source;
			        StopDevice(dispKey);
			    }
			    if (lastSourceNumber <= 0) return;
			    var onlySource = UsageInfoDict.Where(entry => entry.Key.Contains("DISP")).All(entry => entry.Value.sourceNumber != lastSourceNumber);
			    if (!onlySource) return;
			    var lastSourceKey = string.Format("SRC:{0}", lastSourceNumber);
			    StopDevice(lastSourceKey);
			}
			catch (Exception ex)
			{
				Debug.Console(0, this, "{0}", ex);
			}


		}
		public void StartDevice(string key)
		{
			if (UsageInfoDict.ContainsKey(key))
			{
				UsageInfoDict[key].StartTime = DateTime.Now;
			}
			else
			{
				Debug.Console(1,this, "DynFusionDeviceUsage no device number {0}", key);
			}
		}
		public void StopDevice(string key)
		{
		    if (!UsageInfoDict.ContainsKey(key)) return;
		    {
		        var minUsed = (int)(DateTime.Now - UsageInfoDict[key].StartTime).TotalMinutes;
		        if (minUsed >= UsageMinThreshold)
		        {
		            var usageString = string.Format("USAGE||{0}||{1}||TIME||{2}||{3}||-||{4}||-||{5}||{6}||",
		                DateTime.Now.ToString("yyyy-MM-dd"),
		                DateTime.Now.ToString("HH:mm:ss"),
		                UsageInfoDict[key].type,
		                UsageInfoDict[key].name,
		                minUsed,
		                "",
		                "");
		            _dynFusionDevice.FusionSymbol.DeviceUsage.InputSig.StringValue = usageString;
		            Debug.Console(1,this, "DynFusionDeviceUsage message \n{0}", usageString);
		        }
		        else
		        {
		            Debug.Console(1,this, "DynFusionDeviceUsage did not pass threshord {0}", key);
		        }
		    }
		}

	    public void NameDevice(ushort deviceNumber, string name)
		{
			if (DeviceUsageInfo.ContainsKey(deviceNumber))
			{
				DeviceUsageInfo[deviceNumber].name = name;
			}
			else
			{
				Debug.Console(1,this, "DynFusionDeviceUsage no device number {0}", deviceNumber);
			}
		}
		public class UsageInfo
		{
			public DateTime StartTime;
            // ReSharper disable once InconsistentNaming
			public string name;
            // ReSharper disable once InconsistentNaming
            public string type;
            // ReSharper disable once InconsistentNaming
            public uint sourceNumber;
            // ReSharper disable once InconsistentNaming
            public UsageType usageType;
            // ReSharper disable once InconsistentNaming
            public ushort joinNumber;
		}
		public enum UsageType
		{
			Device = 0,
			Display = 1,
			Source = 2
		};
	}
}