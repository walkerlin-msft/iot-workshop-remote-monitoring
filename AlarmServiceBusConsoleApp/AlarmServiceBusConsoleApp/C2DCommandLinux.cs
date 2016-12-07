using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlarmServiceBusConsoleApp
{
    class C2DCommandLinux
    {
        public const string COMMAND_CUTOUT_SPEED_WARNING = "CutoutSpeedWarning";
        public const string COMMAND_REPAIR_WARNING = "RepairWarning";
        public const string COMMAND_TURN_ONOFF = "TurnOnOff";
        public const string COMMAND_RESET_DEPRECIATION = "ResetDepreciation";
        public string Name { get; set; }
        public JObject Parameters { get; set; }
    }
}
