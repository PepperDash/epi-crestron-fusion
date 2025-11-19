using System;
using System.Collections.Generic;
using PepperDash.Core.Logging;
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
				this.LogError("DynFusionDeviceUsage exception: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
			}
		}
		public void CreateDevice(uint deviceNumber, string type, string name)
		{
			try
			{
				var NewDev = new UsageInfo
				{
					name = name,
					type = type,
					usageType = UsageType.Device,
					joinNumber = (ushort)deviceNumber
				};
				var key = string.Format("DEV:{0}", deviceNumber);
				usageInfoDict.Add(key, NewDev);

				this.LogDebug("DynFusionDeviceUsage Created Device key: {key}", key);
			}
			catch (Exception ex)
			{
				this.LogError("DynFusionDeviceUsage exception: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
			}
		}
		public void CreateDisplay(uint deviceNumber, string name)
		{
			try
			{
				var NewDisp = new UsageInfo
				{
					name = name,
					type = "Display",
					sourceNumber = 0,
					usageType = UsageType.Display,
					joinNumber = (ushort)deviceNumber
				};
				var key = string.Format("DISP:{0}", deviceNumber);
				usageInfoDict.Add(key, NewDisp);
				this.LogDebug("DynFusionDeviceUsage Created Display key: {key}", key);
			}
			catch (Exception ex)
			{
				this.LogError("DynFusionDeviceUsage exception: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
			}
		}
		public void CreateSource(uint sourceNumber, string name, string type)
		{
			try
			{
				var NewSource = new UsageInfo
				{
					name = name,
					type = type,
					sourceNumber = sourceNumber,
					usageType = UsageType.Source
				};
				var key = string.Format("SRC:{0}", sourceNumber);
				usageInfoDict.Add(key, NewSource);
				this.LogDebug("DynFusionDeviceUsage Created Source key: {key}", key);
			}
			catch (Exception ex)
			{
				this.LogError("DynFusionDeviceUsage exception: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
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

				this.LogDebug("DynFusionDeviceUsage Change Source {dispKey}", dispKey);

				if (usageInfoDict.ContainsKey(dispKey))
				{
					this.LogDebug("Change Source displayKey: {displayKey}, LastSource: {lastSource} New Source: {newSource}", dispKey, usageInfoDict[dispKey].sourceNumber, source);
					// get last source
					var lastSourceNumber = usageInfoDict[dispKey].sourceNumber;

					// Start new Device
					if (lastSourceNumber > 0 && source > 0)
					{
						var newSourceKey = string.Format("SRC:{0}", source);
						StartDevice(newSourceKey);
						usageInfoDict[dispKey].sourceNumber = source;
					}
					//Start new device && display
					else if (lastSourceNumber == 0 && source > 0)
					{
						var newSourceKey = string.Format("SRC:{0}", source);
						StartDevice(dispKey);
						StartDevice(newSourceKey);
						usageInfoDict[dispKey].sourceNumber = source;
					}
					// Stop display
					else if (lastSourceNumber > 0 && source == 0)
					{
						usageInfoDict[dispKey].sourceNumber = source;
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
				this.LogError("DynFusionDeviceUsage exception: {message}", ex.Message);
				this.LogDebug(ex, "Stack Trace: ");
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
				this.LogWarning("DynFusionDeviceUsage no device number {key}", key);
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
						this.LogVerbose("DynFusionDeviceUsage message \n{usageString}", usageString);
					}
					else
					{
						this.LogWarning("did not pass threshold {key}", key);
					}
				}
				else
				{
					this.LogWarning("no device number with Start time {key}", key);
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
				this.LogWarning("no device number {deviceNumber}", deviceNumber);
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