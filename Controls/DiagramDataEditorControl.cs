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

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var header = (GroupBox)GetTemplateChild("Header");
            if (header != null) header.Header = Header;
        }

    }
}
