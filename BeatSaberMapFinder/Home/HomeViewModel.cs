using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using TinyIoC;

namespace BeatSaberMapFinder
{
    public class HomeViewModel : ObservableObject, IPageViewModel
    {
        private string _beatSaberInstallFolder;

        private HomeState _currentHomeState;
        private ICommand _spotifySignInCommand;
        private ICommand _selectInstallFolderCommand;

        public HomeViewModel()
        {
            EventSystem.Subscribe<SpotifyAccessTokenMessage>(a => CurrentHomeState = HomeState.SignedIn);
            EventSystem.Subscribe<SpotifySignInMessage>(a => CurrentHomeState = HomeState.SignInOptions);
            Load();
        }

        public string Name
        {
            get { return "Home"; }
        }

        public string BeatSaberInstallFolder
        {
            get { return _beatSaberInstallFolder; }
            set
            {
                if (value != _beatSaberInstallFolder)
                {
                    _beatSaberInstallFolder = value;
                    BeatsaverAPI.BeatSaberInstallFolder = value;
                    Save();
                    OnPropertyChanged("BeatSaberInstallFolder");
                }
            }
        }

        public HomeState CurrentHomeState
        {
            get { return _currentHomeState; }
            set
            {
                if (value != _currentHomeState)
                {
                    _currentHomeState = value;
                    OnPropertyChanged("CurrentHomeState");
                }
            }
        }

        public ICommand SpotifySignInCommand
        {
            get
            {
                if (_spotifySignInCommand == null)
                {
                    _spotifySignInCommand = new RelayCommand(p => SpotifySignIn());
                }

                return _spotifySignInCommand;
            }
        }

        public ICommand SelectInstallFolderCommand
        {
            get
            {
                if (_selectInstallFolderCommand == null)
                {
                    _selectInstallFolderCommand = new RelayCommand(p => SelectInstallFolder());
                }

                return _selectInstallFolderCommand;
            }
        }

        private void SpotifySignIn()
        {
            CurrentHomeState = HomeState.SignIn;
            SpotifyAPI.GetNewTokens();
        }

        private void SelectInstallFolder()
        {
            var dlg = new FolderBrowserDialog();
            DialogResult result = dlg.ShowDialog();
            BeatSaberInstallFolder = dlg.SelectedPath;
        }

        void Load()
        {
            var container = TinyIoCContainer.Current;

            IConfigProvider service = container.Resolve<IConfigProvider>("APP_SETTINGS");

            service.Load();

            this.BeatSaberInstallFolder = service.BeatSaberInstallFolder;
        }

        void Save()
        {
            var container = TinyIoCContainer.Current;

            IConfigProvider service = container.Resolve<IConfigProvider>("APP_SETTINGS");

            service.BeatSaberInstallFolder = this.BeatSaberInstallFolder;

            service.Save();
        }
    }

    public enum HomeState
    {
        SignInOptions,
        SignIn,
        SignedIn
    }
}
