using DiagramDesigner.Controls;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiagramDesigner
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //将来用框架的消息通知改写
            ((MainViewModel)DataContext).InitData(Diagram);
        }


        //private void AddSibling_OnClick(object sender, RoutedEventArgs e)
        //{
        //    var param = (DesignerItem)((Button)sender).CommandParameter;
        //    Diagram.AddSibling(param);
        //}

        private void Remove_OnClick(object sender, RoutedEventArgs e)
        {
            var param = (DesignerItem)((Button)sender).CommandParameter;
            Diagram.Delete(param);
        }

        private void AddAfter_OnClick(object sender, RoutedEventArgs e)
        {
            var param = (DesignerItem)((Button)sender).CommandParameter;
            Diagram.AddAfter(param);
        }

        private void Collapse_Click(object sender, RoutedEventArgs e)
        {
            Diagram.Collapse();
        }

        private void Expand_Click(object sender, RoutedEventArgs e)
        {
            Diagram.Expand();
        }

        private void GetData_Click(object sender, RoutedEventArgs e)
        {
            var param = (DiagramControl)((Button)sender).CommandParameter;
            var list = param.ItemDatas;
            MessageBox.Show(string.Format("Changed:{0}\nAdded:{1}\nRemoved:{2}", list.Count(x => x.Changed), list.Count(x => x.Added), list.Count(x => x.Removed)));
        }
    }
}
