using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NikitaMicrosoft
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

            // Автоподключение для теста
            Loaded += async (s, e) =>
            {
                if (DataContext is AppViewModel viewModel)
                {
                    await viewModel.ConnectToServerAsync();
                }
            };

            Closing += (s, e) =>
            {
                if (DataContext is AppViewModel viewModel)
                {
                    viewModel.DisconnectFromServer();
                }
            };
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

        private void ChatItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Chat chat)
            {
                if (DataContext is AppViewModel vm)
                {
                    vm.SelectChat(chat);
                }
            }
        }

        private void UserItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is User user)
            {
                if (DataContext is AppViewModel vm)
                {
                    vm.StartPrivateChatCommand.Execute(user);
                }
            }
        }

        private void MessageTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Автоматическое изменение высоты TextBox
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.Height = textBox.LineCount * 24 + 10;
            }
        }
    }
}