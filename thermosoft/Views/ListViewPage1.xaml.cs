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
using thermosoft.ViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace thermosoft.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ListViewPage1 : ContentPage
    {
        //  uint[] CopyDo_Wielu = new uint[6];

        private MainViewModel mainViewModel;
        private uint[] arr;
        uint[] CopyDo_Wielu = new uint[6];
        private int sizeOfList;
        private int seletetId;
        private ObservableCollection<Employee> _cachedEmployeesList;

        public static MainViewModel SharedViewModel { get; set; }
        public static uint[] SharedCopyDoWielu { get; set; }

        public ListViewPage1() : this(SharedViewModel, SharedCopyDoWielu)
        {
        }

        public ListViewPage1(MainViewModel mainViewModel, uint[] arr)
        {
            InitializeComponent();

            this.mainViewModel = mainViewModel;
            this.arr = arr;

            CopyDo_Wielu[0] = arr[0];
            CopyDo_Wielu[1] = arr[1];
            CopyDo_Wielu[2] = arr[2];
            CopyDo_Wielu[3] = arr[3];
            CopyDo_Wielu[4] = arr[4];
            CopyDo_Wielu[5] = arr[5];

            NapelniListe();
        }

        // Helper method - obliczenia bitowe
        private void GetBitTabBit(int index, out byte tab, out byte bit)
        {
            byte idx = (byte)index;
            tab = (byte)(idx / 32);
            bit = (byte)(idx - (tab * 32));
        }

        // Helper method - sprawdzenie bitu
        private bool IsBitSet(int index)
        {
            GetBitTabBit(index, out byte tab, out byte bit);
            return (CopyDo_Wielu[tab] & (1 << bit)) != 0;
        }

        // Helper method - ustawienie bitu
        private void SetBit(int index)
        {
            GetBitTabBit(index, out byte tab, out byte bit);
            CopyDo_Wielu[tab] |= (uint)(1 << bit);
        }

        // Helper method - zerowanie bitu
        private void ClearBit(int index)
        {
            GetBitTabBit(index, out byte tab, out byte bit);
            CopyDo_Wielu[tab] &= ~(uint)(1 << bit);
        }

        void NapelniListe()
        {
            var vm = BindingContext as MainViewModel;
            if (vm == null) return;

            vm.EmployeesListCopyTo = new ObservableCollection<EmploCopyTo>();
            _cachedEmployeesList = mainViewModel._employeesList;
            sizeOfList = _cachedEmployeesList.Count;
            seletetId = mainViewModel.SelectedEmployee.Id;

            for (int i = 0; i < sizeOfList; i++)
            {
                bool isBitSet = IsBitSet(i);
                bool isSelected = i == seletetId;

                vm.EmployeesListCopyTo.Add(new EmploCopyTo
                {
                    Na = _cachedEmployeesList[i].Na,
                    Id = _cachedEmployeesList[i].Id,
                    CzakBox = isBitSet || isSelected
                });
            }
        }

        protected override void OnAppearing()
        {
        }

        private void EmployeesListViewCopyTo_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            var employee_selectedCopyTo = EmployeesListViewCopyTo.SelectedItem as EmploCopyTo;

            if (employee_selectedCopyTo == null)
                return;

            var vm = BindingContext as MainViewModel;
            if (vm == null)
                return;

            int index = employee_selectedCopyTo.Id;

            if (seletetId != index)
            {
                if (employee_selectedCopyTo.CzakBox)
                {
                    employee_selectedCopyTo.CzakBox = false;
                    ClearBit(index);
                }
                else
                {
                    employee_selectedCopyTo.CzakBox = true;
                    SetBit(index);
                }
            }

            vm.SelectedEmployeeCopyTo = employee_selectedCopyTo;
        }

        private async void Zatwierdz_Clicked(object sender, EventArgs e)
        {
            arr[0] = CopyDo_Wielu[0];
            arr[1] = CopyDo_Wielu[1];
            arr[2] = CopyDo_Wielu[2];
            arr[3] = CopyDo_Wielu[3];
            arr[4] = CopyDo_Wielu[4];
            arr[5] = CopyDo_Wielu[5];

            await Shell.Current.GoToAsync("..");
        }

        private void Wszyskie_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (Wszyskie.IsChecked == true)
            {
                for (int i = 0; i < sizeOfList; i++)
                {
                    if (seletetId != i)
                    {
                        SetBit(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < sizeOfList; i++)
                {
                    ClearBit(i);
                }
            }

            NapelniListe();
        }
    }
}
