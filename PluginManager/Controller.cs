using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using PluginContract;

// todo - need logging for exceptions

namespace PluginManager
{

    public class PluginHost : MarshalByRefObject
    {
        private const string DOMAIN_NAME_COMMAND = "DOM_COMMAND";
        private const string DOMAIN_NAME_PLUGINS = "DOM_PLUGINS";

        private AppDomain domainCommand;
        private AppDomain domainPlugins;
        
        private PluginController controller_command;
        private PluginController controller_plugin;

        public event EventHandler<PluginEventArgs> PluginCallback;

        public bool HostIsTerminating = false;

        // acts as a simple pass through for the owner object / service
         void OnCallback(PluginEventArgs e)
        {
            if (PluginCallback != null)
            {
                PluginCallback(this, e);
            }
        }

    
        public PluginHost()
        
        {
            init();
        }

        // remote ojects have a timeout - this override ensures the plugin stays put until  we manually unload and must be present in all classes that implement "MarshalByRefObject"
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void init()
        {
            if (domainCommand == null)
            {
                domainCommand = AppDomain.CreateDomain(DOMAIN_NAME_COMMAND);
            }        
            if (domainPlugins == null)
            {
                domainPlugins = AppDomain.CreateDomain(DOMAIN_NAME_PLUGINS);
            }

        }

        public void LoadDomain(PluginAssemblyType controllerToLoad)
        {
            init();
            switch (controllerToLoad)
            {
                case PluginAssemblyType.Command:
                    {
                        controller_command = (PluginController)domainCommand.CreateInstanceAndUnwrap((typeof(PluginController)).Assembly.FullName, (typeof(PluginController)).FullName);
                        controller_command.Callback += Plugins_Callback;
                        controller_command.LoadPlugin(PluginAssemblyType.Command);
                        return;
                    }
                case PluginAssemblyType.Plugin:
                    {
                        controller_plugin = (PluginController)domainPlugins.CreateInstanceAndUnwrap((typeof(PluginController)).Assembly.FullName, (typeof(PluginController)).FullName);
                        controller_plugin.Callback += Plugins_Callback;
                        controller_plugin.LoadPlugin(PluginAssemblyType.Plugin);
                        return;
                    }
            }
        }

        public void LoadAllDomains()
        {
            init();
            LoadDomain(PluginAssemblyType.Command);
            LoadDomain(PluginAssemblyType.Plugin);
        }

        public void UnloadAllDomains()
        {
            UnLoadDomain(PluginAssemblyType.Command);
            UnLoadDomain(PluginAssemblyType.Plugin);
        }

        public void UnLoadDomain(PluginAssemblyType domainToUnLoad)
        {
            init();
            try
            {
            switch (domainToUnLoad) {
                case PluginAssemblyType.Command:
                    {
                        if (domainCommand != null) { 
                        AppDomain.Unload(domainCommand);
                        domainCommand = null;
                        //controller_command = null;                        
                    }
                        break;
                }
                case PluginAssemblyType.Plugin:
                    {
                        if (domainPlugins != null)
                        {
                            AppDomain.Unload(domainPlugins);
                            domainPlugins = null;
                            //controller_plugin = null;
                        }
                        break;
                    }
            
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                var s = ex.Message;
                EventLogger.LogEvent("Error: " + ex.Message, EventLogEntryType.Error);
            }

        }

        public void LoadDomainPlugin(PluginAssemblyType LoadType)
        {
            switch (LoadType)
            {
                    case PluginAssemblyType.Command:
                        {
                            LoadDomain(PluginAssemblyType.Command);
                    return;
                }
                    case PluginAssemblyType.Plugin:
                        {
                            LoadDomain(PluginAssemblyType.Plugin);
                    return;
                }
            }
                    
        }

        public void StopAllPlugins()
        {
            StopPlugin(PluginAssemblyType.Command);
            StopPlugin(PluginAssemblyType.Plugin);        
        }

        public void StopPlugin(PluginAssemblyType PluginTypeToStop)
        {
            init();
            switch (PluginTypeToStop)
            {
                case PluginAssemblyType.Command:
                    {
                        if (controller_command != null)
                        {
                            controller_command.IsShuttingDown = true;
                            controller_command.StopPluginType(PluginTypeToStop);
                        }
                        return;
                    }
                case PluginAssemblyType.Plugin:
                    {
                        if (controller_plugin != null)
                        {
                            controller_plugin.IsShuttingDown = true;
                            controller_plugin.StopPluginType(PluginTypeToStop);
                        }
                        return;
                    }
            }            
        }

        public void StartAllPlugins()
        {
            StartPlugin(PluginAssemblyType.Command);
            StartPlugin(PluginAssemblyType.Plugin);
        }

        public void StartPlugin(PluginAssemblyType PluginTypeToStart)
        {
            init();
            switch (PluginTypeToStart)
            {
                case PluginAssemblyType.Command:
                    {
                        if (controller_command != null)
                        {
                            controller_command.StartPluginType(PluginTypeToStart);
                        }
                        return;
                    }
                case PluginAssemblyType.Plugin:
                    {
                        if (controller_plugin != null)
                        {
                            controller_plugin.StartPluginType(PluginTypeToStart);
                        }
                        return;
                    }
            }
        }


        private bool AllDomainPluginsStopped()
        {
            bool LCanTerminate = false;
            bool cmd_Unload = controller_command.CanUnload;
            bool plg_unload = controller_plugin.CanUnload;
            LCanTerminate = (cmd_Unload & plg_unload);
            return LCanTerminate;
        }


        private void Plugins_Callback(object source, PluginEventArgs e)
        {
            // raise own callback to be hooked by service/application
            // pass through callback messages received            
            if (e.EventAction.ActionToTake == PluginActionType.SignalTerminate)
            {
                StopAllPlugins();
            }
            else if (e.EventAction.ActionToTake == PluginActionType.UpdateWithInstaller) // check are ALL domains clear, if yes, pass through message, else ignore
            {
                if (AllDomainPluginsStopped())
                { 
                    OnCallback(e); 
                }
            }
            else
                OnCallback(e);
        }


    }


