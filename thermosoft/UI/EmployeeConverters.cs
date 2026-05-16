using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace thermosoft.UI
{
    public static class BitFlags
    {
        public const byte BoolStatus = 0;
        public const byte BoolStan = 1;
        public const byte BoolOkno = 2;
        public const byte BoolDzienNoc = 3;
        public const byte BoolCzas = 4;
        public const byte BoolZamek = 5;
        public const byte BoolOknoTermostat = 6;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ZadUnion
    {
        [FieldOffset(0)]
        public uint ZadTepCzas;
        [FieldOffset(0)]
        public byte TempNoc;
        [FieldOffset(1)]
        public byte TempDzien;
        [FieldOffset(2)]
        public byte TempReczna;
        [FieldOffset(3)]
        public byte TimerReczna;
    }

    internal static class HexHelper
    {
        public static uint ToUInt(string hex) => (uint)Convert.ToInt32(hex, 16);
        public static short ToShort(string hex) => (short)Convert.ToInt32(hex, 16);

        public static bool IsError(uint st) => (st & 1 << BitFlags.BoolStatus) != 0;
        public static bool IsGrzanie(uint st) => (st & 1 << BitFlags.BoolStan) != 0 && !IsError(st);
        public static bool IsStop(uint st) => (st & 1 << BitFlags.BoolStan) == 0 && !IsError(st);
        public static bool IsOknoOtwarte(uint st) => (st & 1 << BitFlags.BoolOkno) != 0 || (st & 1 << BitFlags.BoolOknoTermostat) != 0;
    }

    // St hex -> kolor obramowania (White=stop/praca, Black=error)
    public class BorderStrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                uint st = HexHelper.ToUInt(hex);
                if (HexHelper.IsError(st))
                    return Colors.Black;
            }
            return Colors.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // St hex -> ikona okna (zale¿y od stanu: grzanie/stop/error + okno otwarte)
    public class OknoIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                uint st = HexHelper.ToUInt(hex);
                if (HexHelper.IsOknoOtwarte(st))
                    return Icons.OknoOtwarte;
                if (HexHelper.IsError(st))
                    return Icons.Blad;
                if (HexHelper.IsGrzanie(st))
                    return Icons.Grzanie;
            }
            return Icons.Nagrzane;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // St hex -> kolor ikony okna
    public class OknoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                uint st = HexHelper.ToUInt(hex);
                if (HexHelper.IsOknoOtwarte(st))
                    return Colors.DodgerBlue;
                if (HexHelper.IsGrzanie(st))
                    return Colors.Red;
            }
            return Colors.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IdConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id)
                return (id + 1).ToString("0");
            return "";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class TemperaturaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                short te = HexHelper.ToShort(hex);
                if (te == -120)
                    return "----";
                return ((double)te / 10).ToString("0.0°", CultureInfo.InvariantCulture);
            }
            return "----";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZadIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "";
            if (values[0] is not string stHex || values[1] is not string zaHex)
                return "";
            if (string.IsNullOrEmpty(stHex) || string.IsNullOrEmpty(zaHex))
                return "";

            uint st = HexHelper.ToUInt(stHex);
            ZadUnion zad = default;
            zad.ZadTepCzas = HexHelper.ToUInt(zaHex);

            bool isNieobecny = (st & 1 << BitFlags.BoolZamek) != 0;

            if ((st & 1 << BitFlags.BoolCzas) != 0)
            {
                string ikona = zad.TimerReczna == 245 ? Icons.Reczne : Icons.Timer;
                return isNieobecny ? ikona + " " + Icons.PozaDomem : ikona;
            }
            else if ((st & 1 << BitFlags.BoolDzienNoc) != 0)
            {
                return Icons.Dzien;
            }
            else
            {
                return isNieobecny ? Icons.PozaDomem : Icons.Noc;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZadIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                uint st = HexHelper.ToUInt(hex);
                bool isNieobecny = (st & 1 << BitFlags.BoolZamek) != 0;
                bool isCzas = (st & 1 << BitFlags.BoolCzas) != 0;
                bool isDzien = (st & 1 << BitFlags.BoolDzienNoc) != 0;

                if (!isCzas && !isDzien && isNieobecny)
                    return Colors.DimGray;
            }
            return Colors.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ZadTempConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return "";
            if (values[0] is not string stHex || values[1] is not string zaHex)
                return "";
            if (string.IsNullOrEmpty(stHex) || string.IsNullOrEmpty(zaHex))
                return "";

            string format = parameter as string ?? "0.0°";

            uint st = HexHelper.ToUInt(stHex);
            ZadUnion zad = default;
            zad.ZadTepCzas = HexHelper.ToUInt(zaHex);

            byte raw;
            if ((st & 1 << BitFlags.BoolCzas) != 0)
                raw = zad.TempReczna;
            else if ((st & 1 << BitFlags.BoolDzienNoc) != 0)
                raw = zad.TempDzien;
            else
                raw = zad.TempNoc;

            double value2 = ((double)raw + 50) / 10;
            return value2.ToString(format, CultureInfo.InvariantCulture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
