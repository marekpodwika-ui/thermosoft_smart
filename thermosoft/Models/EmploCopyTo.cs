using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace thermosoft.Models
{
    public class EmploCopyTo : INotifyPropertyChanged
    {

        public int Id { get; set; }         //id pomieszenia
        public string Na { get; set; }     //nazwa pomieszenia
                                           // public bool CopyTrueFals { get; set; }     //nazwa pomieszenia

        private bool _czakBox;
        public bool CzakBox
        {
            get => _czakBox;
            set
            {
                if (_czakBox != value)
                {
                    _czakBox = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
