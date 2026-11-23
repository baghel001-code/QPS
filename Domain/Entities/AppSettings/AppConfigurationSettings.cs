using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.AppSettings
{
    public class AppConfigurationSettings
    {
        public int MaxSessionTime { get; set; }
        public bool ShowArchivePublic { get; set; }
        public int ProxyValue { get; set; }
        public string Version { get; set; } = string.Empty;
        public string App { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public int AdminHours { get; set; }
        public int UserRevalidateInSeconds { get; set; }
    }
}
