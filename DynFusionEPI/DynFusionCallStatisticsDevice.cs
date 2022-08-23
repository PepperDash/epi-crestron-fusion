using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.Fusion;
using PepperDash.Core;
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

            Debug.Console(2, this, "DynFusionCallStatistics Created Device: {0}, {1}", name, type);
        }

        public void StartDevice()
        {
            _callTime.Start();
            if (_useCallTimer)
            {
                if (_callTimer == null)
                {
                    Debug.Console(2, this, "DynFusionCallStatistics Creating Timer");
                    _callTimer = new CTimer(o => CallTimeFeedback.FireUpdate(), null, 0, 1000);
                }
                else
                {
                    Debug.Console(2, this, "DynFusionCallStatistics Resetting CTimer");
                    _callTimer.Reset(0, 1000);
                }
            }
        }

        public void StopDevice()
        {
            if (_callTime.IsRunning)
            {
                _callTime.Stop();
                var minUsed = _callTime.Elapsed.Minutes;

                Debug.Console(2, this, "Call Time = {0}", _callTime.Elapsed.ToString());
                Debug.Console(2, this, "DynFusionCallStatistics Stopped: minUsed = {0}", minUsed.ToString("D"));

                if (_callTimer != null)
                    _callTimer.Stop();

                if (minUsed >= UsageMinThreshold)
                {
                    const string meetingId = "-";

                    // TODO
                    /*
                    string MeetingID;
                    if (_DynFusion.FusionSchedule != null && _postMeetingId == true)
                    {
                        if (_DynFusion.FusionSchedule.CurrentMeeting != null)
                            MeetingID = _DynFusion.FusionSchedule.CurrentMeeting.MeetingID;
                        else
                            MeetingID = "-";
                    }
                    else
                    {
                        MeetingID = "-";
                    }
                    */

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

                    Debug.Console(2, this, "Call Time = {0}", _callTime.Elapsed.ToString());
                    Debug.Console(2, this, "DynFusionCallStatistics message \n{0}", usageString);
                }
                else
                {
                    Debug.Console(2, this, "DynFusionCallStatistics did not pass threshold");
                }
            }
            else
            {
                Debug.Console(2, this, "Call Timer not running");
            }
        }
    }
}