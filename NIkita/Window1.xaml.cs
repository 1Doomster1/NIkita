using System.Windows;

namespace NIkita
{
    public partial class UsernameDialog : Window
    {
        public string Username { get; private set; }

        public UsernameDialog()
        {
            InitializeComponent();
            Loaded += (s, e) => UsernameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                Username = UsernameTextBox.Text.Trim();
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Введите имя пользователя",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                UsernameTextBox.Focus();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}