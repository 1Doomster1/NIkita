using System.Windows;

namespace NikitaMessenger
{
    public partial class NewChatWindow : Window
    {
        public string ChatName { get; private set; }

        public NewChatWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void InitializeComponent()
        {
            Width = 400;
            Height = 200;
            WindowStyle = WindowStyle.ToolWindow;
            Title = "Новый чат";
            ResizeMode = ResizeMode.NoResize;

            var grid = new System.Windows.Controls.Grid();
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Auto) });

            // Поле ввода
            var textBox = new System.Windows.Controls.TextBox
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Padding = new Thickness(10)
            };
            System.Windows.Controls.Grid.SetRow(textBox, 0);
            grid.Children.Add(textBox);

            // Текст подсказки
            var hintText = new System.Windows.Controls.TextBlock
            {
                Text = "Введите имя пользователя или название группы",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontSize = 12,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(20, 0, 0, 10)
            };
            System.Windows.Controls.Grid.SetRow(hintText, 1);
            grid.Children.Add(hintText);

            // Кнопки
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                Margin = new Thickness(20)
            };
            System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            var createButton = new System.Windows.Controls.Button
            {
                Content = "Создать",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            createButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    ChatName = textBox.Text.Trim();
                    DialogResult = true;
                    Close();
                }
                else
                {
                    System.Windows.MessageBox.Show("Введите название чата", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            var cancelButton = new System.Windows.Controls.Button
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

            Content = grid;
        }
    }
}