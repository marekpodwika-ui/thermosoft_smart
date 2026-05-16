using System;
using thermosoft.Data;
using System.Threading;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft
{
    public partial class App : Application
    {
        static TodoItemDatabase database;


        public EventHandler OnResumeHandler;
        public EventHandler OnSleepHandler;

        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[UNHANDLED] {e.ExceptionObject}");
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // Kiedy aplikacja chowa się do tła (minimalizacja)
            window.Deactivated += async (s, e) =>
            {
                var restClient = new thermosoft.RestClient.RestClient<object>();
                await restClient.CloseWebsocket();
            };

            // Kiedy aplikacja jest całkowicie zamykana ("zabijana")
            window.Destroying += async (s, e) =>
            {
                var restClient = new thermosoft.RestClient.RestClient<object>();
                await restClient.CloseWebsocket();
            };

            return window;
        }

        public static TodoItemDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new TodoItemDatabase();
                }
                return database;
            }
        }






        protected override void OnStart() 
        {
            //  var mp = BindingContext as MainPage;
            //  mp.AppiStart();
          


        }

        protected override void OnSleep()
        {
            thermosoft.RestClient.RestClientBase.IsAppInForeground = false;
#if __IOS__
             //   Thread.CurrentThread.Abort();
#endif
            OnSleepHandler?.Invoke(null, new EventArgs());
        }

        protected override void OnResume()
        {
            thermosoft.RestClient.RestClientBase.IsAppInForeground = true;
            thermosoft.RestClient.RestClientBase.LastResumeTime = DateTime.Now;
            OnResumeHandler?.Invoke(null, new EventArgs());
        }

       
    }
}
