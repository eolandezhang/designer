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
        private List<DesignerItem> _shadows;
        private DesignerItem NewParent;
        private DiagramControl DiagramControl
        {
            get
            {
                var designerItem = DataContext as DesignerItem;
                if (designerItem == null) return null;
                var designer = designerItem.Parent as DesignerCanvas;
                if (designer == null) return null;
                var diagramControl = designer.TemplatedParent as DiagramControl;
                return diagramControl;
            }
        }
        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
        }
        void DragThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var designerItem = DataContext as DesignerItem;
            if (designerItem != null)
            {
                var designer = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;
                if (designer == null || !designerItem.IsSelected) return;
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

                #region 为元素产生影子，并且高亮父节点
                var diagramControl = designer.TemplatedParent as DiagramControl;
                if (diagramControl != null)
                {
                    NewParent = diagramControl.DiagramManager.ChangeParent(designerItem);
                    if (_shadows == null || _shadows.Count == 0)
                        _shadows = diagramControl.DiagramManager.CreateShadows(designerItem, designerItem, NewParent);
                    diagramControl.DiagramManager.ConnectToParent(NewParent, designerItem);
                }
                #endregion

                e.Handled = true;
            }
        }




        //拖动前保存元素位置
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            var diagramControl = DiagramControl;
            if (diagramControl == null) return;
            foreach (var item in diagramControl.DesignerItems)
            {
                item.Oldx = Canvas.GetLeft(item);
                item.Oldy = Canvas.GetTop(item);
            }
        }


        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            var diagramControl = DiagramControl;
            if (diagramControl == null) return;
            if (_shadows != null)
                diagramControl.DiagramManager.FinishChangeParent(NewParent);
            _shadows = null;
            NewParent = null;
        }


    }
}
