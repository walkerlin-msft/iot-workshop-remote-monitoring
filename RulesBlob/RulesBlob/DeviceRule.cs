using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RulesBlobConsoleApp
{
    class DeviceRule
    {
        public string DeviceID { get; set; }
        public double CutOutSpeed { get; set; }
        public double Repair { get; set; }
        public double Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
