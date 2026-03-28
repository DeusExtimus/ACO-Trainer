using System;
using System.Windows;

namespace ACOverlay
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                MessageBox.Show(ex.ExceptionObject?.ToString(), "Unhandled Exception");
            DispatcherUnhandledException += (s, ex) =>
            {
                MessageBox.Show(ex.Exception?.ToString(), "Dispatcher Exception");
                ex.Handled = true;
            };
            ApiServer.Start(54321);
            base.OnStartup(e);
        }
    }
}
