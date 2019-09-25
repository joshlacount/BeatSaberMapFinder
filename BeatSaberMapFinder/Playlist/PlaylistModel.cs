using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BeatSaberMapFinder
{
    public class PlaylistModel : ObservableObject
    {
        #region Fields

        private string _playlistName;
        private List<SongModel> _songs = new List<SongModel>();

        #endregion // Fields

        #region Properties

        public string PlaylistName
        {
            get { return _playlistName; }
            set
            {
                if (value != _playlistName)
                {
                    _playlistName = value;
                    OnPropertyChanged("PlaylistName");
                }
            }
        }

        public List<SongModel> Songs
        {
            get { return _songs; }
            set
            {
                if (value != _songs)
                {
                    _songs = value;
                    OnPropertyChanged("Songs");
                }
            }
        }

        public ObservableCollection<SongModel> SongCollection
        {
            get { return new ObservableCollection<SongModel>(_songs); }
        }

        #endregion // Properties
    }
}
