using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceTest
{
    static class Program
    {
        static void Main()
        {
            //Debug code
            if (!Environment.UserInteractive)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] 
			    { 
				   new PluginServiceExample() 
			    };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                PluginServiceExample service = new PluginServiceExample();

                service.Start();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            }
        }
    }
}