    public class PluginController : MarshalByRefObject
    {
        const string _domainCommand = "command";
        const string _domainPlugin = "plugin";

        const string _assemblyNameCommand = "PLG_CMD"; // command and control plugin
        const string _genericPluginMask = "PLGP_*.dll"; // assumes all generic plugins have this prefix

        public bool IsShuttingDown = false; // internal management flag

        public List<PluginInstance> Plugins; // list of all plugins in this domain

        public event EventHandler<PluginEventArgs> Callback;

        public bool CanUnload = false; // flag to indicate all plugins in this managed domain are stopped and the domain can be unloaded

        #region Plugin management
      

        void OnCallback(PluginEventArgs e)
        {
            // raise own callback to be hooked by service/application
            // pass through callback messages received if relevant
            if (e.MessageType == PluginEventMessageType.Action)
            {
                //if received put self into shutdown mode...
                if (e.EventAction.ActionToTake == PluginActionType.TerminateAndUnloadPlugins)
                {
                    Console.WriteLine("*** UNLOAD ALL RECEIVED!! ... sending SHUTDOWN ...");
                    IsShuttingDown = true;
                    // todo - tell all plugings to stop 
                    //StopAllPlugins();
                    if (Callback != null)
                    {
                        //OnCallback(new PluginEventArgs(PluginEventMessageType.Message, "** UNLOAD ALL RECEIVED - SENDING DIE SIGNAL**"));
                        e.MessageType = PluginEventMessageType.Message;
                        e.ResultMessage = "** UNLOAD ALL RCEIVED- SENDING DIE SIGNAL **";
                        Callback(this, e);
                        e.MessageType = PluginEventMessageType.Action;
                        e.EventAction.ActionToTake = PluginActionType.SignalTerminate;  //< this is the trigger back into itself to signal terminate
                        Callback(this, e);
                    }
                }
                else if (e.EventAction.ActionToTake == PluginActionType.Unload) // since the plugin manager manages plugins, we intercept this type of message and dont pass it on
                {
                    e.MessageType = PluginEventMessageType.Message;
                    e.ResultMessage = "Unload received from plugin";
                    Callback(this, e);

                    if (IsShuttingDown) // are we trying to astop all running plugins?
                    {
                        // are all plugins ** in curren domain context ** in stopped state? ... if yes, we can unload them and notify owner
                        bool LCanTerminate = true;
                        foreach (var itm in Plugins)
                        {
                            PluginStatus itmStatus = itm.Instance.GetStatus();
                            e.MessageType = PluginEventMessageType.Message;
                            e.ResultMessage = "Unload ** checking for stopped: stopped: = " +itmStatus.ToString();
                            Callback(this, e);
                            if (itmStatus != PluginStatus.Stopped)
                            //if (itm.Status != PluginStatus.Stopped)
                            {
                                LCanTerminate = false;
                                break;
                            }

                        }

                        if (LCanTerminate)
                        {
                            if (Callback != null) // all done, nothing to see here, move along... trigger the owner to die....
                            {
                                CanUnload = true;
                                e.MessageType = PluginEventMessageType.Action;
                                e.EventAction.ActionToTake = PluginActionType.UpdateWithInstaller;  //< this is the trigger to finally kill *** current domain context ***
                                Callback(this, e);
                            }
                        }

                        e.MessageType = PluginEventMessageType.Message;
                        e.ResultMessage = "Can terminate: " + LCanTerminate.ToString() ;
                        Callback(this, e);


                    }
                }
            }
            else
            {
                if (Callback != null) // should ONLY happen is not type action and only message
                {
                    Callback(this, e);
                }
            }


        }



