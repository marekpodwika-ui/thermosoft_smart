using thermosoft.ViewModels;
using thermosoft.Views;

namespace thermosoft
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(NewEmployeePage), typeof(NewEmployeePage));
            Routing.RegisterRoute(nameof(ListViewPage1), typeof(ListViewPage1));
            Routing.RegisterRoute(nameof(TodoListPage), typeof(TodoListPage));
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = false;
            await Shell.Current.GoToAsync(nameof(TodoListPage));
        }
    }
}
