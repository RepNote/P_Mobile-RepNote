using Microsoft.Maui.Devices.Sensors;
/*Lieu: ETML
Auteur: Thomas Peltier
Date: 20.05.2026*/
namespace RepNote
{
    public partial class App : Application
    {
        private const double ShakeThreshold = 2.5;
        private bool _isShaking = false;

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            StartListeningShake();
        }

        private void StartListeningShake()
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.Game);
                }
            }
        }

        private async void OnAccelerometerReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;

            double gForce = Math.Sqrt(data.Acceleration.X * data.Acceleration.X +
                                      data.Acceleration.Y * data.Acceleration.Y +
                                      data.Acceleration.Z * data.Acceleration.Z);

            if (gForce > ShakeThreshold && !_isShaking)
            {
                _isShaking = true;

                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                }
                catch { }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync("///MainPage");
                });

                await Task.Delay(1000);
                _isShaking = false;
            }
        }
    }
}