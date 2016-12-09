using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace paas_demo.Models
{
    public class AlarmMessage
    {
        public string ioTHubDeviceID { get; set; }
        public string messageID { get; set; }
        public string alarmType { get; set; }
        public string reading { get; set; }
        public string threshold { get; set; }
        public string localTime { get; set; }
        public string createdAt { get; set; }
    }
}