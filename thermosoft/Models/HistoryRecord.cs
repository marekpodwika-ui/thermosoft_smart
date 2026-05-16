using System.Runtime.InteropServices;

namespace thermosoft.Models
{
    public class HistoryRecord
    {
        public int d { get; set; }      // dzieñ tygodnia (1=pon, 7=niedz)
        public string h { get; set; }   // czas "HH:mm"
        public string t { get; set; }   // 8-znakowy HEX (Temperatura + Mas_Status)
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct HistoriaUnion
    {
        [FieldOffset(0)]
        public uint H_Tep_MasS_R;
        [FieldOffset(0)]
        public short Temperatura;
        [FieldOffset(2)]
        public byte Mas_Status;
        [FieldOffset(3)]
        public byte Rezerwa;
    }

    public class HistoryPoint
    {
        public string Time { get; set; }        // "HH:mm"
        public int Day { get; set; }            // dzieñ tygodnia
        public double Temperature { get; set; } // temperatura w °C
        public bool IsGrzanie { get; set; }     // bit1
        public bool IsOknoOtwarte { get; set; } // bit2 lub bit6
        public bool IsNieobecny { get; set; }   // bit5

        public static HistoryPoint Parse(HistoryRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.t))
                return null;

            string hex = record.t.Trim();
            if (string.IsNullOrEmpty(hex) || hex == "00000000")
                return null;

            try
            {
                HistoriaUnion u = default;
                u.H_Tep_MasS_R = Convert.ToUInt32(hex, 16);

                double temp = u.Temperatura == -120 ? double.NaN : (double)u.Temperatura / 10.0;

                return new HistoryPoint
                {
                    Time = record.h,
                    Day = record.d,
                    Temperature = temp,
                    IsGrzanie = (u.Mas_Status & (1 << 1)) != 0,
                    IsOknoOtwarte = (u.Mas_Status & (1 << 2)) != 0 || (u.Mas_Status & (1 << 6)) != 0,
                    IsNieobecny = (u.Mas_Status & (1 << 5)) != 0
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
