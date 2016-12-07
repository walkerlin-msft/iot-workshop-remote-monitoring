using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryEPHostConsoleApp
{
    class Program
    {
        /* Please check your URL of web server */
#if true // Local Host
        public const string WEBSERVER_URL = "http://localhost:13526/Telemetry/PutTelemetry";
#else   // Production Site
        public const string WEBSERVER_URL = "http://iotworkshop20161118.azurewebsites.net/Telemetry/PutTelemetry";
#endif  

        static void Main(string[] args)
        {
            // IoT Hub
            string eventHubConnectionString = "[Please replace the connection string of IoT Hub]";// "HostName=iothub112101.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9plxivts9llrJWJaAakiU2wT8uvAvGrCr1MyiJXjlro=";
            string eventHubPath = "messages/events";
            string consumerGroupName = "telemetrypush";// It should be fixed for this workshop

            /* Please replace the AccountName and AccountKey of your Storage Account */
            string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=iotworkshopsa112101;AccountKey=WLVBbrurJbmw05TyBtd9vzyE3OCM7zt5H3onzoSFyVDUmgb8/Df9wQcPEpVDR5UhaiktiZ6uDFI53jfozaf1+A==";

            string eventProcessorHostName = Guid.NewGuid().ToString();
            string leaseName = eventProcessorHostName;

            EventProcessorHost eventProcessorHost = new EventProcessorHost(
                eventProcessorHostName,
                eventHubPath,
                consumerGroupName, 
                eventHubConnectionString,
                storageConnectionString,
                leaseName);

            Console.WriteLine("Registering EventProcessor...");

            var options = new EventProcessorOptions
            {
                InitialOffsetProvider = (partitionId) => DateTime.UtcNow
            };
            options.ExceptionReceived += (sender, e) => { Console.WriteLine(e.Exception); };
            eventProcessorHost.RegisterEventProcessorAsync<TelemetryEventProcessor>(options).Wait();

            Console.WriteLine("Receiving. Press enter key to stop worker.");
            Console.ReadLine();
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();
        }
    }
}
