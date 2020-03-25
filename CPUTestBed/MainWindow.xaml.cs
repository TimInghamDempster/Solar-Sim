using System.Windows;
using CPUTestBed;

namespace CPUTestBes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new Simulation();
            InitializeComponent();
        }
    }
}
