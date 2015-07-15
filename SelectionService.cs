using DiagramDesigner.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiagramDesigner
{
    internal class SelectionService
    {
        private DiagramControl _diagramControl;
        private DesignerCanvas designerCanvas;
        private List<ISelectable> _currentSelection;
        internal List<ISelectable> CurrentSelection
        {
            get
            {
                if (_currentSelection == null)
                    _currentSelection = new List<ISelectable>();
                return _currentSelection;
            }
        }


        public SelectionService(DesignerCanvas canvas)
        {
            this.designerCanvas = canvas;
        }
        public SelectionService(DesignerCanvas canvas, DiagramControl diagramControl)
        {
            this.designerCanvas = canvas;
            this._diagramControl = diagramControl;
        }

        internal void SelectItem(ISelectable item)
        {
            this.ClearSelection();
            this.AddToSelection(item);


        }

        internal void AddToSelection(ISelectable item)
        {
            var x = item as DesignerItem;
            if (x != null)
            {
                CurrentSelection.Add(item);
                x.IsSelected = true;

                #region 通知控件
                //if (CurrentSelection.Count == 1)
                //{
                //    _diagramControl.SelectedItem = x;
                //    _diagramControl.SelectedItems.Clear();
                //}
                //else if (CurrentSelection.Count > 1)
                //{
                //    _diagramControl.SelectedItem = null;
                //    _diagramControl.SelectedItems.Add(x);
                //}
                #endregion

                //_diagramControl.SelectedItems.Add(x);

                //_diagramControl.SelectedItems.Clear();
                //foreach (var designerItem in CurrentSelection.Cast<DesignerItem>())
                //{
                //    _diagramControl.SelectedItems.Add(designerItem);
                //}


                //if (item is IGroupable)
                //{
                //  List<IGroupable> groupItems = GetGroupMembers(item as IGroupable);

                //foreach (ISelectable groupItem in groupItems)
                //{
                //    groupItem.IsSelected = true;
                //    CurrentSelection.Add(groupItem);
                //}
                //}
                //else
                //{
                //    item.IsSelected = true;
                //    CurrentSelection.Add(item);
                //}
            }
        }

        internal void RemoveFromSelection(ISelectable item)
        {
            var x = item as DesignerItem;
            if (x != null)
            {
                CurrentSelection.Remove(item);
                x.IsSelected = false;

                #region 通知控件
                //_diagramControl.SelectedItem = null;
                //_diagramControl.SelectedItems.Remove(x);
                #endregion
            }

            //if (item is IGroupable)
            //{
            //    List<IGroupable> groupItems = GetGroupMembers(item as IGroupable);

            //    foreach (ISelectable groupItem in groupItems)
            //    {
            //        groupItem.IsSelected = false;
            //        CurrentSelection.Remove(groupItem);
            //    }
            //}
            //else
            //{
            //    item.IsSelected = false;
            //    CurrentSelection.Remove(item);
            //}
        }

        internal void ClearSelection()
        {
            CurrentSelection.ForEach(item => item.IsSelected = false);
            CurrentSelection.Clear();

            #region 通知控件
            //_diagramControl.SelectedItem = null;
            //_diagramControl.SelectedItems.Clear();
            #endregion
        }

        internal void SelectAll()
        {
            ClearSelection();
            CurrentSelection.AddRange(designerCanvas.Children.OfType<ISelectable>());
            CurrentSelection.ForEach(item => item.IsSelected = true);
        }

        internal List<IGroupable> GetGroupMembers(IGroupable item)
        {
            IEnumerable<IGroupable> list = designerCanvas.Children.OfType<IGroupable>();
            IGroupable rootItem = GetRoot(list, item);
            return GetGroupMembers(list, rootItem);
        }

        internal IGroupable GetGroupRoot(IGroupable item)
        {
            IEnumerable<IGroupable> list = designerCanvas.Children.OfType<IGroupable>();
            return GetRoot(list, item);
        }

        private IGroupable GetRoot(IEnumerable<IGroupable> list, IGroupable node)
        {
            if (node == null || node.ParentID == Guid.Empty)
            {
                return node;
            }
            else
            {
                foreach (IGroupable item in list)
                {
                    if (item.ID == node.ParentID)
                    {
                        return GetRoot(list, item);
                    }
                }
                return null;
            }
        }

        private List<IGroupable> GetGroupMembers(IEnumerable<IGroupable> list, IGroupable parent)
        {
            List<IGroupable> groupMembers = new List<IGroupable>();
            groupMembers.Add(parent);

            var children = list.Where(node => node.ParentID == parent.ID);

            foreach (IGroupable child in children)
            {
                groupMembers.AddRange(GetGroupMembers(list, child));
            }

            return groupMembers;
        }
    }
}
