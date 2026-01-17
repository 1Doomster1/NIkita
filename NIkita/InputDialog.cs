using System.Windows;
using System.Windows.Controls;

namespace NikitaMessenger
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; }

        public InputDialog(string title, string prompt)
        {
            InitializeComponent(title, prompt);
        }

        private void InitializeComponent(string title, string prompt)
        {
            Width = 300;
            Height = 180;
            Title = title;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            // Основная панель
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Подсказка
            var promptText = new TextBlock
            {
                Text = prompt,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };

            // Поле ввода
            var textBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            mainPanel.Children.Add(promptText);
            mainPanel.Children.Add(textBox);
            Grid.SetRow(mainPanel, 0);
            grid.Children.Add(mainPanel);

            // Панель кнопок
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 10, 20, 20)
            };

            var okButton = new Button
            {
                Content = "Добавить",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };

            okButton.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    Answer = textBox.Text.Trim();
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Введите имя пользователя",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    textBox.Focus();
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

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            Content = grid;

            // Фокус на поле ввода при загрузке
            Loaded += (s, e) => textBox.Focus();
        }
    }
}