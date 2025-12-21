using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NIkita
{
    public partial class MainWindow : Window
    {
        Server.Server server = null;
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new AppViewModel();
        }
    }
}