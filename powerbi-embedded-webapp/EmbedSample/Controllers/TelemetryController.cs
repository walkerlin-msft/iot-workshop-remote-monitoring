using Microsoft.AspNet.SignalR;
using paas_demo.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace paas_demo.Controllers
{
    public class TelemetryController : Controller
    {
        // POST Request from Event Processor Host
        [HttpPost]
        public ActionResult PutTelemetry(string deviceId, string msgId, double speed, double depreciation, double power, string time)
        {

            System.Diagnostics.Debug.WriteLine("deviceId = {0}, msgId = {1}, speed = {2}, depreciation = {3}, power = {4}, time = {5}",
                    deviceId,
                    msgId,
                    speed,
                    depreciation,
                    power,
                    time);

            DateTime eventTime = DateTime.Parse(time);
            long epoch = (eventTime.Ticks - 621355968000000000) / 10000;

            var context = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
            context.Clients.All.sendTelemetry(deviceId, msgId, speed, depreciation, power, epoch);

            return this.Content("");
        }
    }
}
