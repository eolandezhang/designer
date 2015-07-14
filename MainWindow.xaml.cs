using System.Windows;

namespace DiagramDesigner
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = DataContext as MainViewModel;
            if (vm != null) vm.DiagramControl = Diagram;
        }


    }
}
