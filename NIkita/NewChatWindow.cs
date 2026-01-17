using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NikitaMessenger
{
    public partial class NewChatWindow : Window
    {
        public string ChatName { get; private set; }
        public bool IsGroupChat { get; private set; }
        public List<string> SelectedUsers { get; private set; } = new List<string>();
        private List<string> availableUsers;

        public NewChatWindow(List<string> onlineUsers)
        {
            availableUsers = onlineUsers ?? new List<string>();
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void InitializeComponent()
        {
            Width = 400;
            Height = 500;
            WindowStyle = WindowStyle.ToolWindow;
            Title = "Новый чат";
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Поле ввода имени чата
            var namePanel = new StackPanel
            {
                Margin = new Thickness(20, 20, 20, 10)
            };

            var nameLabel = new TextBlock
            {
                Text = "Название чата:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var nameTextBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(5)
            };

            namePanel.Children.Add(nameLabel);
            namePanel.Children.Add(nameTextBox);
            Grid.SetRow(namePanel, 0);
            grid.Children.Add(namePanel);

            // Список пользователей
            var usersPanel = new StackPanel
            {
                Margin = new Thickness(20, 10, 20, 10)
            };

            var usersLabel = new TextBlock
            {
                Text = "Выберите пользователей:",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };

            usersPanel.Children.Add(usersLabel);

            if (availableUsers.Any())
            {
                var scrollViewer = new ScrollViewer
                {
                    Height = 200,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                var usersStackPanel = new StackPanel();

                foreach (var user in availableUsers)
                {
                    var checkBox = new CheckBox
                    {
                        Content = user,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 0, 5),
                        Tag = user
                    };

                    checkBox.Checked += (s, e) =>
                    {
                        if (!SelectedUsers.Contains(user))
                            SelectedUsers.Add(user);
                    };

                    checkBox.Unchecked += (s, e) =>
                    {
                        SelectedUsers.Remove(user);
                    };

                    usersStackPanel.Children.Add(checkBox);
                }

                scrollViewer.Content = usersStackPanel;
                usersPanel.Children.Add(scrollViewer);
            }
            else
            {
                var noUsersText = new TextBlock
                {
                    Text = "Нет онлайн пользователей",
                    FontSize = 13,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                usersPanel.Children.Add(noUsersText);
            }

            Grid.SetRow(usersPanel, 1);
            grid.Children.Add(usersPanel);

            // Checkbox для группового чата
            var groupChatCheckBox = new CheckBox
            {
                Content = "Создать групповой чат",
                FontSize = 14,
                Margin = new Thickness(20, 10, 20, 10)
            };

            groupChatCheckBox.Checked += (s, e) => IsGroupChat = true;
            groupChatCheckBox.Unchecked += (s, e) => IsGroupChat = false;

            Grid.SetRow(groupChatCheckBox, 2);
            grid.Children.Add(groupChatCheckBox);

            // Кнопки
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20)
            };

            var createButton = new Button
            {
                Content = "Создать",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            createButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    ChatName = nameTextBox.Text.Trim();

                    if (IsGroupChat && SelectedUsers.Count == 0)
                    {
                        MessageBox.Show("Для группового чата выберите хотя бы одного пользователя",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Введите название чата", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            var cancelButton = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 30,
                IsCancel = true
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(createButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }
    }
}