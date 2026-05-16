using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace thermosoft.Models
{
    public class TodoItem
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AdresPort { get; set; } = string.Empty;
        public string SerwerIdetyfikator { get; set; } = string.Empty;
        public string HostWebsoket { get; set; } = string.Empty;
        public string Haslo { get; set; } = string.Empty;
        public bool Server_Port { get; set; } = true;
    }
}
