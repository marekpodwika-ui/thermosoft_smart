using thermosoft.ViewModels;
using thermosoft.Models;
using System.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;

namespace thermosoft.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HistoryPage : ContentPage
    {
        private HistoryViewModel _vm;
        private double _lastPanX = 0;

        public HistoryPage(bool useWebSocket, Employee employee)
        {
            InitializeComponent();
            Title = employee.Na;
            _vm = new HistoryViewModel(useWebSocket, employee);
            BindingContext = _vm;
            _vm.PropertyChanged += OnViewModelPropertyChanged;

            // Konfiguracja tooltipa z formatowaniem miejsc po przecinku
            Chart.TooltipTextPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.White);
            Chart.TooltipBackgroundPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SkiaSharp.SKColors.Black.WithAlpha(200));
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HistoryViewModel.IsAnimating))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (_vm.IsAnimating)
                    {
                        // Fade out
                        await ChartContainer.FadeTo(0.3, 200); // 200ms do opacity 0.3
                    }
                    else
                    {
                        // Fade in
                        await ChartContainer.FadeTo(1.0, 200); // 200ms do opacity 1.0
                    }
                });
            }
            else if (e.PropertyName == nameof(HistoryViewModel.Series))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // null wymusza pe³ne odœwie¿enie w LiveCharts
                    Chart.Series = null;
                    Chart.Series = _vm.Series?.Length > 0 ? _vm.Series : null;
                    _vm.NotifySeriesVisibility();
                });
            }
            else if (e.PropertyName == nameof(HistoryViewModel.XAxes))
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Chart.XAxes = _vm.XAxes;
                });
            }
            else if (e.PropertyName == nameof(HistoryViewModel.ChartWidth))
            {
                // Po za³adowaniu wykresu przewiñ na koniec (prawo)
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // Krótkie opóŸnienie dla uk³adu
                    await ChartScrollView.ScrollToAsync(ChartScrollView.ContentSize.Width, 0, false);
                });
            }
        }

        private void OnScrollViewSizeChanged(object sender, EventArgs e)
        {
            if (ChartScrollView.Height > 0)
                ChartContainer.HeightRequest = ChartScrollView.Height;
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            const double sensitivity = 2; // Spowolnienie przewijania (0.8 = szybsze przewijanie)

            if (e.StatusType == GestureStatus.Started)
            {
                _lastPanX = 0;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                double deltaX = (e.TotalX - _lastPanX) * sensitivity;
                _lastPanX = e.TotalX;

                double newScrollX = ChartScrollView.ScrollX - deltaX;
                ChartScrollView.ScrollToAsync(newScrollX, 0, false);
            }
            else if (e.StatusType == GestureStatus.Completed || e.StatusType == GestureStatus.Canceled)
            {
                _lastPanX = 0;
            }
        }

        private async void OnLoadPreviousDayTapped(object sender, EventArgs e)
        {
            await _vm.LoadPreviousDayClickAsync();
        }

        private async void OnLoadNextDayTapped(object sender, EventArgs e)
        {
            await _vm.LoadNextDayClickAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm.Cancel();
            ChartContainer.Children.Clear();
        }
    }
}
