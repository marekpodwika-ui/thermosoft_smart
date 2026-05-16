using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using thermosoft.Models;
using thermosoft.ViewModels;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System.Threading;

namespace thermosoft.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewEmployeePage : ContentPage
    {


        public static MainViewModel SharedViewModel { get; set; }

        public const byte BoolStatus = 0;        // 0 ok             1 error
        public const byte BoolStan = 1;        // 0 nie grzeje     1 grzeje
        public const byte BoolOkno = 2;        // 0 zamkniete     1 otwarte
        public const byte BoolDzienNoc = 3;        // 0 Noc            1 Dzien
        public const byte BoolCzas = 4;          // 0 nie aktywny    1 aktywny     termostat

        Button[] tickMarks = new Button[24];
        Label[] tickMarksLabel = new Label[24];

      //  Button[] CopytickMarks = new Button[200];
        // Label[] CopytickMarksLabel = new Label[200]
         uint[] CopyDo_Wielu = new uint[6];

        private byte FlagaOdsieszWszystko;

        private bool isZadDzienActive = false;
        private bool isZadNocActive = false;

        public struct TEMP
        {
            public byte TempNoc;
            public byte TempDzien;
            public byte TempReczna;
            public byte TimerReczna;
        }



        [StructLayout(LayoutKind.Explicit)]
        public struct Union
        {
            [FieldOffset(0)]
            public uint ZadTepCzas;
            [FieldOffset(0)]
            public TEMP Za;
        }

        public Union Zad;
      

        private uint St;
        private uint temp_dzien;
        private uint temp_noc;
        private uint temp_reczna;
        private uint rurznica;
        // private object TapList;
        private uint temp_dzien_porownanie;
        private uint temp_noc_porownanie;

        private int RechnaCzas;
       // private uint Rechnz_dzien;

        private MainViewModel mainViewModel;

        // Zmienne do obsługi długiego naciśnięcia
        private bool isButtonDodajPressed = false;
        private bool isButtonOdejmiPressed = false;
        private bool isButtonDodajRecznaPressed = false;
        private bool isButtonOdejmiRecznaPressed = false;
        private bool isButtonDodajCzasPressed = false;
        private bool isButtonOdejmiCzasPressed = false;

        private CancellationTokenSource? dodajCts;
        private CancellationTokenSource? odejmiCts;
        private CancellationTokenSource? dodajRechnaCts;
        private CancellationTokenSource? odejmiRechnaCts;
        private CancellationTokenSource? dodajCzasCts;
        private CancellationTokenSource? odejmiCzasCts;
        private bool isMaxTimeReached = false;
        private DateTime maxTimeReachedTime = DateTime.MinValue;

        private const int LONG_PRESS_DELAY_MS = 300;
        private const int REPEAT_INTERVAL_MS = 100;
        private const int TEMP_STEP = 5;
        private const int MIN_TEMP = 70;
        private const int MAX_TEMP = 300;
        
        // Cache dla obliczeń sinus/cosinus
        private double[]? _sinCache;
        private double[]? _cosCache;
        private const int CACHE_SIZE = 24;

        public NewEmployeePage() : this(SharedViewModel)
        {
        }

        public NewEmployeePage(MainViewModel mainViewModel)
        {

            InitializeComponent();
            //  BindingContext = mainViewModel;
            this.mainViewModel = mainViewModel;

          


            St = (uint)Convert.ToInt32(mainViewModel.SelectedEmployee.St, 16);


            for (int i = 0; i < tickMarks.Length; i++)
            {
                if ((St & 1 << i + 8) != 0)
                {
                    tickMarks[i] = new Button { BackgroundColor = Colors.Red };
                    tickMarks[i].Pressed += MainPage_Clicked;
                }
                else
                {
                    tickMarks[i] = new Button { BackgroundColor = Colors.RoyalBlue };
                    tickMarks[i].Pressed += MainPage_Clicked;
                }
                tickMarksLabel[i] = new Label { Text = "" + i, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };


                Zegar.Children.Add(tickMarksLabel[i]);
                Zegar.Children.Add(tickMarks[i]);

            }




        

            short Te = (short)Convert.ToInt32(mainViewModel.SelectedEmployee.Te, 16);

            //  Etykieta.Title = mainViewModel.SelectedEmployee.Na;
           

            if (Te == -120)
            {
              //  temPomieszczenia.Text = "----";
              //  temPomieszczeniaReczna.Text = "----";
                Etykieta.Title = mainViewModel.SelectedEmployee.Na + "  ----";

              
            }
            else
            {
                double zamianaa = (double)Te / 10;
              //  temPomieszczenia.Text = zamianaa.ToString("0.0°");
              //  temPomieszczeniaReczna.Text = zamianaa.ToString("0.0°");
                Etykieta.Title = mainViewModel.SelectedEmployee.Na + "  " + zamianaa.ToString("0.0°");


            }




            Zad.ZadTepCzas = (uint)Convert.ToInt32(mainViewModel.SelectedEmployee.Za, 16);

               temp_dzien  = (uint)(Zad.Za.TempDzien + 50);
               temp_noc    = (uint)(Zad.Za.TempNoc + 50);
               temp_reczna = (uint)(Zad.Za.TempReczna + 50);
               RechnaCzas  = (int)(Zad.Za.TimerReczna * 2);

            if (RechnaCzas==0)
            {
                PanelReczny.IsVisible = false;
                PanelHarmonogram.IsVisible = true;
            }
            else
            {
                PanelHarmonogram.IsVisible = false;
                PanelReczny.IsVisible = true;
            }


            Wyswiet_Czas(RechnaCzas);


               temp_dzien_porownanie  =  temp_dzien;
               temp_noc_porownanie    =  temp_noc;
               rurznica = temp_dzien  -  temp_noc;


               AktualizujWyswietlanie();

               var test = (double)temp_noc / 10;
               ZadNoc.Text = test.ToString("0.0°");

               // Wyświetl temperaturę ręczną
               var testReczna = (double)temp_reczna / 10;
               RecznaTemper.Text = testReczna.ToString("0.0°");
        }



        protected override void OnAppearing()
        {
            if (CopyDo_Wielu[0] != 0 || CopyDo_Wielu[1] != 0 || CopyDo_Wielu[2] != 0 || CopyDo_Wielu[3] != 0 || CopyDo_Wielu[4] != 0 || CopyDo_Wielu[5] != 0)
            {
                if (PanelHarmonogram.IsVisible == true)
                {
                    MeniHanmorogramZapisz();
                }
                else
                {
                    MeniReczneZapisz();
                }
            }
        }




        private void MainPage_Clicked(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            int index = Array.IndexOf(tickMarks, clickedButton);

          //  tickMarks[7].BackgroundColor = Color.Red;

            if (clickedButton.BackgroundColor == Colors.Red)
            {
                clickedButton.BackgroundColor = Colors.RoyalBlue;
                St &= ~(uint)(1 << index + 8);//zerowanie
            }
            else
            {
                clickedButton.BackgroundColor = Colors.Red;
                St |= (uint)(1 << index + 8);//jeden
                // (sender as Button).BackgroundColor = Color.Red;
            }
            // tem_zad_eko.Text = St.ToString("0"); // do testow

        }


        private void Button_zmienTemper_Clicked(object sender, EventArgs e)
        {

            MeniHanmorogramZapisz();
        }


        async void MeniHanmorogramZapisz()
        {
            PanelHarmonogram.IsEnabled = false; // zabokoj przyciski
            if (temp_dzien_porownanie != temp_dzien || temp_noc_porownanie != temp_noc)
            {
                St &= ~(uint)(1 << BoolCzas);//zerowanie
            }

            temp_dzien -= 50;
            temp_noc -= 50;


            Zad.Za.TempDzien = (byte)temp_dzien;
            Zad.Za.TempNoc = (byte)temp_noc;
            Zad.Za.TempReczna = 0;
            Zad.Za.TimerReczna = 0;


            // var emplo = new Employee();

            mainViewModel.SelectedEmployee.Za = Zad.ZadTepCzas.ToString("X");  // zamiana na hex
            mainViewModel.SelectedEmployee.St = St.ToString("X");
            FlagaOdsieszWszystko = 0;
            //  mainViewModel.SelectedEmployee.Na = "&‭7FFFFFF‬,ff,ff,ff,ff,FFFF,";
            PomieszDoKopiowania();

            fresz.IsVisible = true;
            fresz.IsRunning = true;
            await zmien_temperatureAsync();

        }



        void PomieszDoKopiowania()
        {
            if (CopyDo_Wielu[0] != 0 || CopyDo_Wielu[1] != 0 || CopyDo_Wielu[2] != 0 || CopyDo_Wielu[3] != 0 || CopyDo_Wielu[4] != 0 || CopyDo_Wielu[5] != 0)
            {
                StringBuilder sb = new StringBuilder("&");
                for (int i = 0; i < 6; i++)
                {
                    sb.Append(CopyDo_Wielu[i].ToString("X"));
                    if (i < 5) sb.Append(",");
                }
                sb.Append(",");
                mainViewModel.SelectedEmployee.Na = sb.ToString();
                FlagaOdsieszWszystko = 1;
            }
        }


        async Task zmien_temperatureAsync()
        {
            await mainViewModel?.zmien_temAsync(0);
            if (FlagaOdsieszWszystko == 1)
            {
                await mainViewModel?.InitializeDataAsync();
            }
            await Shell.Current.GoToAsync("..");
        }



        // ============ TEMPERATURA DNIA/NOCY ============

        private async void Button_Dodaj_Pressed(object sender, EventArgs e)
        {
            isButtonDodajPressed = true;
            DisposeCancellationToken(ref dodajCts);
            dodajCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, dodajCts.Token);

                while (isButtonDodajPressed && !dodajCts.Token.IsCancellationRequested)
                {
                    InkrementujTemperature();
                    await Task.Delay(REPEAT_INTERVAL_MS, dodajCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref dodajCts);
            }
        }

        private void Button_Dodaj_Released(object sender, EventArgs e)
        {
            if (isButtonDodajPressed)
            {
                isButtonDodajPressed = false;

                if (dodajCts != null && !dodajCts.Token.IsCancellationRequested)
                {
                    InkrementujTemperature();
                }

                DisposeCancellationToken(ref dodajCts);
            }
        }

        private void InkrementujTemperature()
        {
            bool isDzienSelected = isZadDzienActive;
            bool isNocSelected = isZadNocActive;

            if (!isDzienSelected && !isNocSelected)
            {
                temp_dzien = ClampTemperature(temp_dzien + TEMP_STEP);
                temp_noc = ClampTemperature(temp_noc + TEMP_STEP);
            }
            else if (isDzienSelected)
            {
                temp_dzien = ClampTemperature(temp_dzien + TEMP_STEP);
            }
            else if (isNocSelected)
            {
                temp_noc = ClampTemperature(temp_noc + TEMP_STEP);
            }

            AktualizujWyswietlanie();
        }

        private void DekrementujTemperature()
        {
            bool isDzienSelected = isZadDzienActive;
            bool isNocSelected = isZadNocActive;

            if (!isDzienSelected && !isNocSelected)
            {
                temp_dzien = ClampTemperature(temp_dzien - TEMP_STEP);
                temp_noc = ClampTemperature(temp_noc - TEMP_STEP);
            }
            else if (isDzienSelected)
            {
                temp_dzien = ClampTemperature(temp_dzien - TEMP_STEP);
            }
            else if (isNocSelected)
            {
                temp_noc = ClampTemperature(temp_noc - TEMP_STEP);
            }

            AktualizujWyswietlanie();
        }

        private uint ClampTemperature(uint value)
        {
            if (value < MIN_TEMP) return MIN_TEMP;
            if (value > MAX_TEMP) return MAX_TEMP;
            return value;
        }

        private async void Button_Odejmi_Pressed(object sender, EventArgs e)
        {
            isButtonOdejmiPressed = true;
            DisposeCancellationToken(ref odejmiCts);
            odejmiCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, odejmiCts.Token);

                while (isButtonOdejmiPressed && !odejmiCts.Token.IsCancellationRequested)
                {
                    DekrementujTemperature();
                    await Task.Delay(REPEAT_INTERVAL_MS, odejmiCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref odejmiCts);
            }
        }

        private void Button_Odejmi_Released(object sender, EventArgs e)
        {
            if (isButtonOdejmiPressed)
            {
                isButtonOdejmiPressed = false;

                if (odejmiCts != null && !odejmiCts.Token.IsCancellationRequested)
                {
                    DekrementujTemperature();
                }

                DisposeCancellationToken(ref odejmiCts);
            }
        }

        private async void Button_DodajCzas_Pressed(object sender, EventArgs e)
        {
            isButtonDodajCzasPressed = true;
            DisposeCancellationToken(ref dodajCzasCts);
            dodajCzasCts = new CancellationTokenSource();
            isMaxTimeReached = false;

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, dodajCzasCts.Token);

                while (isButtonDodajCzasPressed && !dodajCzasCts.Token.IsCancellationRequested)
                {
                    // Jeśli osiągnęliśmy 480, zanotuj czas
                    if (RechnaCzas >= 480 && !isMaxTimeReached)
                    {
                        isMaxTimeReached = true;
                        maxTimeReachedTime = DateTime.Now;
                    }

                    // Jeśli jesteśmy na 480 i trzymamy przez 3 sekundy, odblokuj limit
                    if (isMaxTimeReached && (DateTime.Now - maxTimeReachedTime).TotalSeconds >= 3)
                    {
                        RechnaCzas = 490; // Przeskok na 490
                        isMaxTimeReached = false;
                    }

                    InkrementujCzas();
                    await Task.Delay(REPEAT_INTERVAL_MS, dodajCzasCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref dodajCzasCts);
            }
        }

        private void Button_DodajCzas_Released(object sender, EventArgs e)
        {
            if (isButtonDodajCzasPressed)
            {
                isButtonDodajCzasPressed = false;
                isMaxTimeReached = false;

                if (dodajCzasCts != null && !dodajCzasCts.Token.IsCancellationRequested)
                {
                    InkrementujCzas();
                }

                DisposeCancellationToken(ref dodajCzasCts);
            }
        }

        private async void Button_OdejmiCzas_Pressed(object sender, EventArgs e)
        {
            isButtonOdejmiCzasPressed = true;
            DisposeCancellationToken(ref odejmiCzasCts);
            odejmiCzasCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, odejmiCzasCts.Token);

                while (isButtonOdejmiCzasPressed && !odejmiCzasCts.Token.IsCancellationRequested)
                {
                    DekrementujCzas();
                    await Task.Delay(REPEAT_INTERVAL_MS, odejmiCzasCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref odejmiCzasCts);
            }
        }

        private void Button_OdejmiCzas_Released(object sender, EventArgs e)
        {
            if (isButtonOdejmiCzasPressed)
            {
                isButtonOdejmiCzasPressed = false;

                if (odejmiCzasCts != null && !odejmiCzasCts.Token.IsCancellationRequested)
                {
                    DekrementujCzas();
                }

                DisposeCancellationToken(ref odejmiCzasCts);
            }
        }

        private void InkrementujCzas()
        {
            // Jeśli przeskokaliśmy na 490, zatrzymaj na tym limicie
            if (RechnaCzas >= 490)
            {
                // Nie zwiększaj dalej, zostań na 490
                if (RechnaCzas > 490) RechnaCzas = 490;
            }
            else if (RechnaCzas < 480)
            {
                RechnaCzas += 30;
                if (RechnaCzas > 480) RechnaCzas = 480;
            }
            Wyswiet_Czas(RechnaCzas);
        }

        private void DekrementujCzas()
        {
            // Jeśli jesteśmy na 490, przeskocz na 480 w pierwszym kroku
            if (RechnaCzas >= 490)
            {
                RechnaCzas = 480;
            }
            else if (RechnaCzas > 0)
            {
                RechnaCzas -= 30;
                if (RechnaCzas < 0) RechnaCzas = 0;
            }
            Wyswiet_Czas(RechnaCzas);
        }

        void Wyswiet_Czas(int Czas)
        {
            if (Czas >= 490)
            {
                CzasWswie.Text = "--:--";
            }
            else
            {
                TimeSpan time = TimeSpan.FromMinutes(Czas);
                CzasWswie.Text = time.ToString(@"hh\:mm");
            }
        }

        private void InkrementujTemperatureReczna()
        {
            temp_reczna = ClampTemperature(temp_reczna + TEMP_STEP);
            AktualizujWyswietlanyTemperatureReczna();
        }

        private void DekrementujTemperatureReczna()
        {
            temp_reczna = ClampTemperature(temp_reczna - TEMP_STEP);
            AktualizujWyswietlanyTemperatureReczna();
        }

        private void AktualizujWyswietlanyTemperatureReczna()
        {
            var test = (double)temp_reczna / 10;
            RecznaTemper.Text = test.ToString("0.0°");
        }

        // ============ TEMPERATURA RĘCZNA ============

        private async void Button_DodajReczna_Pressed(object sender, EventArgs e)
        {
            isButtonDodajRecznaPressed = true;
            DisposeCancellationToken(ref dodajRechnaCts);
            dodajRechnaCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, dodajRechnaCts.Token);

                while (isButtonDodajRecznaPressed && !dodajRechnaCts.Token.IsCancellationRequested)
                {
                    InkrementujTemperatureReczna();
                    await Task.Delay(REPEAT_INTERVAL_MS, dodajRechnaCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref dodajRechnaCts);
            }
        }

        private void Button_DodajReczna_Released(object sender, EventArgs e)
        {
            if (isButtonDodajRecznaPressed)
            {
                isButtonDodajRecznaPressed = false;

                if (dodajRechnaCts != null && !dodajRechnaCts.Token.IsCancellationRequested)
                {
                    InkrementujTemperatureReczna();
                }

                DisposeCancellationToken(ref dodajRechnaCts);
            }
        }

        private async void Button_OdejmiReczna_Pressed(object sender, EventArgs e)
        {
            isButtonOdejmiRecznaPressed = true;
            DisposeCancellationToken(ref odejmiRechnaCts);
            odejmiRechnaCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(LONG_PRESS_DELAY_MS, odejmiRechnaCts.Token);

                while (isButtonOdejmiRecznaPressed && !odejmiRechnaCts.Token.IsCancellationRequested)
                {
                    DekrementujTemperatureReczna();
                    await Task.Delay(REPEAT_INTERVAL_MS, odejmiRechnaCts.Token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                DisposeCancellationToken(ref odejmiRechnaCts);
            }
        }

        private void Button_OdejmiReczna_Released(object sender, EventArgs e)
        {
            if (isButtonOdejmiRecznaPressed)
            {
                isButtonOdejmiRecznaPressed = false;

                if (odejmiRechnaCts != null && !odejmiRechnaCts.Token.IsCancellationRequested)
                {
                    DekrementujTemperatureReczna();
                }

                DisposeCancellationToken(ref odejmiRechnaCts);
            }
        }

        private void Button_zmienTemperRecza_Clicked(object sender, EventArgs e)
        {
            MeniReczneZapisz();
        }

        async void MeniReczneZapisz()
        {
            // Sprawdzenie timera PRZED modyfikacją
            if (RechnaCzas == 0)
            {
                await DisplayAlert("błąd", "Ustaw timer", "Ok");
                return;
            }

            // Sprawdzenie czy temperatury się zmieniły
            if (temp_dzien_porownanie != temp_dzien || temp_noc_porownanie != temp_noc)
            {
                St &= ~(uint)(1 << BoolCzas); // zerowanie
            }

            // Obliczanie wartości PRZED przypisaniem (bez odejmowania 50)
            byte tempDzienByte = (byte)(temp_dzien > 50 ? temp_dzien - 50 : 0);
            byte tempNocByte = (byte)(temp_noc > 50 ? temp_noc - 50 : 0);
            byte tempRechnaByte = (byte)(temp_reczna > 50 ? temp_reczna - 50 : 0);
            byte timerByte = (byte)(RechnaCzas > 0 ? RechnaCzas / 2 : 0);

            Zad.Za.TempDzien = tempDzienByte;
            Zad.Za.TempNoc = tempNocByte;
            Zad.Za.TempReczna = tempRechnaByte;
            Zad.Za.TimerReczna = timerByte;

            PanelHarmonogram.IsEnabled = false; // blokuj przyciski

            mainViewModel.SelectedEmployee.Za = Zad.ZadTepCzas.ToString("X");  // zamiana na hex
            mainViewModel.SelectedEmployee.St = St.ToString("X");

            PomieszDoKopiowania();

            freszReczny.IsVisible = true;
            freszReczny.IsRunning = true;
            await zmien_temperatureAsync();
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            ListViewPage1.SharedViewModel = mainViewModel;
            ListViewPage1.SharedCopyDoWielu = CopyDo_Wielu;
            await Shell.Current.GoToAsync(nameof(ListViewPage1));
        }

        private async void Historia_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistoryPage(
                MainViewModel.SereweLubSolet__,
                mainViewModel.SelectedEmployee));
        }

        private void InitializeTrigCache()
        {
            if (_sinCache == null)
            {
                _sinCache = new double[CACHE_SIZE];
                _cosCache = new double[CACHE_SIZE];
                for (int i = 0; i < CACHE_SIZE; i++)
                {
                    double radians = i * 2 * Math.PI / CACHE_SIZE;
                    _sinCache[i] = Math.Sin(radians);
                    _cosCache[i] = Math.Cos(radians);
                }
            }
        }

        private void AktualizujWyswietlanie()
        {
            var testDzien = (double)temp_dzien / 10;
            ZadDzien.Text = testDzien.ToString("0.0°");

            var testNoc = (double)temp_noc / 10;
            ZadNoc.Text = testNoc.ToString("0.0°");
        }

        private void ZadDzien_Tapped(object sender, TappedEventArgs e)
        {
            if (!isZadDzienActive)
            {
                isZadDzienActive = true;
                isZadNocActive = false;
                ZadNoc.TextColor = Colors.Gray;
                ZadDzien.TextColor = Colors.White;
            }
            else
            {
                isZadDzienActive = false;
                ZadDzien.TextColor = Colors.Red;
                ZadNoc.TextColor = Colors.RoyalBlue;
            }

            if (!isZadDzienActive && !isZadNocActive)
            {
                rurznica = temp_dzien - temp_noc;
            }

            rurznica = temp_dzien - temp_noc;
        }

        private void ZadNoc_Tapped(object sender, TappedEventArgs e)
        {
            if (!isZadNocActive)
            {
                isZadNocActive = true;
                isZadDzienActive = false;
                ZadDzien.TextColor = Colors.Gray;
                ZadNoc.TextColor = Colors.White;
            }
            else
            {
                isZadNocActive = false;
                ZadDzien.TextColor = Colors.Red;
                ZadNoc.TextColor = Colors.RoyalBlue;
            }

            rurznica = temp_dzien - temp_noc;
        }

        void OnAbsoluteLayoutSizeChanged(object sender, EventArgs args)
        {
            InitializeTrigCache();

            Point center = new Point(Zegar.Width / 2, Zegar.Height / 2);
            double radius = 0.45 * Math.Min(Zegar.Width, Zegar.Height);
            double radiusL = 0.37 * Math.Min(Zegar.Width, Zegar.Height);

            for (int index = 0; index < tickMarks.Length; index++)
            {
                double size = radius / (index % 6 == 0 ? 6 : 8);
                double radians = index * 2 * Math.PI / tickMarks.Length;
                
                // Użyj cache'owanych wartości
                double sinVal = _sinCache![index];
                double cosVal = _cosCache![index];
                
                double x = center.X + radius * sinVal - size / 2;
                double y = center.Y - radius * cosVal - size / 2;
                
                Microsoft.Maui.Controls.AbsoluteLayout.SetLayoutBounds(tickMarks[index], new Rect(x, y, size, size));
                tickMarks[index].Rotation = 180 * radians / Math.PI;

                double sizeL = radiusL / 4;
                double xL = center.X + radiusL * sinVal - sizeL / 2;
                double yL = center.Y - radiusL * cosVal - sizeL / 2;

                Microsoft.Maui.Controls.AbsoluteLayout.SetLayoutBounds(tickMarksLabel[index], new Rect(xL, yL, sizeL, sizeL));
            }
        }

        private void Button_Reczny_Clicked(object sender, EventArgs e)
        {
            PanelHarmonogram.IsVisible = false;
            PanelReczny.IsVisible = true;
        }

        private void Button_Harmonogram_Clicked(object sender, EventArgs e)
        {
            PanelReczny.IsVisible = false;
            PanelHarmonogram.IsVisible = true;
        }

        // ============ CZAS ============

        private void DisposeCancellationToken(ref CancellationTokenSource? cts)
        {
            if (cts != null)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch { }
                cts = null;
            }
        }
    }
}