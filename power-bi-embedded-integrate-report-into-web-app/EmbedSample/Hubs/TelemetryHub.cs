using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace paas_demo.Hubs
{
    public class TelemetryHub : Hub
    {
        public void Hello()
        {
            System.Diagnostics.Debug.WriteLine("Hello!");
        }
    }
}