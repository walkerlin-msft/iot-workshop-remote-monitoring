using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelemetryEPHostConsoleApp
{
    class TelemetryEventProcessor : IEventProcessor
    {
        static WebServerConnector _webSC = new WebServerConnector();
        Stopwatch checkpointStopWatch;

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine("Processor Shutting Down. Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason);
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Console.WriteLine("TelemetryEventProcessor initialized.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset);
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                string data = Encoding.UTF8.GetString(eventData.GetBytes());
                string msgType = GetMessageType(eventData, "msgType");
                if (!string.IsNullOrEmpty(msgType))
                {
                    ProcessMessage(data);
                }
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }

        private void ProcessMessage(string data)
        {
            try
            {
                TelemetryMessage telemetryMessage = JsonConvert.DeserializeObject<TelemetryMessage>(data);

                ConsoleColor color;
                if (telemetryMessage.deviceId.Equals("LinuxTurbine"))
                    color = ConsoleColor.Yellow;
                else
                    color = ConsoleColor.Green;

                Console.ForegroundColor = color;
                Console.WriteLine("deviceId = {0}, msgId = {1}, speed = {2}, depreciation = {3}, power = {4}, time = {5}", 
                    telemetryMessage.deviceId, 
                    telemetryMessage.msgId, 
                    telemetryMessage.speed, 
                    telemetryMessage.depreciation,
                    telemetryMessage.power,
                    telemetryMessage.time);
                Console.ResetColor();

                string webSCResult = _webSC.PostTelemetryMessage(telemetryMessage);
                //Console.WriteLine(webSCResult);

            } catch(Exception ex)
            {
                Console.WriteLine(DateTime.UtcNow.ToString() + ": Exception : " + ex.Message);
            }
        }

        private string GetMessageType(EventData eventData, string elementName)
        {
            // Try get msgType from message Properties first
            if (eventData.Properties.Count > 0)
            {
                if (eventData.Properties[elementName] != null)
                    return eventData.Properties[elementName].ToString();                
            }

            return null;
        }
    }
}
