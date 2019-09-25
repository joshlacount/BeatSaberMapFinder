using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BeatSaberMapFinder
{
    public class PlaylistViewModel : ObservableObject, IPageViewModel
    {
        #region Fields

        private int _playlistName;
        private ObservableCollection<SongModel> _songs = new ObservableCollection<SongModel>();
        private SongModel _selectedSong;
        private IList<PlaylistModel> _playlists = new List<PlaylistModel>();
        private PlaylistModel _selectedPlaylist;
        private BeatsaverMap _selectedMap;
        private string _comboDefaultText = "Select playlist";
        private string _matchStatus;

        private ICommand _matchSingleSongCommand;
        private ICommand _matchPlaylistCommand;
        private ICommand _downloadSingleMapCommand;
        private ICommand _downloadPlaylistMatchesCommand;

        private string _selectedPlaylistCopy;
        private string _selectedSongCopy;
        private bool _preserveSong = false;

        #endregion

        #region Constructor

        public PlaylistViewModel()
        {
            this.PropertyChanged += PlaylistViewModel_PropertyChanged;
            EventSystem.Subscribe<SpotifyAccessTokenMessage>(a => Task.Run(UpdateComboBox));
            //Playlists = SpotifyAPI.GetPlaylists(true);

            //if (_playlists.Count > 0)
                //SelectedPlaylist = _playlists[0];
            //EventSystem.Publish<PlaylistsFromLocalMessage>();
            //Console.WriteLine("Local load sent");
        }

        private void PlaylistViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Playlists":
                    foreach (var p in _playlists)
                    {
                        if (p == null)
                            continue;

                        if (p.PlaylistName == _selectedPlaylistCopy)
                            SelectedPlaylist = p;
                    }
                    break;

                case "SelectedSong":
                    if (_preserveSong)
                    {
                        if (_selectedSongCopy != null)
                        {
                            foreach (var s in _selectedPlaylist.Songs)
                            {
                                if (s.SongID == _selectedSongCopy)
                                    SelectedSong = s;
                            }
                        }
                        _preserveSong = false;
                    }

                    MatchStatus = "";
                    break;

                case "SelectedPlaylist":
                    MatchStatus = "";
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region Properties/Commands

        public string Name
        {
            get { return "Playlist"; }
        }

        public int PlaylistName
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

        public ObservableCollection<SongModel> Songs
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

        public SongModel SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                if (value != _selectedSong)
                {
                    if (_selectedSong != null)
                        _selectedSongCopy = _selectedSong.SongID;
                    _selectedSong = value;
                    OnPropertyChanged("SelectedSong");
                }
            }
        }

        public BeatsaverMap SelectedMap
        {
            get { return _selectedMap; }
            set
            {
                if (value != _selectedMap)
                {
                    _selectedMap = value;
                    OnPropertyChanged("SelectedMap");
                }
            }
        }

        public IList<PlaylistModel> Playlists
        {
            get { return _playlists; }
            set
            {
                if (value != _playlists)
                {
                    _playlists = value;
                    _preserveSong = true;
                    OnPropertyChanged("Playlists");
                }
            }
        }

        public PlaylistModel SelectedPlaylist
        {
            get { return _selectedPlaylist; }
            set
            {
                if (value != _selectedPlaylist)
                {
                    _selectedPlaylist = value;
                    _selectedPlaylistCopy = value.PlaylistName;
                    OnPropertyChanged("SelectedPlaylist");
                }
            }
        }

        public string ComboDefaultText
        {
            get { return _comboDefaultText; }
            set
            {
                if (value != _comboDefaultText)
                {
                    _comboDefaultText = value;
                    OnPropertyChanged("ComboDefaultText");
                }
            }
        }

        public string MatchStatus
        {
            get { return _matchStatus; }
            set
            {
                if (value != _matchStatus)
                {
                    _matchStatus = value;
                    OnPropertyChanged("MatchStatus");
                }
            }
        }

        public ICommand MatchSingleSongCommand
        {
            get
            {
                if (_matchSingleSongCommand == null)
                    _matchSingleSongCommand = new RelayCommand(p => MatchSingleSong());

                return _matchSingleSongCommand;
            }
        }

        public ICommand MatchPlaylistCommand
        {
            get
            {
                if (_matchPlaylistCommand == null)
                    _matchPlaylistCommand = new RelayCommand(p => MatchPlaylist());

                return _matchPlaylistCommand;
            }
        }

        public ICommand DownloadSingleMapCommand
        {
            get
            {
                if (_downloadSingleMapCommand == null)
                    _downloadSingleMapCommand = new RelayCommand(p => DownloadSingleMap());

                return _downloadSingleMapCommand;
            }
        }

        public ICommand DownloadPlaylistMatchesCommand
        {
            get
            {
                if (_downloadPlaylistMatchesCommand == null)
                    _downloadPlaylistMatchesCommand = new RelayCommand(p => DownloadPlaylistMatches());

                return _downloadPlaylistMatchesCommand;
            }
        }

        #endregion

        #region Methods

        private void UpdateComboBox()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ComboDefaultText = "Updating playlists...";
            var updatedPlaylists = SpotifyAPI.GetPlaylists();
            Playlists = updatedPlaylists;
            ComboDefaultText = "Select Playlist";
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
        }

        private void MatchSingleSong()
        {
            MatchStatus = "Finding maps...";
            Task.Run(() =>
            {
                SelectedSong.MapMatches = BeatsaverAPI.FindMapsSingleSong(SelectedSong);
                if (SelectedSong.MapMatches.Count > 0)
                    MatchStatus = "Maps found!";
                else
                    MatchStatus = "No maps found";
            });
        }

        private void MatchPlaylist()
        {
            MatchStatus = "Finding maps...";
            Task.Run(() =>
            {
                var matches = BeatsaverAPI.FindMapsPlaylist(SelectedPlaylist);
                foreach (var song in SelectedPlaylist.Songs)
                {
                    if (song.SongID != null) //TODO: handle local songs that don't have a spotify id
                    {
                        if (matches.ContainsKey(song.SongID))
                        {
                            song.MapMatches = matches[song.SongID];
                            Console.WriteLine(song.MapMatches.Count);
                        }
                    }
                }

                if (matches.Count > 0)
                    MatchStatus = "Maps found!";
                else
                    MatchStatus = "No maps found";
            });
        }

        private void DownloadSingleMap()
        {
            if (SelectedMap != null)
                Task.Run(() => BeatsaverAPI.DownloadMap(SelectedMap.Key, SelectedMap.Metadata.SongName, SelectedMap.Metadata.LevelAuthorName));
        }

        private void DownloadPlaylistMatches()
        {
            Task.Run(() => BeatsaverAPI.DownloadMaps(SelectedPlaylist));
        }

        #endregion
    }

    public class PlaylistsFromLocalMessage
    { }

    public class PlaylistsUpdated
    { }
}
