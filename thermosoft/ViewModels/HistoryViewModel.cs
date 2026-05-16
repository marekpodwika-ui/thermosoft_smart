using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using thermosoft.Models;
using thermosoft.Services;

namespace thermosoft.ViewModels
{
    public class HistoryViewModel : INotifyPropertyChanged
    {
        private readonly HistoryService _service;
        private readonly bool _useWebSocket;
        private readonly Employee _employee;

        private bool _isBusy;
        private string _errorMessage;
        private int _selectedDayIndex = 0;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private int _detectedDayOfWeek = 0; // 0=nieznany, 1-7=pon-niedz (wykryty z "Ostatnie 24h")
        private int _daysBack = 0; // Ile dni wstecz od "Ostatnie 24h"
        private const int MAX_DAYS_BACK = 6; // Maksymalnie 6 dni wstecz (7 dni danych w³¹cznie z dzisiejszym)
        private bool _isChangingFromButton = false; // Flaga: zmiana pochodzi z przycisku, nie z Pickera

        // Akumulacja klikniêæ - zabezpieczenie przed wielokrotnym szybkim naciskaniem
        private int _pendingPreviousClicks = 0;
        private int _pendingNextClicks = 0;
        private CancellationTokenSource _clickDebounceToken = null;
        private const int DEBOUNCE_MS = 1000; // 400ms animacja
        private bool _isAnimating = false;

        public bool IsAnimating
        {
            get => _isAnimating;
            set
            {
                _isAnimating = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set 
            { 
                _isBusy = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasNoData));
                OnPropertyChanged(nameof(CanLoadPreviousDay));
                OnPropertyChanged(nameof(CanLoadNextDay));
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasNoData)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_errorMessage);
        public bool HasData => Series != null && Series.Length > 0;
        public bool HasNoData => !IsBusy && !HasData && !HasError;

        // Przycisk "Starsze" widoczny gdy: nie busy i nie osi¹gniêto limitu (niezale¿nie od HasData)
        public bool CanLoadPreviousDay
        {
            get
            {
                var result = !IsBusy && _daysBack < MAX_DAYS_BACK;
                System.Diagnostics.Debug.WriteLine($"CanLoadPreviousDay: IsBusy={IsBusy}, HasData={HasData}, DaysBack={_daysBack}/{MAX_DAYS_BACK}, Result={result}");
                return result;
            }
        }

        // Przycisk "Nowsze" widoczny gdy: nie busy i cofniêto siê wstecz (niezale¿nie od HasData)
        public bool CanLoadNextDay
        {
            get
            {
                var result = !IsBusy && _daysBack > 0;
                return result;
            }
        }

