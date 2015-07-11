﻿using System;
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
                    NewParent = diagramControl.DiagramManager.ChangeItemParent(designerItem, _shadows);

                    //var parent = diagramControl.DiagramManager.ChangeParent(designerItem);/*改变父节点*/
                    //else { _shadows.ForEach(x => x.NewParent = parent); }
                    //diagramControl.DiagramManager.HideOthers(designerItem);
                    //diagramControl.DiagramManager.MoveUpAndDown(parent, designerItem);
                    //diagramControl.DiagramManager.HideItemConnection(designerItem, parent);/*拖动时隐藏连线*/
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
            var designerItem = DataContext as DesignerItem;
            var diagramControl = DiagramControl;
            if (diagramControl == null) return;
            diagramControl.DiagramManager.FinishChangeParent(NewParent);
            //diagramControl.DiagramManager.ShowOthers();
            //diagramControl.DiagramManager.RemoveShadows();/*移除影子*/
            //diagramControl.DiagramManager.ArrangeWithRootItems();/*重新布局*/
            //diagramControl.DesignerItems.Where(x => x.IsNewParent).ToList().ForEach(x => x.IsNewParent = false);
            //diagramControl.DiagramManager.ShowItemConnection();/*拖动完毕，显示连线*/
            _shadows = null;
            NewParent = null;
        }


    }
}
