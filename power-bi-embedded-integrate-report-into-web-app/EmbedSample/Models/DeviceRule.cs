using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace paas_demo.Models
{
    public class DeviceRule
    {
        public string DeviceID { get; set; }
        public double CutOutSpeed { get; set; }
        public double Repair { get; set; }
        public double Altitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}