using System;
using thermosoft.Models;
using thermosoft.Helpers;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft.Views
{
    public partial class TodoListPage : ContentPage
    {
        public TodoListPage()
        {
            InitializeComponent();
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            listView.ItemsSource = await App.Database.GetItemsAsync();




        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {

            var rta = await DisplayAlert("Komunikat", "Usunąć?", "Tak", "Nie");
            if (!rta)
            {
                return;
            }

            MenuItem item = sender as MenuItem;
            await App.Database.DeleteItemAsync(item.BindingContext as TodoItem);
            listView.ItemsSource = await App.Database.GetItemsAsync();
        }

        async void OnEditClicked(object sender, EventArgs e)
        {
          

            MenuItem item = sender as MenuItem;

            if (item.BindingContext != null)
            {
                await Navigation.PushAsync(new TodoItemPage
                {
                    BindingContext = item.BindingContext as TodoItem
                });
            }


        }

        async void OnItemAdded(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new TodoItemPage
            {
                BindingContext = new TodoItem()
            });
        }

        async void OnListItemSelected(object sender, SelectedItemChangedEventArgs e)
        {



            TodoItem i = (TodoItem)e.SelectedItem;
             
            Settings.Swit_stan = i.Server_Port.ToString();
            Settings.Username = i.Name;
            Settings.Serwer = i.SerwerIdetyfikator;
            Settings.Adres = i.AdresPort;
            Settings.Password = i.Haslo;
            Settings.HostWebsoket = i.HostWebsoket;




            //  await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("//MainPage");



        }
    }
}