        public int SelectedDayIndex
        {
            get => _selectedDayIndex;
            set
            {
                if (_selectedDayIndex == value) return;
                _selectedDayIndex = value;

                // Reset tylko gdy zmiana pochodzi z Pickera (nie z przycisku)
                if (!_isChangingFromButton)
                {
                    // Reset wykrytego dnia i licznika cofniêæ gdy zmienia siê wybór w Pickerze
                    if (value != 0) _detectedDayOfWeek = 0;
                    _daysBack = 0; // Reset licznika
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentDayDisplay));
                _ = LoadDataAsync();
            }
        }

        public List<string> Days { get; } = new List<string>
        {
            "Ostatnie 24h", "Poniedzia³ek", "Wtorek", "Œroda",
            "Czwartek", "Pi¹tek", "Sobota", "Niedziela"
        };

        public string CurrentDayDisplay
        {
            get
            {
                string dayName = _selectedDayIndex >= 0 && _selectedDayIndex < Days.Count 
                    ? Days[_selectedDayIndex] 
                    : "?";

                return dayName;

                //if (_daysBack > 0)
                //{
                //    return $"{dayName} (-{_daysBack} dni)";
                //}
                //return dayName;
            }
        }

        public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; }
        public double ChartWidth { get; private set; } = 400;

        public HistoryViewModel(bool useWebSocket, Employee employee)
        {
            _service = new HistoryService();
            _useWebSocket = useWebSocket;
            _employee = employee;

            YAxes = new[]
            {
                new Axis
                {
                    Name = "°C",
                    NamePaint = new SolidColorPaint(SKColors.White),
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 12,
                    MinStep = 1,
                    SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(40), 1),
                    SubseparatorsPaint = null,
                    Position = LiveChartsCore.Measure.AxisPosition.Start
                },
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColors.White),
                    TextSize = 12,
                    MinStep = 1,
                    SeparatorsPaint = null,
                    SubseparatorsPaint = null,
                    Position = LiveChartsCore.Measure.AxisPosition.End,
                    ShowSeparatorLines = false,
                    IsVisible = true
                }
            };

            _ = LoadDataAsync();
        }

        public void NotifySeriesVisibility()
        {
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(HasNoData));
        }

        public void Cancel()
        {
            Series = Array.Empty<ISeries>();
            XAxes = Array.Empty<Axis>();
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
        }

        public async Task LoadDataAsync()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsBusy = true;

            // Wyczyœæ wykres natychmiast przy zmianie dnia
            Series = Array.Empty<ISeries>();
            XAxes = Array.Empty<Axis>();
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));

            try
            {
                ErrorMessage = null;

                var (points, error) = await _service.GetHistoryAsync(
                    _useWebSocket, _employee, _selectedDayIndex);

                if (token.IsCancellationRequested) return;

                if (error == 1)
                    ErrorMessage = "Brak po³¹czenia z urz¹dzeniem";
                else if (error == 2)
                    ErrorMessage = "Urz¹dzenie nie obs³uguje historii";

                if (points == null || points.Count == 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        Series = Array.Empty<ISeries>();
                        XAxes = Array.Empty<Axis>();
                        OnPropertyChanged(nameof(Series));
                        OnPropertyChanged(nameof(XAxes));
                        OnPropertyChanged(nameof(CanLoadPreviousDay));
                        IsBusy = false;
                    });
                    return;
                }

                // Dla "Ostatnie 24h" wykryj dzieñ tygodnia z najnowszych danych
                if (_selectedDayIndex == 0 && points.Count > 0)
                {
                    _detectedDayOfWeek = points.Last().Day; // Ostatni punkt = najnowszy
                    System.Diagnostics.Debug.WriteLine($"Wykryto dzieñ tygodnia: {_detectedDayOfWeek}");
                }

                var labels = points.Select(p => p.Time).ToArray();

                // Buduj segmenty linii: niebieski = normalny, czerwony = grzanie
                // Ka¿dy segment zawiera o 1 punkt wiêcej na granicach (overlap) — linia jest ci¹g³a
                var lineSeriesList = new List<ISeries>();
                int idx = 0;
                while (idx < points.Count)
                {
                    bool heating = points[idx].IsGrzanie;
                    int segStart = idx;
                    while (idx < points.Count && points[idx].IsGrzanie == heating)
                        idx++;
                    int segEnd = idx; // exclusive

                    // Czerwony segment zaczyna siê od ostatniego punktu niebieskiego
                    // Niebieski segment koñczy siê na pierwszym punkcie czerwonego
                    int from = heating && segStart > 0 ? segStart - 1 : segStart;
                    int to = segEnd < points.Count ? segEnd + 1 : segEnd;

                    var values = new double?[points.Count];
                    for (int j = from; j < to; j++)
                        values[j] = double.IsNaN(points[j].Temperature) ? (double?)null : points[j].Temperature;

                    lineSeriesList.Add(new LineSeries<double?>
                    {
                        Name = heating ? "Grzanie" : "Temperatura",
                        Values = values,
                        Stroke = new SolidColorPaint(heating ? SKColors.Red : SKColors.DodgerBlue, 2),
                        GeometryFill = null,
                        GeometryStroke = null,
                        Fill = null,
                        LineSmoothness = 0.4,
                        IsHoverable = false,
                        IsVisibleAtLegend = segStart == 0,
                        ScalesYAt = 0
                    });
                }

                // Przezroczysta seria tylko do tooltipa - jeden punkt = jedna temperatura
                var tooltipSeries = new LineSeries<double?>
                {
                    Name = "Temperatura",
                    Values = points.Select(p => double.IsNaN(p.Temperature) ? (double?)null : p.Temperature).ToArray(),
                    Stroke = new SolidColorPaint(SKColors.Transparent, 0),
                    GeometryFill = new SolidColorPaint(SKColors.Transparent),
                    GeometryStroke = new SolidColorPaint(SKColors.Transparent),
                    Fill = null,
                    IsVisibleAtLegend = false,
                    ScalesYAt = 0
                };

                var oknoSeries = new ScatterSeries<double?>
                {
                    Name = "Okno",
                    Values = points.Select(p => p.IsOknoOtwarte && !double.IsNaN(p.Temperature) ? (double?)p.Temperature : null).ToArray(),
                    Fill = new SolidColorPaint(SKColors.Cyan),
                    GeometrySize = 4,
                    ScalesYAt = 0,
                    IsHoverable = false
                };

                var nieobecnySeries = new ScatterSeries<double?>
                {
                    Name = "Nieobecny",
                    Values = points.Select(p => p.IsNieobecny && !double.IsNaN(p.Temperature) ? (double?)p.Temperature : null).ToArray(),
                    Fill = new SolidColorPaint(SKColors.DimGray),
                    GeometrySize = 8,
                    ScalesYAt = 0,
                    IsHoverable = false
                };

                if (token.IsCancellationRequested) return;

                // Oblicz min/max temperatur do synchronizacji osi Y
                var validTemps = points.Where(p => !double.IsNaN(p.Temperature)).Select(p => p.Temperature).ToList();
                double minTemp = validTemps.Any() ? validTemps.Min() : 0;
                double maxTemp = validTemps.Any() ? validTemps.Max() : 30;

                // Dodaj margines
                double margin = (maxTemp - minTemp) * 0.1;
                if (margin < 1) margin = 1;
                minTemp = Math.Floor(minTemp - margin);
                maxTemp = Math.Ceiling(maxTemp + margin);

                var newXAxes = new[]
                {
                    new Axis
                    {
                        Labels = labels,
                        LabelsPaint = new SolidColorPaint(SKColors.White),
                        TextSize = 10,
                        LabelsRotation = 45
                    }
                };

                var newYAxes = new[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(SKColors.White),
                        TextSize = 12,
                        MinStep = 1,
                        MinLimit = minTemp,
                        MaxLimit = maxTemp,
                        SeparatorsPaint = new SolidColorPaint(SKColors.White.WithAlpha(40), 1),
                        SubseparatorsPaint = null,
                        Position = LiveChartsCore.Measure.AxisPosition.Start,
                        Labeler = value => $"{value:F1}°"
                    },
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(SKColors.White),
                        TextSize = 12,
                        MinStep = 1,
                        MinLimit = minTemp,
                        MaxLimit = maxTemp,
                        SeparatorsPaint = null,
                        SubseparatorsPaint = null,
                        Position = LiveChartsCore.Measure.AxisPosition.End,
                        ShowSeparatorLines = false,
                        IsVisible = true,
                        Labeler = value => $"{value:F1}°"
                    }
                };

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (token.IsCancellationRequested) return;
                    Series = lineSeriesList
                        .Append(tooltipSeries)
                        .Append(nieobecnySeries)
                        .Append(oknoSeries)
                        .ToArray();
                    XAxes = newXAxes;
                    YAxes = newYAxes;
                    ChartWidth = Math.Max(400, points.Count * 18.0);
                    OnPropertyChanged(nameof(Series));
                    OnPropertyChanged(nameof(XAxes));
                    OnPropertyChanged(nameof(YAxes));
                    OnPropertyChanged(nameof(ChartWidth));
                    OnPropertyChanged(nameof(CanLoadPreviousDay));
                    IsBusy = false;
                });
            }
            catch (OperationCanceledException)
            {
                IsBusy = false;
            }
            catch
            {
                ErrorMessage = "Wyst¹pi³ nieoczekiwany b³¹d";
                IsBusy = false;
            }
        }

        private async Task LoadPreviousDayAsync()
        {
            if (IsBusy) return;

            // SprawdŸ limit
            if (_daysBack >= MAX_DAYS_BACK)
            {
                ErrorMessage = $"Osi¹gniêto limit historii ({MAX_DAYS_BACK} dni)";
                await Task.Delay(2000); // Poka¿ przez 2 sekundy
                ErrorMessage = null;
                return;
            }

            _daysBack++; // Zwiêksz licznik cofniêtych dni

            System.Diagnostics.Debug.WriteLine($"LoadPreviousDayAsync: SelectedDayIndex={_selectedDayIndex}, DetectedDay={_detectedDayOfWeek}, DaysBack={_daysBack}");

            _isChangingFromButton = true; // Ustaw flagê przed zmian¹

            // Dla "Ostatnie 24h" - u¿yj wykrytego dnia tygodnia MINUS 1 (poprzedni dzieñ)
            if (_selectedDayIndex == 0)
            {
                if (_detectedDayOfWeek == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Nie wykryto dnia - brak danych");
                    _daysBack--; // Cofnij licznik
                    _isChangingFromButton = false;
                    return;
                }

                // PrzejdŸ do POPRZEDNIEGO dnia wzglêdem wykrytego
                int previousDayIndex = _detectedDayOfWeek > 1 ? _detectedDayOfWeek - 1 : 1;
                SelectedDayIndex = previousDayIndex;
                _isChangingFromButton = false;
                OnPropertyChanged(nameof(CanLoadPreviousDay));
                return;
            }

            // Dla konkretnych dni tygodnia: przejdŸ do poprzedniego dnia (wczeœniejszego)
            if (_selectedDayIndex > 1)
            {
                SelectedDayIndex = _selectedDayIndex - 1;
            }
            else if (_selectedDayIndex == 1)
            {
                SelectedDayIndex = 7; // Niedziela
            }

            _isChangingFromButton = false; // Reset flagi
            OnPropertyChanged(nameof(CanLoadPreviousDay));
            OnPropertyChanged(nameof(CanLoadNextDay));
            await Task.CompletedTask;
        }

        private async Task LoadNextDayAsync()
        {
            if (IsBusy || _daysBack <= 0) return;

            _daysBack--; // Zmniejsz licznik (idziemy do nowszych dni)

            System.Diagnostics.Debug.WriteLine($"LoadNextDayAsync: SelectedDayIndex={_selectedDayIndex}, DetectedDay={_detectedDayOfWeek}, DaysBack={_daysBack}");

            _isChangingFromButton = true; // Ustaw flagê

            // Jeœli dotarliœmy do "dzisiaj", wróæ do "Ostatnie 24h"
            if (_daysBack == 0)
            {
                SelectedDayIndex = 0; // "Ostatnie 24h"
                _isChangingFromButton = false;
                OnPropertyChanged(nameof(CanLoadPreviousDay));
                OnPropertyChanged(nameof(CanLoadNextDay));
                return;
            }

            // Dla konkretnych dni: przejdŸ do nastêpnego dnia (nowszego)
            // Idziemy w stronê wykrytego dnia (dzisiejszego)
            if (_selectedDayIndex == 7)
            {
                // Niedziela -> Poniedzia³ek (lub do wykrytego dnia)
                SelectedDayIndex = 1;
            }
            else if (_selectedDayIndex < 7)
            {
                SelectedDayIndex = _selectedDayIndex + 1;
            }

            _isChangingFromButton = false;
            OnPropertyChanged(nameof(CanLoadPreviousDay));
            OnPropertyChanged(nameof(CanLoadNextDay));
            await Task.CompletedTask;
        }

        // Publiczne metody z akumulacj¹ klikniêæ (debouncing)
        public async Task LoadPreviousDayClickAsync()
        {
            _pendingPreviousClicks++;

            // Natychmiast oblicz i poka¿ docelowy dzieñ (bez ³adowania danych)
            UpdateTargetDayDisplay(backward: true);

            // Anuluj poprzednie oczekiwanie
            _clickDebounceToken?.Cancel();
            _clickDebounceToken = new CancellationTokenSource();
            var token = _clickDebounceToken.Token;

            try
            {
                // W³¹cz animacjê
                IsAnimating = true;

                // Czekaj 400ms (podczas animacji mo¿na dalej klikaæ)
                await Task.Delay(DEBOUNCE_MS, token);

                // Po animacji - za³aduj dane
                IsAnimating = false;

                int clicks = _pendingPreviousClicks;
                _pendingPreviousClicks = 0;

                System.Diagnostics.Debug.WriteLine($"Animacja zakoñczona, ³adujê dane po {clicks} klikniêciach");

                // Za³aduj dane dla aktualnego dnia (_selectedDayIndex i _daysBack ju¿ ustawione)
                _isChangingFromButton = false;
                OnPropertyChanged(nameof(SelectedDayIndex));
                await LoadDataAsync();

                OnPropertyChanged(nameof(CanLoadPreviousDay));
                OnPropertyChanged(nameof(CanLoadNextDay));
            }
            catch (TaskCanceledException)
            {
                // Anulowano - u¿ytkownik klikn¹³ ponownie (animacja restart)
            }
        }

        private void UpdateTargetDayDisplay(bool backward)
        {
            int clicks = backward ? _pendingPreviousClicks : _pendingNextClicks;

            if (backward)
            {
                // Oblicz docelowy dzieñ wstecz
                int targetDaysBack = Math.Min(_daysBack + clicks, MAX_DAYS_BACK);
                int daysToJump = targetDaysBack - _daysBack;

                if (daysToJump <= 0) return;

                _daysBack = targetDaysBack;

                _isChangingFromButton = true;
                int targetIndex = _selectedDayIndex;

                if (_selectedDayIndex == 0)
                {
                    if (_detectedDayOfWeek == 0) return;
                    targetIndex = _detectedDayOfWeek - daysToJump;
                    while (targetIndex < 1) targetIndex += 7;
                }
                else
                {
                    targetIndex = _selectedDayIndex - daysToJump;
                    while (targetIndex < 1) targetIndex += 7;
                }

                _selectedDayIndex = targetIndex;
                OnPropertyChanged(nameof(CurrentDayDisplay));
            }
            else
            {
                // Oblicz docelowy dzieñ do przodu
                int targetDaysBack = Math.Max(_daysBack - clicks, 0);
                int daysToJump = _daysBack - targetDaysBack;

                if (daysToJump <= 0) return;

                _daysBack = targetDaysBack;

                if (_daysBack == 0)
                {
                    _isChangingFromButton = true;
                    _selectedDayIndex = 0;
                    OnPropertyChanged(nameof(CurrentDayDisplay));
                    return;
                }

                _isChangingFromButton = true;
                int targetIndex = _selectedDayIndex + daysToJump;
                while (targetIndex > 7) targetIndex -= 7;

                _selectedDayIndex = targetIndex;
                OnPropertyChanged(nameof(CurrentDayDisplay));
            }
        }

        public async Task LoadNextDayClickAsync()
        {
            _pendingNextClicks++;

            // Natychmiast oblicz i poka¿ docelowy dzieñ
            UpdateTargetDayDisplay(backward: false);

            _clickDebounceToken?.Cancel();
            _clickDebounceToken = new CancellationTokenSource();
            var token = _clickDebounceToken.Token;

            try
            {
                // W³¹cz animacjê
                IsAnimating = true;

                // Czekaj 400ms
                await Task.Delay(DEBOUNCE_MS, token);

                // Po animacji - za³aduj dane
                IsAnimating = false;

                int clicks = _pendingNextClicks;
                _pendingNextClicks = 0;

                System.Diagnostics.Debug.WriteLine($"Animacja zakoñczona, ³adujê dane po {clicks} klikniêciach");

                _isChangingFromButton = false;
                OnPropertyChanged(nameof(SelectedDayIndex));
                await LoadDataAsync();

                OnPropertyChanged(nameof(CanLoadPreviousDay));
                OnPropertyChanged(nameof(CanLoadNextDay));
            }
            catch (TaskCanceledException)
            {
                // Anulowano
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