        public override object InitializeLifetimeService()
        {
            return null;
        }


        public void ProxyLoader_RaiseCallbackEvent(object source, PluginEventArgs e)
        {
            OnCallback(e);
        }

        public void LoadPlugin(PluginAssemblyType PluginType)
        {
            switch (PluginType)
            {
                case PluginAssemblyType.Command:
                    {
                        AppDomain.CurrentDomain.Load(_assemblyNameCommand);
                        AddPlugin(Load(_assemblyNameCommand, ProxyLoader_RaiseCallbackEvent), PluginAssemblyType.Command, false);
                        return;
                    }
                case PluginAssemblyType.Plugin:
                    {
                        try
                        {
                            string[] FileList = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, _genericPluginMask);
                            foreach (var plugin in FileList)
                            {
                                string _plugin = Path.GetFileNameWithoutExtension(plugin);
                                AppDomain.CurrentDomain.Load(_plugin);
                                AddPlugin(Load(_plugin, ProxyLoader_RaiseCallbackEvent), PluginAssemblyType.Plugin, false);
                            }
                            return;
                        }
                        catch (Exception ex)
                        {
                            EventLogger.LogEvent("Error loading plugin: " + ex.Message, EventLogEntryType.Error);
                            break;
                        }
                    }
                default:
                    {
                        break;
                    }
            }
        }


        public void AddPlugin(IPlugin Plugin, PluginAssemblyType PluginType, bool AutoStart)
        {
            try
            {
                PluginInstance pInstance = new PluginInstance();
                pInstance.Instance = (IPlugin)Plugin;
                pInstance.PluginType = PluginType;
                pInstance.Status = PluginStatus.Stopped;
                Plugins.Add(pInstance);
            }
            catch (Exception ex)
            {
                EventLogger.LogEvent("Error adding plugin: " + ex.Message, EventLogEntryType.Error);
            }

        }


        public void StopPluginType(PluginAssemblyType StartType)
        {
            for (int i = Plugins.Count - 1; i > -1; i--)
            {
                if (Plugins[i].PluginType == StartType)
                {
                    Plugins[i].Instance.Stop();
                }
            }
        }

        public void StartPluginType(PluginAssemblyType StartType)
        {
            for (int i = Plugins.Count - 1; i > -1; i--)
            {
                if (Plugins[i].PluginType == StartType)
                {
                    Plugins[i].Instance.Start();
                }
            }
        }

        public void RemovePluginFromList(PluginAssemblyType UnloadType)
        {
            for (int i = Plugins.Count - 1; i > -1; i--)
            {
                if (Plugins[i].PluginType == UnloadType)
                {
                    Plugins.RemoveAt(i);
                }
            }
        }


        #endregion


        public IPlugin Load(string assemblyName, EventHandler<PluginEventArgs> proxyLoader_RaiseCallbackEvent)
        {
            AssemblyInstanceInfo AInfo = new AssemblyInstanceInfo();
            //nb: this AppDomain.CurrentDomain is in its own context / different from the caller app domain.
            Assembly pluginAssembly = AppDomain.CurrentDomain.Load(assemblyName);
            object instance = null;
            foreach (Type type in pluginAssembly.GetTypes())
            {
                if (type.GetInterface("IPlugin") != null)
                {
                    instance = Activator.CreateInstance(type, null, null);
                    ((IPlugin)instance).CallbackEvent += proxyLoader_RaiseCallbackEvent;
                }
            }
            return (IPlugin)instance;
        }

        public PluginController()
        {
            init();
        }



        private void init()
        {
            if (Plugins == null)
            {
                Plugins = new List<PluginInstance>();
            }
            else
            {


            }

        }

    }

    public class AssemblyInstanceInfo : MarshalByRefObject
    {
        public Assembly ASM;
        public Object ObjectInstance;
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
      
    public class PluginInstance : MarshalByRefObject
    {
    public IPlugin Instance;
    public PluginAssemblyType PluginType;
    public PluginStatus Status;

    public override object InitializeLifetimeService()
    {
        return null;
    }

    }



}


