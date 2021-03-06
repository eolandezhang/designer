﻿using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using DiagramDesigner.Controls;

namespace DiagramDesigner
{
    public class RubberbandAdorner : Adorner
    {
        private Point? startPoint;
        private Point? endPoint;
        private Pen rubberbandPen;
        private DiagramControl DiagramControl;
        private DesignerCanvas designerCanvas;

        public RubberbandAdorner(DiagramControl diagramControl, DesignerCanvas designerCanvas, Point? dragStartPoint)
            : base(designerCanvas)
        {
            this.designerCanvas = designerCanvas;
            this.startPoint = dragStartPoint;
            rubberbandPen = new Pen(Brushes.LightSlateGray, 1);
            rubberbandPen.DashStyle = new DashStyle(new double[] { 2 }, 1);
            DiagramControl = diagramControl;
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!this.IsMouseCaptured)
                    this.CaptureMouse();

                endPoint = e.GetPosition(this);
                UpdateSelection();
                this.InvalidateVisual();
            }
            else
            {
                if (this.IsMouseCaptured) this.ReleaseMouseCapture();
            }

            e.Handled = true;
        }

        protected override void OnMouseUp(System.Windows.Input.MouseButtonEventArgs e)
        {
            // release mouse capture
            if (this.IsMouseCaptured) this.ReleaseMouseCapture();

            // remove this adorner from adorner layer
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this.designerCanvas);
            if (adornerLayer != null)
                adornerLayer.Remove(this);
            e.Handled = true;


            if (DiagramControl != null)
            {
                var selectedItems = designerCanvas.SelectionService.CurrentSelection;
                if (selectedItems.Count == 1)
                { DiagramControl.SelectedItem = selectedItems.FirstOrDefault() as DesignerItem; }
                foreach (var selectedItem in selectedItems.ConvertAll(x => x as DesignerItem))
                {
                    DiagramControl.SelectedItems.Clear();
                    DiagramControl.SelectedItems.Add(selectedItem);
                }
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            // without a background the OnMouseMove event would not be fired!
            // Alternative: implement a Canvas as a child of this adorner, like
            // the ConnectionAdorner does.
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

            if (this.startPoint.HasValue && this.endPoint.HasValue)
                dc.DrawRectangle(Brushes.Transparent, rubberbandPen, new Rect(this.startPoint.Value, this.endPoint.Value));
        }

        private void UpdateSelection()
        {
            designerCanvas.SelectionService.ClearSelection();

            Rect rubberBand = new Rect(startPoint.Value, endPoint.Value);
            foreach (Control item in designerCanvas.Children)
            {
                Rect itemRect = VisualTreeHelper.GetDescendantBounds(item);
                Rect itemBounds = item.TransformToAncestor(designerCanvas).TransformBounds(itemRect);

                //rubberBand.Contains
                if (rubberBand.IntersectsWith(itemBounds))
                {
                    if (item is Connection)
                        designerCanvas.SelectionService.AddToSelection(item as ISelectable);
                    else
                    {
                        DesignerItem di = item as DesignerItem;
                        if (di.ParentID == null)
                        {
                            designerCanvas.SelectionService.AddToSelection(di);
                        }
                    }
                }
            }
        }
    }
}
