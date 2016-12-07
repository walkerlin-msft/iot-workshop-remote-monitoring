using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServiceBusConsoleApp
{
    class Program
    {
        /* Service Bus */
        private const string ConnectionString = "[Please replace your connection string of Service Bus]";//"Endpoint=sb://iotworkshop112101.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=DcgYWbhb3ZXGoK1Y5/BWKu/U8mPZQNYcbZLUCWqgGow=";
        private const string QueueName = "cloud2device";// It should be fixed for this workshop

        /* IoT Hub */
        private const string iotHubConnectionString = "[Please replace your connection string of IoT Hub]";//"HostName=iothub112101.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=9plxivts9llrJWJaAakiU2wT8uvAvGrCr1MyiJXjlro=";
        private static ServiceClient serviceClient;

        private const string DEVICEID_WINDOWS_TURBINE = "WinTurbine";// It should be fixed for this workshop
        private const string DEVICEID_LINUX_TURBINE = "LinuxTurbine";// It should be fixed for this workshop

        /* Debug for Linux C# Simulator */
        private const bool useCSharpLinuxTurbine = false;

        static void Main(string[] args)
        {   
            Console.WriteLine("Alarm Service Bus is running!");

            // Queue
            var client = QueueClient.CreateFromConnectionString(ConnectionString, QueueName);

            serviceClient = ServiceClient.CreateFromConnectionString(iotHubConnectionString);

            client.OnMessage(message =>
            {
                Console.WriteLine("\n*******************************************************");
                string msg = message.GetBody<String>();
                try
                {
                    AlarmMessage alarmMessage = JsonConvert.DeserializeObject<AlarmMessage>(msg);

                    processAlarmMessage(alarmMessage);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("****  Exception=" + ex.Message);
                }


            });

            Console.ReadLine();
        }

        private static void processAlarmMessage(AlarmMessage alarmMessage)
        {
            switch(alarmMessage.alarmType)
            {
                case "CutOutSpeed":
                    actionCutOutSpeed(alarmMessage);
                    break;
                case "Repair":
                    actionRepair(alarmMessage);
                    break;
                case "EnableWindTurbine":
                    actionEnableWindTurbine(alarmMessage.ioTHubDeviceID, alarmMessage.reading, alarmMessage.createdAt);
                    break;
                case "ResetDepreciation":
                    actionResetDepreciation(alarmMessage.ioTHubDeviceID, alarmMessage.createdAt);
                    break;
                default:
                    Console.WriteLine("AlarmType is Not accpeted!");
                    break;
            }
        }

        private static void actionCutOutSpeed(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    alarmMessage.ioTHubDeviceID +
                    " is CutOutSpeed! Speed=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.Yellow);

            if (useCSharpLinuxTurbine | alarmMessage.ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                actionCutOutSpeedWindows(alarmMessage);
            else if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                actionCutOutSpeedLinux(alarmMessage);
        }

        private static void actionCutOutSpeedWindows(AlarmMessage alarmMessage)
        {
            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_CUTOUT_SPEED_WARNING;
            c2dCommand.value = alarmMessage.messageID;
            c2dCommand.time = alarmMessage.createdAt;

            sendCloudToDeviceCommand(
                serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void actionCutOutSpeedLinux(AlarmMessage alarmMessage)
        {
            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_CUTOUT_SPEED_WARNING;
            c2dCommand.Parameters = new JObject();

            sendCloudToDeviceLinuxCommand(
                serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }
        private static void actionRepair(AlarmMessage alarmMessage)
        {
            WriteHighlightedMessage(
                    alarmMessage.ioTHubDeviceID +
                    " is Repair! Depreciation=" + alarmMessage.reading +
                    ", MessageID=" + alarmMessage.messageID +
                    ", Threshold=" + alarmMessage.threshold,
                    ConsoleColor.Red);

            if (useCSharpLinuxTurbine | alarmMessage.ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                actionRepairWindows(alarmMessage);
            else if (alarmMessage.ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                actionRepairLinux(alarmMessage);
        }

        private static void actionRepairLinux(AlarmMessage alarmMessage)
        {
            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_REPAIR_WARNING;
            c2dCommand.Parameters = new JObject();

            sendCloudToDeviceLinuxCommand(
                serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void actionRepairWindows(AlarmMessage alarmMessage)
        {
            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_REPAIR_WARNING;
            c2dCommand.value = alarmMessage.messageID;
            c2dCommand.time = alarmMessage.createdAt;

            sendCloudToDeviceCommand(
                serviceClient,
                alarmMessage.ioTHubDeviceID,
                c2dCommand).Wait();
        }
        private static void actionEnableWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            WriteHighlightedMessage(
                    ioTHubDeviceID +
                    " set EnableWindTurbine ON=" + on +
                    ", Time=" + time,
                    ConsoleColor.Green);

            if (useCSharpLinuxTurbine | ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                actionEnableWindowsWindTurbine(ioTHubDeviceID, on, time);
            else if (ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                actionEnableLinuxWindTurbine(ioTHubDeviceID, on, time);
        }

        private static void actionEnableWindowsWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_TURN_ONOFF;
            c2dCommand.value = on;
            c2dCommand.time = time;

            sendCloudToDeviceCommand(
                serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void actionEnableLinuxWindTurbine(string ioTHubDeviceID, string on, string time)
        {
            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_TURN_ONOFF;
            JObject jObj = new JObject();
            int onoff = on.Equals("1") ? 1 : 0;
            jObj.Add("On", onoff);
            c2dCommand.Parameters = jObj;

            sendCloudToDeviceLinuxCommand(
                serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void actionResetDepreciation(string ioTHubDeviceID, string time)
        {
            WriteHighlightedMessage(
                    ioTHubDeviceID +
                    " reset the depreciation" +
                    ", Time=" + time,
                    ConsoleColor.Cyan);

            if (useCSharpLinuxTurbine | ioTHubDeviceID.Equals(DEVICEID_WINDOWS_TURBINE))
                actionResetDepreciationWindows(ioTHubDeviceID, time);
            else if (ioTHubDeviceID.Equals(DEVICEID_LINUX_TURBINE))
                actionResetDepreciationLinux(ioTHubDeviceID, time);
        }

        private static void actionResetDepreciationWindows(string ioTHubDeviceID, string time)
        {
            C2DCommand c2dCommand = new C2DCommand();
            c2dCommand.command = C2DCommand.COMMAND_RESET_DEPRECIATION;
            c2dCommand.value = "1";// set it to 100%
            c2dCommand.time = time;

            sendCloudToDeviceCommand(
                serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private static void actionResetDepreciationLinux(string ioTHubDeviceID, string time)
        {
            C2DCommandLinux c2dCommand = new C2DCommandLinux();

            c2dCommand.Name = C2DCommandLinux.COMMAND_RESET_DEPRECIATION;
            JObject jObj = new JObject();
            jObj.Add("Depreciation", 1);
            c2dCommand.Parameters = jObj;

            sendCloudToDeviceLinuxCommand(
                serviceClient,
                ioTHubDeviceID,
                c2dCommand).Wait();
        }

        private async static Task sendCloudToDeviceCommand(ServiceClient serviceClient, String deviceId, C2DCommand command)
        {
            var commandMessage = new Message(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(command)));
            await serviceClient.SendAsync(deviceId, commandMessage);
        }

        private async static Task sendCloudToDeviceLinuxCommand(ServiceClient serviceClient, String deviceId, C2DCommandLinux command)
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
    }
}
