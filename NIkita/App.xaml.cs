using System;
using System.Windows;

namespace NIkita
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (Application.Current.MainWindow.DataContext is MainViewModel vm)
            {
                vm.SaveChatHistory();
            }
            base.OnExit(e);
        }
    }
}