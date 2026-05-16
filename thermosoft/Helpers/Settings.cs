using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace thermosoft.Helpers
{
    public static class Settings
    {


        public static string Swit_stan
        {
            get => Preferences.Get("Swit_stan", "");
            set => Preferences.Set("Swit_stan", value);
        }


        public static string Username
        {
            get => Preferences.Get("Username", "");
            set => Preferences.Set("Username", value);
        }

        public static string HostWebsoket
        {
            get => Preferences.Get("HostWebsoket", "");
            set => Preferences.Set("HostWebsoket", value);
        }

        public static string Serwer
        {
            get => Preferences.Get("Serwer", "");
            set => Preferences.Set("Serwer", value);
        }

        public static string Adres
        {
           get => Preferences.Get("Adres", "");
           set => Preferences.Set("Adres", value);
        }

        public static string Password
        {
            get => Preferences.Get("Password", "");
            set => Preferences.Set("Password", value);
        }


    }
}
