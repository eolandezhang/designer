using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner.Controls
{
    public class DragThumb : Thumb
    {
        double _originalLeft;
        double _originalTop;

        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
        }

        private DesignerItem _shadow = null;
        void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            var designer = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;
            if (designerItem == null || designer == null || !designerItem.IsSelected) return;
            var minLeft = double.MaxValue;
            var minTop = double.MaxValue;

            // we only move DesignerItems
            var designerItems = designer.SelectionService.CurrentSelection.OfType<DesignerItem>();

            foreach (var item in designerItems)
            {
                var left = Canvas.GetLeft(item);
                var top = Canvas.GetTop(item);

                minLeft = double.IsNaN(left) ? 0 : Math.Min(left, minLeft);
                minTop = double.IsNaN(top) ? 0 : Math.Min(top, minTop);
            }

            var deltaHorizontal = Math.Max(-minLeft, e.HorizontalChange);
            var deltaVertical = Math.Max(-minTop, e.VerticalChange);

            foreach (var item in designerItems)
            {
                var left = Canvas.GetLeft(item);
                var top = Canvas.GetTop(item);

                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                Canvas.SetLeft(item, left + deltaHorizontal);
                Canvas.SetTop(item, top + deltaVertical);
            }

            designer.InvalidateMeasure();
            e.Handled = true;

            #region 为元素产生影子，并且高亮父节点
            if (_shadow == null)
                _shadow = DiagramManager.GenerateShadow(designerItem, _originalLeft, _originalTop);
            DiagramManager.HighlightParent(designerItem, designer);/*拖动节点时，高亮父节点*/

            #endregion
        }

        //拖动前保存元素位置
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                _originalLeft = (double)designerItem.GetValue(Canvas.LeftProperty);
                _originalTop = (double)designerItem.GetValue(Canvas.TopProperty);
            }
            DiagramManager.HighlightSelected(designerItem);
        }


        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                DiagramManager.FinishDraging(designerItem, _originalLeft, _originalTop);
            }
            _shadow = null;
        }


    }
}
