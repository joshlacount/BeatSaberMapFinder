using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMapFinder
{
    interface IConfigProvider
    {
        string BeatSaberInstallFolder { get; set; }

        void Save();
        void Load();
    }
}
