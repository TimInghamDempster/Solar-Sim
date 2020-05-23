using System.Windows;

namespace CPUTestBed
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            DataContext = new GridBased.GridSimulation();
            InitializeComponent();
        }
    }
}
