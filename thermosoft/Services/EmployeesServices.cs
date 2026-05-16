using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using thermosoft.Models;
using thermosoft.RestClient;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Networking;

namespace thermosoft.Services
{
    public class EmployeesServices
    {
        enum WebError : byte { oK,Powtusz,BrakSterownika }
        public async Task<ObservableCollection<Employee>> GetEmployeesAsync(bool SereweLubSolet) // zapytanie list
        {
            RestClient<Employee> restClient = new RestClient<Employee>();

            var current = Connectivity.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                await Application.Current.MainPage.DisplayAlert("błąd", "brak inernetu", "OK");
                restClient.CloseWebsocket();
                return null;
            }



            if (SereweLubSolet == true)
            {
                var employeesList = await restClient.GetAsyncWs();


                WebError webError = (WebError)restClient.ErrorStatus();

                switch (webError)
                {
                    case WebError.oK:

                        break;
                    case WebError.Powtusz:
                        employeesList = await restClient.GetAsyncWs();
                        webError = (WebError)restClient.ErrorStatus();
                        if (webError != WebError.oK)
                        {
                            await Application.Current.MainPage.DisplayAlert("błąd", "Brak połączenia z serwerem", "OK");
                        }
                        break;
                    case WebError.BrakSterownika:
                        await Application.Current.MainPage.DisplayAlert("błąd", "Brak Sterownika", "OK");
                        break;
                    default:
                        break;
                }

                return employeesList;
            }
            else
            {
                var employeesList = await restClient.GetAsyncTcp2();

                WebError webError = (WebError)restClient.ErrorStatus();

                switch (webError)
                {
                    case WebError.oK:
                        break;
                    case WebError.Powtusz: // Przy TCP uderzy raz jeszcze
                        employeesList = await restClient.GetAsyncTcp2();
                        webError = (WebError)restClient.ErrorStatus();
                        if (webError != WebError.oK)
                        {
                            await Application.Current.MainPage.DisplayAlert("Błąd TCP", "Brak połączenia z urządzeniem po wybudzeniu", "OK");
                        }
                        break;
                    default:
                        break;
                }

                return employeesList;
            }


        }



        public async Task<Employee> GetEmployeesSelekt(bool SereweLubSolet, Employee employee) // strafa
        {
            RestClient<Employee> restClient = new RestClient<Employee>();

            var current = Connectivity.NetworkAccess;

            if (current != NetworkAccess.Internet)
            {
                await Application.Current.MainPage.DisplayAlert("błąd", "brak inernetu", "OK");
                restClient.CloseWebsocket();
                return null;
            }
            //  else
            //  {
            //      await Application.Current.MainPage.DisplayAlert("błąd", " inernetu", "OK");
            //  }


          

            if (SereweLubSolet == true)
            {
                var employeesList = await restClient.GetAsyncSelektWs(employee);

                WebError webError = (WebError)restClient.ErrorStatus();

                switch (webError)
                {
                    case WebError.oK:
                        break;
                    case WebError.Powtusz:
                        employeesList = await restClient.GetAsyncSelektWs(employee);
                        webError = (WebError)restClient.ErrorStatus();
                        if (webError != WebError.oK)
                        {
                            await Application.Current.MainPage.DisplayAlert("błąd", "Brak połączenia z serwerem", "OK");
                        }
                        break;
                    case WebError.BrakSterownika:
                        await Application.Current.MainPage.DisplayAlert("błąd", "Brak Sterownika", "OK");
                        break;
                    default:
                        break;
                }

                return employeesList;
            }
            else
            {
                var employeesList = await restClient.GetAsyncSelektTcp2(employee);

                WebError webError = (WebError)restClient.ErrorStatus();

                switch (webError)
                {
                    case WebError.oK:
                        break;
                    case WebError.Powtusz:
                        employeesList = await restClient.GetAsyncSelektTcp2(employee);
                        webError = (WebError)restClient.ErrorStatus();
                        if (webError != WebError.oK)
                        {
                            await Application.Current.MainPage.DisplayAlert("Błąd TCP", "Brak połączenia z urządzeniem po wybudzeniu", "OK");
                        }
                        break;
                    default:
                        break;
                }

                return employeesList;
            }


        }

    }
}
