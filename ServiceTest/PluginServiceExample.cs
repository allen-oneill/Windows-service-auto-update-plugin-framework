using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using PluginManager;
using PluginContract;


namespace ServiceTest
{
    partial class PluginServiceExample : ServiceBase
    {
        public PluginHost pluginHost;
        int UseInstallerVersion = 1;

        public PluginServiceExample()
        {
            InitializeComponent();
        }

        private void Plugins_Callback(object source, PluginContract.PluginEventArgs e)
        {
            if (e.MessageType == PluginEventMessageType.Message)
            {
                EventLogger.LogEvent(e.ResultMessage, EventLogEntryType.Information);
                Console.WriteLine(e.executingDomain + " - " + e.pluginName + " - " + e.ResultMessage); // for debug    
            }
            else if (e.MessageType == PluginEventMessageType.Action) {
                if (e.EventAction.ActionToTake == PluginActionType.UpdateWithInstaller)
                {
                    Console.WriteLine("****  DIE DIE DIE!!!! ... all plugins should be DEAD and UNLOADED at this stage ****");
                    EventLogger.LogEvent("Update with installer event received", EventLogEntryType.Information);
                    // Plugin manager takes care of shutting things down before calling update so we are safe to proceed...
                    if (UseInstallerVersion == 1)
                    {
                        EventLogger.LogEvent("Using installer 1", EventLogEntryType.Information);
                        UseInstallerVersion = 2;
                        // run installer1 in silent mode - it should replace files, and tell service to re-start
                    }
                    else if (UseInstallerVersion == 2)
                    {
                        EventLogger.LogEvent("Using installer 2", EventLogEntryType.Information);
                        // run installer2 in silent mode - it should replace files, and tell service to re-start
                        UseInstallerVersion = 1;
                    }
                }
            }
        }

        public void Start()
        {
            if (pluginHost == null)
            { 
                pluginHost = new PluginHost();
                pluginHost.PluginCallback += Plugins_Callback;
                pluginHost.LoadAllDomains();
                pluginHost.StartAllPlugins();
            }
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
        }
    }
}
