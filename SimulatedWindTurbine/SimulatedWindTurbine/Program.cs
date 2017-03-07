using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Configuration;

namespace SimulatedWindTurbine
{
    class Program
    {
        private const string DEVICENAME = "WinTurbine";// It's hard-coded for this workshop
        private static DeviceClient _deviceClient;
        private static bool _isStopped = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Simulated Wind Turbine\n");

            // String containing Hostname, Device Id & Device Key in one of the following formats:
            //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
            string deviceConnectionString = ConfigurationManager.AppSettings["DeviceConnectionString"];
            Console.WriteLine("deviceConnectionString={0}\n", deviceConnectionString);

            try
            {
                /* Create the DeviceClient instance */
                _deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);

                /* Task for sending message */
                sendWindTurbineMessageToCloudAsync();

                /* Task for receiving message */
                receiveCloudToDeviceMessageAsync();

            } catch (FormatException ex)
            {
                Console.WriteLine("Please make sure you have pasted the correct connection string of IoT Hub!!\n\n FormatException={0}", ex.ToString());
            }


            /* Wait for any key to terminate the console App */
            Console.ReadLine();
        }

        const double MAXIMUM_DEPRECIATION = 1;
        const double MINIMUM_DEPRECIATION = 0.3;
        const double DEPRECIATION_RATE = 0.01;
        private static double _currentDepreciation { get; set; }
        private static async void sendWindTurbineMessageToCloudAsync()
        {
            _currentDepreciation = MAXIMUM_DEPRECIATION; // 100%
            int minWindSpeed = 2; // m/s
            Random rand = new Random();

            int i = 1;
            while (true)
            {
                if(_isStopped == false)
                {
                    int currentWindSpeed = minWindSpeed + (rand.Next() % 19);// 2~20
                    calculateNewDepreciation(i);
                    double currentWindPower = getWindPower(currentWindSpeed, _currentDepreciation);

                    var telemetryDataPoint = new
                    {
                        deviceId = DEVICENAME,
                        msgId = "message id " + i,
                        speed = currentWindSpeed,
                        depreciation = _currentDepreciation,
                        power = currentWindPower,
                        time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") // ISO8601 format, https://zh.wikipedia.org/wiki/ISO_8601
                    };

                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));
                    message.Properties.Add("msgType", "Telemetry");
                    await _deviceClient.SendEventAsync(message);
                    Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                    i++;
                } else
                {
                    Console.WriteLine("{0} > Turn Off", DateTime.Now);
                }
                
                Task.Delay(5000).Wait();
            }
        }

        /* Simulate the real wind power */
        private static double getWindPower(int speed, double depreciation)
        {
            if (speed <= 3)
                return 0;
            else if (speed <= 7)
                return (speed - 3) * 50 * depreciation;
            else if (speed <= 9)
                return (speed - 7) * 100 * depreciation + 200;
            else if (speed < 12)
                return (speed - 9) * 200 * depreciation + 400;
            else
                return 1000 * depreciation;
        }

        private static void calculateNewDepreciation(int i)
        {
            if (i % 5 == 0)
            {
                _currentDepreciation -= DEPRECIATION_RATE;
            }

            if (_currentDepreciation < MINIMUM_DEPRECIATION)
                _currentDepreciation = MINIMUM_DEPRECIATION;
        }

        private static async void receiveCloudToDeviceMessageAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                Message receivedMessage = await _deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;// It returns null after a specifiable timeout period (in this case, the default of one minute is used)

                string msg = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                C2DCommand c2dCommand = JsonConvert.DeserializeObject<C2DCommand>(msg);

                processCommand(c2dCommand);

                await _deviceClient.CompleteAsync(receivedMessage);
            }
        }    
        
        private static void processCommand(C2DCommand c2dCommand)
        {
            switch(c2dCommand.command)
            {
                case C2DCommand.COMMAND_CUTOUT_SPEED_WARNING:
                    displayReceivedCommand(c2dCommand, ConsoleColor.Yellow);
                    break;
                case C2DCommand.COMMAND_REPAIR_WARNING:
                    displayReceivedCommand(c2dCommand, ConsoleColor.Red);
                    break;
                case C2DCommand.COMMAND_TURN_ONOFF:
                    displayReceivedCommand(c2dCommand, ConsoleColor.Green);
                    _isStopped = c2dCommand.value.Equals("0"); // 0 means turn the machine off, otherwise is turning on.
                    break;
                case C2DCommand.COMMAND_RESET_DEPRECIATION:
                    displayReceivedCommand(c2dCommand, ConsoleColor.Cyan);
                    try
                    {
                        _currentDepreciation = Convert.ToDouble(c2dCommand.value);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Unable to convert '{0}' to a Double. \n\nException={1}", c2dCommand.value, ex.ToString());
                    }
                    
                    break;
                default:
                    Console.WriteLine("IT IS NOT A SUPPORTED COMMAND!");
                    break;
            }
            
        }    

        private static void displayReceivedCommand(C2DCommand c2dCommand, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("Received message: {0}, {1}, {2}\n", c2dCommand.command, c2dCommand.value, c2dCommand.time);
            Console.ResetColor();
        }
    }
}
