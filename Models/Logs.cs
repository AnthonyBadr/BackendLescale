using System;

namespace backend.Models
{
    public class Logs
    {
        public string action { get; set; }
        public string description { get; set; }
        public string bywho { get; set; }
        public string date { get; set; }  // Use DateTime to store date and time
    }
}
