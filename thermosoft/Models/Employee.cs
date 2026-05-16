using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace thermosoft.Models
{
    public class Employee : INotifyPropertyChanged
    {
        private int _id;
        private string _za;
        private string _st;
        private string _te;
        private string _na;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Za        //temperatura zadana
        {
            get => _za;
            set { _za = value; OnPropertyChanged(); }
        }

        public string St        //Stan przekaznika
        {
            get => _st;
            set { _st = value; OnPropertyChanged(); }
        }

        public string Te        // temperatura aktualna
        {
            get => _te;
            set { _te = value; OnPropertyChanged(); }
        }

        public string Na        //nazwa pomieszenia
        {
            get => _na;
            set { _na = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
