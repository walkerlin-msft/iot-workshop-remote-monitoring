using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServiceBusConsoleApp
{
    class Program
    {
        /* Service Bus */
        private const string QueueName = "cloud2device";// It's hard-coded for this workshop

        /* IoT Hub */
        private static ServiceClient _serviceClient;
        private const string DEVICEID_WINDOWS_TURBINE = "WinTurbine";// It's hard-coded for this workshop
        private const string DEVICEID_LINUX_TURBINE = "LinuxTurbine";// It's hard-coded for this workshop

        static void Main(string[] args)
        {
            Console.WriteLine("Console App for Alarm Service Bus...");

            /* Load the settings from App.config */
            string serviceBusConnectionString = ConfigurationManager.AppSettings["ServiceBus.ConnectionString"];
            Console.WriteLine("serviceBusConnectionString={0}\n", serviceBusConnectionString);
            string iotHubConnectionString = ConfigurationManager.AppSettings["IoTHub.ConnectionString"];
            Console.WriteLine("iotHubConnectionString={0}\n", iotHubConnectionString);

            // Retrieve a Queue Client
            QueueClient queueClient = QueueClient.CreateFromConnectionString(serviceBusConnectionString, QueueName);

            // Retrieve a Service Client of IoT Hub
            _serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            queueClient.OnMessage(message =>
            {
                Console.WriteLine("\n*******************************************************");
                string msg = message.GetBody<String>();
                try
                {
                    AlarmMessage alarmMessage = JsonConvert.DeserializeObject<AlarmMessage>(msg);

                    ProcessAlarmMessage(alarmMessage);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("****  Exception=" + ex.Message);
                }


            });

            Console.ReadLine();
        }

        private static void ProcessAlarmMessage(AlarmMessage alarmMessage)
        {
            switch (alarmMessage.alarmType)
            {
                case "CutOutSpeed":
                    ActionCutOutSpeed(alarmMessage);
                    break;
                case "Repair":
                    ActionRepair(alarmMessage);
                    break;
                case "EnableWindTurbine":
                    ActionEnableWindTurbine(alarmMessage.ioTHubDeviceID, alarmMessage.reading, alarmMessage.createdAt);
                    break;
                case "ResetDepreciation":
                    ActionResetDepreciation(alarmMessage.ioTHubDeviceID, alarmMessage.createdAt);
                    break;
                default:
                    Console.WriteLine("AlarmType is Not accpeted!");
                    break;
            }
        }

        private static void ActionCutOutSpeed(AlarmMessage alarmMessage)
        {
            if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                ActionCutOutSpeedWindows(alarmMessage);
            else if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                ActionCutOutSpeedLinux(alarmMessage);
        }

        private static void ActionCutOutSpeedWindows(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.ioTHubDeviceID) +
                    " CutOutSpeed! Speed=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.Yellow);

            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_CUTOUT_SPEED_WARNING;
            c2dCommand.value = alarmMessage.messageID;
            c2dCommand.time = alarmMessage.createdAt;

            SendCloudToDeviceCommand(
                _serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionCutOutSpeedLinux(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.ioTHubDeviceID) +
                    " CutOutSpeed! Speed=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.DarkYellow);

            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_CUTOUT_SPEED_WARNING;
            c2dCommand.Parameters = new JObject();

            SendCloudToDeviceLinuxCommand(
                _serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }
        private static void ActionRepair(AlarmMessage alarmMessage)
        {
            if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                ActionRepairWindows(alarmMessage);
            else if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                ActionRepairLinux(alarmMessage);
        }

        private static void ActionRepairLinux(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.ioTHubDeviceID) +
                    " Repair! Depreciation=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.DarkRed);

            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_REPAIR_WARNING;
            c2dCommand.Parameters = new JObject();

            SendCloudToDeviceLinuxCommand(
                _serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionRepairWindows(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(alarmMessage.ioTHubDeviceID) +
                    " Repair! Depreciation=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.Red);

            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_REPAIR_WARNING;
            c2dCommand.value = alarmMessage.messageID;
            c2dCommand.time = alarmMessage.createdAt;

            SendCloudToDeviceCommand(
                _serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionEnableWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            if (ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                ActionEnableWindowsWindTurbine(ioTHubDeviceID, on, time);
            else if (ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                ActionEnableLinuxWindTurbine(ioTHubDeviceID, on, time);
        }

        private static void ActionEnableWindowsWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(ioTHubDeviceID) +
                    " WindTurbine Enable=" + on +
                    ", Time=" + time,
                    ConsoleColor.Green);

            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_TURN_ONOFF;
            c2dCommand.value = on;
            c2dCommand.time = time;

            SendCloudToDeviceCommand(
                _serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionEnableLinuxWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(ioTHubDeviceID) +
                    " WindTurbine Enable=" + on +
                    ", Time=" + time,
                    ConsoleColor.DarkGreen);

            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_TURN_ONOFF;
            JObject jObj = new JObject();
            int onoff = on.Equals("1") ? 1 : 0;
            jObj.Add("On", onoff);
            c2dCommand.Parameters = jObj;

            SendCloudToDeviceLinuxCommand(
                _serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionResetDepreciation(string ioTHubDeviceID, string time)
        {
            if (ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                ActionResetDepreciationWindows(ioTHubDeviceID, time);
            else if (ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                ActionResetDepreciationLinux(ioTHubDeviceID, time);
        }

        private static void ActionResetDepreciationWindows(string ioTHubDeviceID, string time)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(ioTHubDeviceID) +
                    " Depreciation Reset!" +
                    ", Time=" + time,
                    ConsoleColor.Cyan);

            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_RESET_DEPRECIATION;
            c2dCommand.value = "1";// set it to 100%
            c2dCommand.time = time;

            SendCloudToDeviceCommand(
                _serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void ActionResetDepreciationLinux(string ioTHubDeviceID, string time)
        {
            WriteHighlightedMessage(
                    GetDeviceIdHint(ioTHubDeviceID) +
                    " Depreciation Reset!" +
                    ", Time=" + time,
                    ConsoleColor.DarkCyan);

            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_RESET_DEPRECIATION;
            JObject jObj = new JObject();
            jObj.Add("Depreciation", 1);
            c2dCommand.Parameters = jObj;

            SendCloudToDeviceLinuxCommand(
                _serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private async static Task SendCloudToDeviceCommand(ServiceClient serviceClient, String deviceId, C2DCommand command)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(command)));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private async static Task SendCloudToDeviceLinuxCommand(ServiceClient serviceClient, String deviceId, C2DCommandLinux command)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(command)));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private static void WriteHighlightedMessage(string message, System.ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static string GetDeviceIdHint(string ioTHubDeviceID)
        {
            return "[" + ioTHubDeviceID +" ("+ DateTime.UtcNow.ToString("MM-ddTHH:mm:ss") + ")"+ "]";
        }
    }
}
