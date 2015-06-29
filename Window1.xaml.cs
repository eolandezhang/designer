using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DiagramDesigner
{
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
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
            var list = param.DataSourceRepository.DataSources;
            MessageBox.Show(string.Format("Changed:{0},Added:{1},Removed:{2}", list.Count(x => x.Changed), list.Count(x => x.Added), list.Count(x => x.Removed)));
        }
    }
}
