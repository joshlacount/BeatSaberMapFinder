using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace BeatSaberMapFinder
{
    public class SongModel : ObservableObject
    {
        #region Fields

        private string _songTitle;
        private List<string> _songArtists;
        private string _songID;
        private List<BeatsaverMap> _mapMatches;
        private ObservableCollection<BeatsaverMap> _mapMatchesCollection;

        #endregion

        public SongModel()
        {
            this.PropertyChanged += SongModel_PropertyChanged;
        }

        private void SongModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        #region Properties

        public string SongTitle
        {
            get { return _songTitle; }
            set
            {
                if (value != _songTitle)
                {
                    _songTitle = value;
                    OnPropertyChanged("SongTitle");
                }
            }
        }

        public List<string> SongArtists
        {
            get { return _songArtists; }
            set
            {
                if (value != _songArtists)
                {
                    _songArtists = value;
                    OnPropertyChanged("SongArtists");
                }
            }
        }

        public string SongID
        {
            get { return _songID; }
            set
            {
                if (value != _songID)
                {
                    _songID = value;
                    OnPropertyChanged("SongID");
                }
            }
        }

        public List<BeatsaverMap> MapMatches
        {
            get { return _mapMatches; }
            set
            {
                if (value != _mapMatches)
                {
                    _mapMatches = value;
                    MapMatchesCollection = new ObservableCollection<BeatsaverMap>(_mapMatches);
                    OnPropertyChanged("MapMatches");
                }
            }
        }

        public ObservableCollection<BeatsaverMap> MapMatchesCollection
        {
            get { return _mapMatchesCollection; }
            set
            {
                if (value != _mapMatchesCollection)
                {
                    _mapMatchesCollection = value;
                    OnPropertyChanged("MapMatchesCollection");
                }
            }
        }

        #endregion
    }
}
