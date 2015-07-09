using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner.Controls
{
    public class DragThumb : Thumb
    {
        private List<DesignerItem> _shadows;
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
                e.Handled = true;

                #region 为元素产生影子，并且高亮父节点
                var diagramControl = designer.TemplatedParent as DiagramControl;
                if (diagramControl != null)
                {
                    diagramControl.DiagramManager.HideOthers();
                    diagramControl.DiagramManager.HideItemConnection(designerItem);/*拖动时隐藏连线*/
                    var parent = diagramControl.DiagramManager.HighlightParent(designerItem);/*拖动节点时，高亮父节点*/
                    diagramControl.DiagramManager.MoveUpAndDown(parent);
                    if (_shadows == null)
                        _shadows = diagramControl.DiagramManager.CreateShadows(designerItem);
                    diagramControl.DiagramManager.ChangeParent();/*改变父节点*/
                }
                #endregion
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
            diagramControl.DiagramManager.ShowOthers();
            diagramControl.DiagramManager.ShowItemConnection();/*拖动完毕，显示连线*/
            diagramControl.DiagramManager.RemoveShadows();/*移除影子*/
            diagramControl.DiagramManager.ArrangeWithRootItems();/*重新布局*/
            diagramControl.DiagramManager.ResetBrushBorderFontStyle(diagramControl.Designer);/*恢复边框字体样式*/
            diagramControl.DiagramManager.HighlightSelected();
            _shadows = null;
        }


    }
}
