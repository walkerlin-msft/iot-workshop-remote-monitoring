using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimulatedWindTurbine
{
    class C2DCommand
    {
        public const string COMMAND_CUTOUT_SPEED_WARNING = "CUTOUT_SPEED_WARNING";
        public const string COMMAND_REPAIR_WARNING = "REPAIR_WARNING";
        public const string COMMAND_TURN_ONOFF = "TURN_ONOFF";
        public const string COMMAND_RESET_DEPRECIATION = "RESET_DEPRECIATION";

        public string command { get; set; }
        public string value { get; set; }
        public string time { get; set; }        
    }
}
