using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner.Controls
{
    public class DragThumb : Thumb
    {
        //double _originalLeft;
        //double _originalTop;

        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
        }

        private List<DesignerItem> _shadows = null;
        void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                var designer = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;
                if (designerItem == null || designer == null || !designerItem.IsSelected) return;
                var minLeft = double.MaxValue;
                var minTop = double.MaxValue;

                // we only move DesignerItems
                var designerItems = designer.SelectionService.CurrentSelection.ConvertAll((x) => x as DesignerItem);

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

                var diagramControl = designer.TemplatedParent as DiagramControl;
                if (diagramControl != null)
                {

                    _shadows = diagramControl.DiagramManager.CreateShadows(designerItems);
                    diagramControl.DiagramManager.HighlightParent(designerItem);/*拖动节点时，高亮父节点*/
                }
                #endregion
                designerItem.Data.XIndex = Canvas.GetLeft(designerItem);
                designerItem.Data.YIndex = Canvas.GetTop(designerItem);

            }


        }



        //拖动前保存元素位置
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                var canvas = designerItem.Parent as DesignerCanvas;
                if (canvas != null)
                {
                    var control = canvas.TemplatedParent as DiagramControl;

                    if (control != null)
                        foreach (var item in control.DiagramManager.GetDesignerItems())
                        {
                            item.oldx = Canvas.GetLeft(item);
                            item.oldy = Canvas.GetTop(item);
                        }
                }
            }
        }
        DiagramControl DiagramControl()
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                var designer = designerItem.Parent as DesignerCanvas;
                if (designer == null) return null;
                var diagramControl = designer.TemplatedParent as DiagramControl;
                return diagramControl;
            }
            return null;
        }

        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                var diagramControl = DiagramControl();
                if (diagramControl != null)
                    diagramControl.DiagramManager.FinishDraging(designerItem);
            }
            _shadows = null;
        }


    }
}
