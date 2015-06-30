using System.Windows;
using System.Windows.Controls;
using DiagramDesigner.Data;

namespace DiagramDesigner.Controls
{
    public class DiagramDataEditorControl : ContentControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header", typeof(string), typeof(DiagramDataEditorControl), new FrameworkPropertyMetadata(default(string)));

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }



        public ItemDataBase DataSource
        {
            get
            {
                return (ItemDataBase)GetValue(DataSourceProperty);
            }
            set
            {
                SetValue(DataSourceProperty, value);

            }
        }

        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource",
            typeof(ItemDataBase), typeof(DiagramDataEditorControl),
            new FrameworkPropertyMetadata(null, (d, e) =>
            {
                var c = d as DiagramDataEditorControl;
                //if (c != null)
                //{
                //    if (e.NewValue == null) return;
                //    c.ItemData = (e.NewValue as DesignerItem).Data;
                //}
                if (c != null)
                {
                    if (c.DataSource != null)
                        c.DataContext = e.NewValue as ItemDataBase;
                }
            }));


        public DiagramDataEditorControl()
        {

        }

        //public DiagramControl DiagramControl { get; set; }

        //private ItemDataBase _itemData = null;
        //public ItemDataBase ItemData
        //{
        //    get
        //    {
        //        if (_itemData != null) return _itemData;
        //        if (DiagramControl == null) return null;
        //        return DiagramControl.SelectedItem == null ? null : DiagramControl.SelectedItem.Data;
        //    }
        //    set
        //    {
        //        _itemData = value;
        //        DataContext = _itemData;
        //        if (DiagramControl != null)
        //            DiagramControl.BindData();
        //    }
        //}

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var header = (GroupBox)GetTemplateChild("Header");
            if (header != null) header.Header = Header;
        }

    }
}
