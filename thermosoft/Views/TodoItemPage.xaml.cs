using System;
using thermosoft.Models;
using thermosoft.Helpers;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft.Views
{
    public partial class TodoItemPage : ContentPage
    {
        public TodoItemPage()
        {
            InitializeComponent();

         
          
            if (Switch_.IsToggled == true)
            {
                Przelacznik.Text = "HostWeb";
            }
            else
            {

                Przelacznik.Text = "Adres:port";
            }
        }

        async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Nazwa.Text))
            {
                await DisplayAlert("błąd", "Brak Nazwy", "Ok");
                Nazwa.Focus();
                return;
            }

            if (string.IsNullOrEmpty(AdresPort.Text) && Switch_.IsToggled == false)
            {
                await DisplayAlert("błąd", "Brak Adres:port", "Ok");
                AdresPort.Focus();
                return;
            }

            if (string.IsNullOrEmpty(Serwer.Text) && Switch_.IsToggled == true)
            {
                await DisplayAlert("błąd", "Brak Serwer", "Ok");
                Serwer.Focus();
                return;
            }

            if (string.IsNullOrEmpty(HostWeb.Text) && Switch_.IsToggled == true)
            {
                await DisplayAlert("błąd", "Brak Hosta", "Ok");
                Serwer.Focus();
                return;
            }

            if (string.IsNullOrEmpty(Haslo.Text))
            {
                await DisplayAlert("błąd", "Brak Haslo", "Ok");
                Haslo.Focus();
                return;
            }




            var todoItem = (TodoItem)BindingContext;
            await App.Database.SaveItemAsync(todoItem);
            await Navigation.PopAsync();
        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            var todoItem = (TodoItem)BindingContext;
            await App.Database.DeleteItemAsync(todoItem);
            await Navigation.PopAsync();
        }

        async void OnCancelClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            if (Switch_.IsToggled == true)
            {
                Przelacznik.Text = "HostWeb";
            }
            else
            {
                Przelacznik.Text = "Adres:port";
            }
        }
    }
}