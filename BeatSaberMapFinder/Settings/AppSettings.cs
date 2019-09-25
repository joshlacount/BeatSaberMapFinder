using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMapFinder
{
    sealed class AppSettings : ApplicationSettingsBase
    {
        [UserScopedSetting()]
        public string BeatSaberInstallFolder
        {
            get { return (string)(this["BeatSaberInstallFolder"]); }
            set { this["BeatSaberInstallFolder"] = value; }
        }
    }
}
