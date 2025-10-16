using System;
using System.Windows;
using System.Windows.Threading;

namespace AgOpenGPS.WpfApp.Notifications
{
    public partial class TimedMessageWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public TimedMessageWindow(TimeSpan lifetime, string title, string message)
        {
            InitializeComponent();

            Title = title;
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;

            _timer = new DispatcherTimer
            {
                Interval = lifetime <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : lifetime
            };
            _timer.Tick += OnTimerTick;
            Loaded += (_, _) => _timer.Start();
            Closed += (_, _) => _timer.Stop();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _timer.Stop();
            Close();
        }
    }
}
