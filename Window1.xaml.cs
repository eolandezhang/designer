using DiagramDesigner.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiagramDesigner
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            var data = new ObservableCollection<IItemData>()
            {
                new CustomItemData(Diagram,"d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false),
                new CustomItemData(Diagram,"d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,2),
                new CustomItemData(Diagram,"d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,1)
            };
            Diagram.ItemDatas = data;
            
        }
        

        private void AddSibling_OnClick(object sender, RoutedEventArgs e)
        {
            var param = (DesignerItem)((Button)sender).CommandParameter;
            Diagram.AddSibling(param);
        }

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
