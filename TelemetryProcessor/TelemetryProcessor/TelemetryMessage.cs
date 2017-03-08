using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryEPHostConsoleApp
{
    class TelemetryMessage
    {
        public string deviceId { get; set; }
        public string msgId { get; set; }
        public double speed { get; set; }
        public double depreciation { get; set; }
        public double power { get; set; }
        public string time { get; set; }
    }
}
