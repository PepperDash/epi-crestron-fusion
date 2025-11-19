using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Fusion;
using PepperDash.Core.Logging;
using PepperDash.Essentials.Core;

namespace DynFusion
{
    public class DynFusionCallStatisticsDevice : EssentialsDevice
    {
        public uint JoinNumber { get; private set; }
        private readonly string _name;
        private readonly FusionRoom _symbol;
        private readonly string _type;
        private readonly bool _useCallTimer;
        private readonly bool _postMeetingId;

        // private DateTime _startTime;
        private readonly Stopwatch _callTime = new Stopwatch();
        private CTimer _callTimer;

        public readonly StringFeedback CallTimeFeedback;
        public readonly int UsageMinThreshold = 1;

        public DynFusionCallStatisticsDevice(string key, string name, FusionRoom symbol, string type, bool useCallTimer,
            bool postMeetingId, uint joinNumber)
            : base(key)
        {
            JoinNumber = joinNumber;
            _name = name;
            _symbol = symbol;
            _type = type;
            _useCallTimer = useCallTimer;
            _postMeetingId = postMeetingId;

            CallTimeFeedback = new StringFeedback(
                () => !_callTime.IsRunning
                    ? "00:00:00"
                    : string.Format("{0:00}:{1:00}:{2:00}",
                        _callTime.Elapsed.Hours,
                        _callTime.Elapsed.Minutes,
                        _callTime.Elapsed.Seconds));

            this.LogVerbose("DynFusionCallStatistics Created Device: {name}, {type}", name, type);
        }

        public void StartDevice()
        {
            _callTime.Start();

            if (!_useCallTimer)
            {
                return;
            }

            if (_callTimer == null)
            {
                _callTimer = new CTimer(o => CallTimeFeedback.FireUpdate(), null, 0, 1000);
            }
            else
            {
                _callTimer.Reset(0, 1000);
            }
        }

        public void StopDevice()
        {
            if (_callTime.IsRunning)
            {
                _callTime.Stop();
                var minUsed = _callTime.Elapsed.Minutes;

                this.LogVerbose("DynFusionCallStatistics Stopped: minUsed = {minUsed}", minUsed.ToString("D"));

                if (_callTimer != null)
                    _callTimer.Stop();

                if (minUsed >= UsageMinThreshold)
                {
                    const string meetingId = "-";

                    var usageString = string.Format("STAT||{0}||{1}||CALL||{2}||{3}||||{4}||Success||-||{5}||",
                        DateTime.Now.ToString("yyyy-MM-dd"),
                        DateTime.Now.ToString("HH:mm:ss"),
                        _type,
                        _name,
                        minUsed,
                        "",
                        "",
                        meetingId);

                    _symbol.DeviceUsage.InputSig.StringValue = usageString;

                    _callTime.Reset();
                    CallTimeFeedback.FireUpdate();

                    this.LogVerbose("DynFusionCallStatistics message {message}", usageString);
                }
                else
                {
                    this.LogVerbose("DynFusionCallStatistics did not pass threshold");
                }
            }
            else
            {
                this.LogVerbose("Call Timer not running");
            }
        }
    }
}