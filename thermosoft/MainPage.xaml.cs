using System;
using System.ComponentModel;
using thermosoft.Helpers;
using thermosoft.Models;
using thermosoft.RestClient;
using thermosoft.ViewModels;
using thermosoft.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]

  
    public partial class MainPage : ContentPage
    {
        public static int FlagTmer;
        public int FlagSocketClose;

        // Cache dla RestClient'a - unikanie wielokrotnych alokacji
        private RestClient<Employee> _restClient;
        private IDispatcherTimer _pingTimer;

        public MainPage()
        {
            InitializeComponent();

            // Cache RestClient'a
            _restClient = new RestClient<Employee>();

            EmployeesListView.RefreshCommand = new Command(() => {
                var vm = BindingContext as MainViewModel;
                vm?.InitializeDataAsync();
            });

            (App.Current as App).OnResumeHandler += Handle_OnResumeHandler;
            (App.Current as App).OnSleepHandler += Handle_OnSleepHandler;

            // Zamiana Device.StartTimer na IDispatcherTimer (MAUI)
            _pingTimer = Application.Current.Dispatcher.CreateTimer();
            _pingTimer.Interval = TimeSpan.FromSeconds(22);
            _pingTimer.Tick += (s, e) => OnTimerTick();
            _pingTimer.Start();
        }

        private void OnTimerTick()
        {
            try
            {
                _restClient?.PingWebsocket();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TIMER] Error in PingWebsocket: {ex.Message}");
            }
        }

        async void Handle_OnResumeHandler(object sender, EventArgs e)
        {
            // Oczekiwanie, aż systemowy mostek sieciowy (szczególnie Android/iOS) wstanie po wybudzeniu
            await Task.Delay(500);
            AppiStart();
        }

        void Handle_OnSleepHandler(object sender, EventArgs e)
        {
            AppiStop();
        }

        protected override void OnAppearing()
        {
            AppiStart();
        }

        public void AppiStop()
        {
            if (FlagSocketClose == 0)
            {
                _restClient?.CloseWebsocket();
            }
        }

        public async void AppiStart()
        {
            var vm = BindingContext as MainViewModel;
            if (vm == null)
                return;

            if (MainViewModel.flaga_listy == 0)
            {
                if (await vm.Login() == true)
                {
                    vm?.InitializeDataAsync();
                }
            }

            if (MainViewModel.flaga_listy == 1)
            {
                await vm.zmien_temAsync(1);
            }

            MainViewModel.flaga_listy = 0;
            Title = Settings.Username;
        }

        private async void ListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var employee_selected = EmployeesListView.SelectedItem as Employee;

            if (employee_selected == null)
                return;

            var mainViewModel = BindingContext as MainViewModel;
            if (mainViewModel == null)
                return;

            mainViewModel.SelectedEmployee = employee_selected;
            MainViewModel.flaga_listy = 1;
            FlagSocketClose = 1;

            NewEmployeePage.SharedViewModel = mainViewModel;
            await Shell.Current.GoToAsync(nameof(Views.NewEmployeePage));

            FlagSocketClose = 0;
            EmployeesListView.SelectedItem = null;
        }

        private void Odswierz_Clicked(object sender, EventArgs e)
        {
            var vm = BindingContext as MainViewModel;
            vm?.InitializeDataAsync();
        }
    }
}
