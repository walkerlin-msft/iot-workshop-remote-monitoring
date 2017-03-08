using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryEPHostConsoleApp
{
    class Program
    {
        private const string STORAGEACCOUNT_PROTOCOL = "https";// We use HTTPS to access the storage account
        public static string _webServerUrl { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Console App for Telemetry Event Processor Host...\n");

            /* Load the settings from App.config */
            string isProduction = ConfigurationManager.AppSettings["WebServer.isProduction"];
            if (isProduction.Equals("1"))
                _webServerUrl = ConfigurationManager.AppSettings["WebServer.Production"];
            else
                _webServerUrl = ConfigurationManager.AppSettings["WebServer.Localhost"];

            Console.WriteLine("_webServerUrl={0}\n", _webServerUrl);

            // IoT Hub
            string iotHubConnectionString = ConfigurationManager.AppSettings["IoTHub.ConnectionString"];
            Console.WriteLine("iotHubConnectionString={0}\n", iotHubConnectionString);

            string eventHubPath = "messages/events";// It's hard-coded for IoT Hub
            string consumerGroupName = "telemetrypush";// It's hard-coded for this workshop

            // Storage Account
            string storageAccountName = ConfigurationManager.AppSettings["StorageAccount.Name"];
            string storageAccountKey = ConfigurationManager.AppSettings["StorageAccount.Key"];
            string storageAccountConnectionString = CombineConnectionString(storageAccountName, storageAccountKey);
            Console.WriteLine("storageAccountConnectionString={0}\n", storageAccountConnectionString);

            string eventProcessorHostName = Guid.NewGuid().ToString();
            string leaseName = eventProcessorHostName;

            EventProcessorHost eventProcessorHost = new EventProcessorHost(
                eventProcessorHostName,
                eventHubPath,
                consumerGroupName,
                iotHubConnectionString,
                storageAccountConnectionString,
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

        private static string CombineConnectionString(string storageAccountName, string storageAccountKey)
        {
            return "DefaultEndpointsProtocol=" + STORAGEACCOUNT_PROTOCOL + ";" +
                "AccountName=" + storageAccountName + ";" +
                "AccountKey=" + storageAccountKey;
        }
    }
}
