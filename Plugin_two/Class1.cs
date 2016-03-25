using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginContract;
using System.Timers;
using System.Threading;
using System.Diagnostics;

namespace Plugin_two
{
    [Serializable]
    public class Plugin_2 : IPlugin
    {

        System.Timers.Timer counter;
        string _pluginName = "UPDATER";
        int _timerInterval = 4000;
        bool _terminateRequestReceived;
        PluginStatus _Status = PluginStatus.Stopped;
        string _pluginID = "9C1037F4-6B63-4A8A-AA8B-9BB95CBD675D";

        public string PluginID()
        {
            return _pluginID;
        }

        public string GetName()
        {
            return _pluginName;
        }
        public string GetVersion()
        {
            return "";
        }
        public PluginStatus GetStatus()
        {
            return _Status;
        }

        public void ProcessEnded()
        {
            _Status = PluginStatus.Running;
            if (_terminateRequestReceived) // plugin has been asked to notify on process so it can be terminated, therefore send "unload" message
            {
                if (counter != null)
                {
                    counter.Stop();
                }
                PluginEventAction actionCommand = new PluginEventAction();
                actionCommand.ActionToTake = PluginActionType.Unload;
            }
        }

        public bool TerminateRequestReceived()
        {
            return _terminateRequestReceived;
        }
        public bool Start()
        {
            DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "UPD Started"));
            RunProcess();
            return true;
        }
        public void LogError(string Message, EventLogEntryType LogType)
        {
            EventLogger.LogEvent(Message, LogType);
        }

        // OnTimer event, process start raised, sleep to simulate doing some work, then process end raised
        public void OnCounterElapsed(Object sender, EventArgs e)
        {
            _Status = PluginStatus.Processing;
            DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "Counter elapsed from: " + _pluginName));
            if (_terminateRequestReceived)
            {
                counter.Stop();
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "Acting on terminate signal: " + _pluginName));
                _Status = PluginStatus.Stopped;
                Call_Die();
            }
            else
            {
                _Status = PluginStatus.Running; // nb: in normal plugin, this gets set after all processes complete - may be after scrapes etc.
            }

        }

        public void Call_Die()
        {
            _Status = PluginStatus.Stopped;
            DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "Calling die, stopping process, sending UNLOAD...  from: " + _pluginName));
            PluginEventAction actionCommand = new PluginEventAction();
            actionCommand.ActionToTake = PluginActionType.Unload;
            DoCallback(new PluginEventArgs(PluginEventMessageType.Action, null, actionCommand)); // make a generic "reboot / update service" event handler ?!
        }

        public bool Stop()
        {
            if (_Status == PluginStatus.Running) // process running - cannot die yet, instead, flag to die at next opportunity
            {
                _terminateRequestReceived = true;
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "Stop called but process is running from: " + _pluginName));
            }
            else
            {
                if (counter != null)
                {
                    counter.Stop();
                }
                _terminateRequestReceived = true;
                DoCallback(new PluginEventArgs(PluginEventMessageType.Message, "Stop called from: " + _pluginName));
            }

            return true;
        }

        // this is the main kick off process. The most important thing is to manage the status - this determines when the plugin dies
        public string RunProcess()
        {
            _Status = PluginStatus.Running;
            if (counter == null)
            {
                counter = new System.Timers.Timer(_timerInterval);
            }
            else
            {
                counter.Stop();
                counter.Enabled = false;
                counter.Interval = _timerInterval;
            }

            counter.Elapsed += OnCounterElapsed;
            counter.Start();
            return "";
        }

        public event EventHandler<PluginEventArgs> CallbackEvent;

        public void DoCallback(PluginEventArgs e)
        {
            if (CallbackEvent != null)
            {
                CallbackEvent(this, e);
            }
        }



    }
}
