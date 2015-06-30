using System.Windows;
using System.Windows.Controls;

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
        public DiagramControl DiagramControl { get; set; }

        private IItemData _itemData = null;
        public IItemData ItemData
        {
            get
            {
                if (_itemData != null) return _itemData;
                if (DiagramControl == null) return null;
                return DiagramControl.SelectedItem == null ? null : DiagramControl.SelectedItem.Data;
            }
            set
            {
                _itemData = value;
                DataContext = _itemData;
                if (DiagramControl != null)
                    DiagramControl.BindData();
            }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var header = (GroupBox)GetTemplateChild("Header");
            if (header != null) header.Header = Header;
        }

    }
}
