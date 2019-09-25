using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using TinyIoC;

namespace BeatSaberMapFinder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApplicationView app = new ApplicationView();
            ApplicationViewModel context = new ApplicationViewModel();
            app.DataContext = context;
            app.Show();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var container = TinyIoCContainer.Current;
            // Register config
            container.Register<IConfigProvider>((c, n) =>
            {
                IConfigProvider config = new SettingsConfigProvider();
                return config;
            }, "APP_SETTINGS");
        }
    }
}
