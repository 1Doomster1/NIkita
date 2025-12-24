using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace NIkita
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new AppViewModel();

            if (DataContext is AppViewModel vm)
            {
                vm.MessageAdded += (sender, e) => ScrollToBottom();
            }
        }

        private void ScrollToBottom()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                MessagesScrollViewer?.ScrollToEnd();
            }));
        }

        private void MessageTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                if (DataContext is AppViewModel vm && vm.CanSendMessage)
                {
                    vm.SendMessageCommand.Execute(null);
                }
            }
        }
    }
}