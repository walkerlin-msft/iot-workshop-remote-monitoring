using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.WindowsAzure;

namespace TelemetryEPHostConsoleApp
{
    class WebServerConnector
    {
        public WebServerConnector()
        { }

        public string PostTelemetryMessage(TelemetryMessage telemetryMessage)
        {
            string _realTimeDataFeedInURI = Program._webServerUrl;

            try
            {
                string postData = "deviceId=" + telemetryMessage.deviceId +"&"+
                                    "msgId=" + telemetryMessage.msgId + "&" +
                                    "speed=" + telemetryMessage.speed + "&" +
                                    "depreciation=" + telemetryMessage.depreciation + "&" +
                                    "power=" + telemetryMessage.power + "&" +
                                    "time=" + telemetryMessage.time;

                var data = Encoding.UTF8.GetBytes(postData);

                var request = (HttpWebRequest)WebRequest.Create(_realTimeDataFeedInURI);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                return new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
