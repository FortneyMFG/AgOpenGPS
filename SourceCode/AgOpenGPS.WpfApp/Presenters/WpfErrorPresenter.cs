using AgOpenGPS.Core.Interfaces;
using AgOpenGPS.WpfApp.Notifications;
using System;
using System.Windows;

namespace AgOpenGPS.WpfApp.Presenters
{
    public class WpfErrorPresenter : IErrorPresenter
    {
        void IErrorPresenter.PresentTimedMessage(TimeSpan timeSpan, string titleString, string messageString)
        {
            void ShowWindow()
            {
                var window = new TimedMessageWindow(timeSpan, titleString, messageString)
                {
                    Owner = Application.Current?.MainWindow
                };
                window.Show();
                window.Activate();
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                ShowWindow();
                return;
            }

            if (dispatcher.CheckAccess())
            {
                ShowWindow();
            }
            else
            {
                dispatcher.BeginInvoke((Action)ShowWindow);
            }
        }
    }
}
