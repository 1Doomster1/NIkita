using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NIkita
{
    public partial class InputDialog : Window
    {
        public string Answer => InputBox.Text;

        public InputDialog(string title, string prompt)
        {
            InitializeComponent();
            Title = title;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(InputBox.Text))
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
