using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using thermosoft.Helpers;
using thermosoft.Models;
using thermosoft.RestClient;
using thermosoft.Services;
using thermosoft.UI;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {

        internal static bool SereweLubSolet__;
      //  private static string Serwer__;
       // private static string Adres__;


        public static int flaga_listy;

        public ObservableCollection<Employee> _employeesList;
        private Employee _selectedEmployee = new Employee();
        private Employee _selectedEmployeeUpdate;  // do zmiany zawartosci listy
                                                   //   private static  Employee _selectedEmployeePrzedZmiana ;  // do zmiany zawartosci listy


        private static string zaddana;
        private static string St;
        private static string Na;
        //  public Employee _selectedEmployee = new Employee();

        private bool _isBusy;
        private bool _isBusy_Button;
        private bool _IsBusy_Lista;

        //CopyTo***********************************************
        public ObservableCollection<EmploCopyTo> _employeesListCopyTo;
        private EmploCopyTo _selectedEmployeeCopyTo = new EmploCopyTo();
        private EmploCopyTo _selectedEmployeeUpdateCopyTo;  // do zmiany zawartosci listy



       //**********************************************************


        public ObservableCollection<Employee> EmployeesList
        {
            get { return _employeesList; }
            set
            {

                _employeesList = value;
                OnPropertyChanged();

            }
        }

        public Employee SelectedEmployee       // zaladuj dane z listy
        {
            get { return _selectedEmployee; }
            set
            {



                _selectedEmployee = value;
                OnPropertyChanged();

                _selectedEmployeeUpdate = _selectedEmployee;                // przypisanie id listy
                                                                            // _selectedEmployeeUpdate.Za = _selectedEmployee.Za;        // przypisanie id listy
                zaddana = _selectedEmployee.Za;
                St = _selectedEmployee.St;
                Na = _selectedEmployee.Na;
                // this
                //  _selectedEmployeePrzedZmiana.Za = value.Za;
                IsBusy_Button = true;
            }
        }


        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy_Button
        {
            get { return _isBusy_Button; }
            set
            {
                _isBusy_Button = value;
                OnPropertyChanged();
            }
        }


        public bool IsBusy_Lista
        {
            get { return _IsBusy_Lista; }
            set
            {
                _IsBusy_Lista = value;
                OnPropertyChanged();
            }
        }




        public async Task<int> zmien_temAsync(int flaga)
        {
            flaga_listy = 2;

            var employeesServices = new EmployeesServices();
            
            if (flaga == 1 || (String.Compare(_selectedEmployee.Za, zaddana, true) == 0 && String.Compare(_selectedEmployee.St, St, true) == 0 && String.Compare(_selectedEmployee.Na, Na, true) == 0 ))
            {
                _selectedEmployee.Za = zaddana;
                _selectedEmployee.St = St;
                _selectedEmployee.Na = Na;
            }
            else
            {
                var zapas = await employeesServices.GetEmployeesSelekt(SereweLubSolet__, _selectedEmployee);

                if (zapas == null)
                {
                    _selectedEmployee.Na = Na;
                    _selectedEmployee.St = St;
                    _selectedEmployee.Za = zaddana;
                }
                else
                {
                    _selectedEmployee.Id = zapas.Id;
                    _selectedEmployee.Na = zapas.Na;
                    _selectedEmployee.St = zapas.St;
                    _selectedEmployee.Za = zapas.Za;
                    _selectedEmployee.Te = zapas.Te;
                }
            }

            return 1;
        }


        public async Task<bool> Login()
        {
            if (Settings.Swit_stan != null)
            {
                RestClient<Employee> restClient = new RestClient<Employee>();

                await restClient.CloseWebsocket();  // ← await

                if (Settings.Swit_stan == "True")
                {
                    SereweLubSolet__ = true;
                    restClient.RegisterAdresSerwer(Settings.HostWebsoket, Settings.Serwer, Settings.Password);
                    return true;
                }
                else
                {
                    SereweLubSolet__ = false;
                    restClient.RegisterAdresSoket(Settings.Adres, Settings.Password);
                    return true;
                }
            }
            return false;

        }


        public async Task InitializeDataAsync()
        {
            IsBusy = true;
            IsBusy_Lista = false;

            var employeesServices = new EmployeesServices();
            var newList = await employeesServices.GetEmployeesAsync(SereweLubSolet__);

            if (newList != null && _employeesList != null && _employeesList.Count == newList.Count)
            {
                for (int i = 0; i < newList.Count; i++)
                {
                    _employeesList[i].Id = newList[i].Id;
                    _employeesList[i].Za = newList[i].Za;
                    _employeesList[i].St = newList[i].St;
                    _employeesList[i].Te = newList[i].Te;
                    _employeesList[i].Na = newList[i].Na;
                }
            }
            else
            {
                EmployeesList = newList;
            }

            IsBusy = false;
            IsBusy_Lista = true;
        }





        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

          

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));



        }

        public void ShowOrHidePoducts(Employee product)
        {
            //   _selectedEmployeeUpdate = product;        // przypisanie id listy

            //  _selectedEmployeePrzedZmiana = product;
            //   zaddana = product.Za;

            //  _selectedEmployeePrzedZmiana.Za = _selectedEmployee.Za;

            //  _oldProduct.Na = "mmmmmmm";

            //   UpdateProducts(_oldProduct);
        }



        private void UpdateProducts(Employee product)     // zmaina elementu na liscie
        {
            var index = _employeesList.IndexOf(product);
            if (index < 0)
                return;

            var target = _employeesList[index];
            target.Id = product.Id;
            target.Za = product.Za;
            target.St = product.St;
            target.Te = product.Te;
            target.Na = product.Na;
        }


        // listCopyTo *********************************************************

       



        public ObservableCollection<EmploCopyTo> EmployeesListCopyTo
        {
            get { return _employeesListCopyTo; }
            set
            {

                _employeesListCopyTo = value;
                OnPropertyChanged();

            }
        }

        public EmploCopyTo SelectedEmployeeCopyTo       // zaladuj dane z listy
        {
            get { return _selectedEmployeeCopyTo; }
            set
            {
                _selectedEmployeeCopyTo = value;
                OnPropertyChanged();

                _selectedEmployeeUpdateCopyTo = _selectedEmployeeCopyTo;                                                                                           
                UpdateProductsCopyTo(_selectedEmployeeUpdateCopyTo);


            }
        }


        private void UpdateProductsCopyTo(EmploCopyTo product)     // zmaina elementu na liscie
        {
            var index = _employeesListCopyTo.IndexOf(product);
            _employeesListCopyTo.Remove(product);
            _employeesListCopyTo.Insert(index, product);
        }


    }
}
