using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PluginContract
{
    // Interface each plugin must implement
    public interface IPlugin
    {
        string PluginID(); // this should be a unique GUID for the plugin - a different one shold be used for each version of the plugin. Get one from: http://createguid.com/
        bool TerminateRequestReceived(); // internal flag if sef-terminate request has been received
        string GetName(); // assembly friendly name
        string GetVersion();// can be used to store verison of assembly
        bool Start();// trigger assembly to start
        bool Stop(); // trigger assembly to stop
        void LogError(string Message, EventLogEntryType LogType); // failsafe - logs to eventlog on error
        string RunProcess(); // main process that gets called
        void Call_Die(); // process that gets called to kill the current plugin
        void ProcessEnded(); // gets called when main process ends, ie: web-scrape complete, etc...

        // custom event handler to be implemented, event arguments defined in child class
        event EventHandler<PluginEventArgs> CallbackEvent;
        PluginStatus GetStatus(); // current plugin status (running, stopped, processing...)
    }



    // event arguments defined, usage: ResultMessage is for any error trapping messages, result bool is fail/success
    // "MessageType" used to tell plugin parent if it needs to record a message or take an action etc.
    [Serializable]
    public class PluginEventArgs : EventArgs
    {
        public PluginEventMessageType MessageType;
        public string ResultMessage;
        public bool ResultValue;
        public string MessageID;
        public string executingDomain;
        public string pluginName;
        public string pluginID;
        public PluginEventAction EventAction;
        public CallbackEventType CallbackType; 

        public PluginEventArgs(PluginEventMessageType messageType = PluginEventMessageType.Message, string resultMessage = "",PluginEventAction eventAction = (new PluginEventAction()), bool resultValue = true)
        {
            // default empty values allows us to send back default event response
            this.MessageType = messageType; // define message type that is bring sent
            this.ResultMessage = resultMessage; // used to send any string messages
            this.ResultValue = resultValue;
            this.EventAction = eventAction; // if the event type = "Action" then this carries the action to take
            this.executingDomain = AppDomain.CurrentDomain.FriendlyName;
            this.pluginName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            //this.pluginID = ((IPlugin)System.Reflection.Assembly.GetExecutingAssembly()).PluginID();
        }
    }

    // used to tell event hook what to do with the message sent
    public enum PluginEventMessageType
    { 
    Message = 0, // informational message
    EventLog,  // event that needs to be logged
    Action   // action the hosting application needs to take 
    }

    public enum PluginActionType
    { 
    None = 0,
    Load,
    Unload,
    RunProcess,
    TerminateAndUnloadPlugins,
    SignalTerminate,
    UpdateWithInstaller
    }

    // when we tell the parent process to carry out an action, this tells the process the type of action and what plugin/domains to execute the action on
    // here we send in a list of actions the parent process is to carry out

    public class PluginEventActionList
    {
        public List<PluginEventAction> ActionsToTake;
        public PluginEventActionList()
        {
            if (ActionsToTake == null)
            {
                ActionsToTake = new List<PluginEventAction>();
            }
        }
    }

   [Serializable]
   public struct PluginEventAction
    {
       public PluginActionType ActionToTake;
       public PluginAssemblyType TargetPluginAssemblyType;
    }

    public enum PluginAssemblyType
    {
        None = 0,
        Command,
        Plugin
    }

    public enum CallbackEventType
    { 
        AssemblyLoad = 0,  
        AssemblyUnload,
        AssemblyStop,
        AssemblyStart,
        ProcessStart,
        ProcessStop
    }

    public enum PluginStatus
    {
        Stopped = 0,
        Running,
        Processing
    }


    // supporting logging class for plugins that implement the interfaace
    public static class EventLogger
    {
        public static EventLog log = new EventLog("Application", ".", "VetImpress Service");
        public static void LogEvent(string Message, EventLogEntryType LogType)
        {
            try
            {
                log.WriteEntry(Message, LogType, 1310);
            }
            catch (Exception ex)
            {
                // die - nothing we can do if we end up here?
            }
            
        }

    }

}