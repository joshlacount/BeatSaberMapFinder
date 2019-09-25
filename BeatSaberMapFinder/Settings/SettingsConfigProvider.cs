using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMapFinder
{
    class SettingsConfigProvider : IConfigProvider
    {
        public string BeatSaberInstallFolder { get; set; }

        public void Load()
        {
            AppSettings settings = AppSettingsLocator.Instance;

            this.BeatSaberInstallFolder = settings.BeatSaberInstallFolder;
        }

        public void Save()
        {
            AppSettings settings = AppSettingsLocator.Instance;

            settings.BeatSaberInstallFolder = this.BeatSaberInstallFolder;
            settings.Save();
        }
    }
}
