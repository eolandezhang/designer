using System;
using System.Collections.Generic;
using System.Windows;
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
        private DiagramControl _diagramControl;
        private DiagramControl DiagramControl
        {
            get
            {
                if (_diagramControl != null) return _diagramControl;
                var designerItem = DataContext as DesignerItem;
                if (designerItem == null) return null;
                var designer = designerItem.Parent as DesignerCanvas;
                if (designer == null) return null;
                _diagramControl = designer.TemplatedParent as DiagramControl;
                return _diagramControl;
            }
        }
        public DragThumb()
        {
            DragDelta += DragThumb_DragDelta;
        }



        private double _verticalOffset = 0;
        private double _horizontalOffset = 0;
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
                ChangeParent(designerItem);


                VerticalScroll(designerItem, e.VerticalChange);
                HorizontalScroll(designerItem, e.HorizontalChange);
            }
        }

        void VerticalScroll(DesignerItem designerItem, double VerticalChange)
        {
            _verticalOffset += VerticalChange;
            if (_verticalOffset < designerItem.ActualHeight)
            {
                var yPos = Canvas.GetTop(designerItem);
                var sv = (ScrollViewer)DiagramControl.Template.FindName("DesignerScrollViewer", DiagramControl);
                if (sv.VerticalOffset + sv.ViewportHeight - 100 < yPos && VerticalChange > 0)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset + VerticalChange);
                }
                else if (yPos < sv.VerticalOffset + 100 && VerticalChange < 0)
                {
                    sv.ScrollToVerticalOffset(sv.VerticalOffset + VerticalChange);
                }

            }
            else if (_verticalOffset > designerItem.ActualHeight)
            {
                _verticalOffset = 0;
            }
        }
        void HorizontalScroll(DesignerItem designerItem, double HorizontalChange)
        {
            _horizontalOffset += HorizontalChange;
            if (_horizontalOffset < designerItem.ActualWidth)
            {
                var xPos = Canvas.GetLeft(designerItem);
                var sv = (ScrollViewer)DiagramControl.Template.FindName("DesignerScrollViewer", DiagramControl);
                if (sv.HorizontalOffset + sv.ViewportWidth - designerItem.ActualWidth < xPos && HorizontalChange > 0)
                {
                    sv.ScrollToHorizontalOffset(sv.HorizontalOffset + HorizontalChange);
                }
                else if (xPos < sv.HorizontalOffset + designerItem.ActualWidth && HorizontalChange < 0)
                {
                    sv.ScrollToHorizontalOffset(sv.HorizontalOffset + HorizontalChange);
                }
            }
            else if (_horizontalOffset > designerItem.ActualWidth)
            {
                _horizontalOffset = 0;
            }
        }

        void ChangeParent(DesignerItem designerItem)
        {
            DiagramControl.DiagramManager.SetDragItemChildFlag();
            NewParent = DiagramControl.DiagramManager.ChangeParent(designerItem);
            if (_shadows == null) { _shadows = DiagramControl.DiagramManager.CreateShadows(designerItem, NewParent); }
            DiagramControl.DiagramManager.CreateHelperConnection(NewParent, designerItem);
            DiagramControl.DiagramManager.MoveUpAndDown(NewParent, designerItem);
        }

        //拖动前保存元素位置
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            _verticalOffset = 0; _horizontalOffset = 0;
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
            DiagramControl.DiagramManager.RestoreDragItemChildFlag();
            _verticalOffset = 0;
            _horizontalOffset = 0;
        }


    }
}
