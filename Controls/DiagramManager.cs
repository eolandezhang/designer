using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiagramDesigner
{
    /*
     *DesignerItem的结构是
     *Border
     *TextBlock
     *如果结构发生变化，需要更改 Helpers region中的方法
     */


    public class DiagramManager
    {
        readonly DiagramControl _diagramControl;

        public DiagramManager(DiagramControl diagramControl)
        {
            _diagramControl = diagramControl;
        }

        #region Setters
        private const double CHILD_TOP_OFFSET = 27;
        private const double LEFT_OFFSET = 20;
        private const int DEFAULT_BORDER_THICKNESS = 2;
        private const int HIGHLIGHT_BORDER_THICKNESS = 2;
        private const double MIN_ITEM_WIDTH = 150d;
        private const double FONT_SIZE = 12d;
        static readonly SolidColorBrush DEFAULT_BORDER_BRUSH = Brushes.SkyBlue;
        static readonly SolidColorBrush SELECTED_BORDER_BRUSH = Brushes.DeepSkyBlue;
        static readonly SolidColorBrush HIGHLIGHT_BORDER_BRUSH = Brushes.IndianRed;
        private static readonly SolidColorBrush HIGHLIGHT_BACKGROUND_BRUSH = Brushes.LightSkyBlue;
        private static readonly SolidColorBrush DEFAULT_BACKGROUND_BRUSH = Brushes.GhostWhite;
        static readonly SolidColorBrush SHADOW_BORDER_BRUSH = Brushes.Blue;
        static readonly SolidColorBrush MOVE_ITEM_BORDER_THICKNESS = Brushes.Gray;
        private static readonly SolidColorBrush SHADOW_BACKGROUND_BRUSH = Brushes.WhiteSmoke;
        private static readonly SolidColorBrush SHADOW_FONT_COLOR_BRUSH = Brushes.Gray;
        private static readonly SolidColorBrush DEFAULT_FONT_COLOR_BRUSH = Brushes.Black;
        private const string PARENT_CONNECTOR = "Bottom";
        private const string CHILD_CONNECTOR = "Left";

        #endregion

        #region Get

        private Connector GetItemConnector/*根据名称，取得元素连接点*/(DesignerItem item, string name)
        {
            var itemConnectorDecorator = item.Template.FindName("PART_ConnectorDecorator", item) as Control;
            if (itemConnectorDecorator == null) return null;
            var itemConnector = itemConnectorDecorator.Template.FindName(name, itemConnectorDecorator) as Connector;
            return itemConnector;
        }
        private List<Connector> GetItemConnectors/*取得所有连接点*/(DesignerItem designerItem)
        {
            var connectors = new List<Connector>();

            var leftItemConnector = GetItemConnector(designerItem, "Left");
            if (leftItemConnector != null) connectors.Add(leftItemConnector);

            var bottomItemConnector = GetItemConnector(designerItem, "Bottom");
            if (bottomItemConnector != null) connectors.Add(bottomItemConnector);

            var topItemConnector = GetItemConnector(designerItem, "Top");
            if (topItemConnector != null) connectors.Add(topItemConnector);

            var rightItemConnector = GetItemConnector(designerItem, "Right");
            if (rightItemConnector != null) connectors.Add(rightItemConnector);
            return connectors;
        }
        public IEnumerable<Connection> GetItemConnections/*取得所有连线*/(DesignerItem designerItem)
        {
            var connections = new List<Connection>();
            var list = GetItemConnectors(designerItem);
            if (list.Count == 0) return connections;
            foreach (var c in list.Select(connector => connector.Connections.Where(x => x.Source != null && x.Sink != null)).Where(c => c.Any()))
            {
                connections.AddRange(c);
            }
            return connections;
        }

        private List<DesignerItem> GetDirectSubItems/*取得直接子节点*/(DesignerItem item)
        {
            var list =
                _diagramControl.DesignerItems.Where(x => x.Data.ParentId == item.ID && !x.Data.Removed).OrderBy(x => x.Data.YIndex).ToList();

            if (item.CanCollapsed == false)
            {
                item.IsExpanderVisible = false;
            }
            else if (list.Any())
            {
                item.IsExpanderVisible = true;
            }
            else
            {
                item.IsExpanderVisible = false;
            }
            return list;
        }
        public void GetAllSubItems/*取得直接及间接的子节点*/(DesignerItem item/*某个节点*/, List<DesignerItem> subitems/*其所有子节点*/)
        {
            var child = new List<DesignerItem>();
            var list =
                _diagramControl.DesignerItems.Where(x => x.Data.ParentId == item.ID && !x.Data.Removed).OrderBy(x => x.Data.YIndex).ToList();

            foreach (var subItem in list.Where(subItem => !subitems.Contains(subItem)))
            {
                child.Add(subItem);
                subitems.Add(subItem);
                foreach (var designerItem in child)
                {
                    GetAllSubItems(designerItem, subitems);
                }
            }
        }

        private DesignerItem GetParentItem/*父节点*/(DesignerItem item)
        {

            return _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == item.Data.ParentId);

        }


        #endregion

        #region Set

        public void SetSelectItem(DesignerItem designerItem)
        {
            _diagramControl.DesignerCanvas.SelectionService.ClearSelection();
            _diagramControl.DesignerCanvas.SelectionService.SelectItem(designerItem);
        }

        #endregion

        #region Create
        public List<DesignerItem>/*根节点*/ GenerateItems()
        {
            _diagramControl.DesignerCanvas.Children.Clear();
            if (_diagramControl.DesignerItems == null) return null;
            if (!_diagramControl.DesignerItems.Any()) return null;
            var roots = _diagramControl.DesignerItems.Where(x => x.Data.ParentId.Equals(Guid.Empty)).ToList();
            foreach (var root in roots)
            {
                IList<DesignerItem> designerItems = new List<DesignerItem>();
                CreateItems(root, designerItems);
            }
            return roots;
        }
        private void CreateItems(DesignerItem parentItem, IList<DesignerItem> designerItems)
        {
            if (parentItem == null) return;
            if (designerItems == null) return;
            DesignerItem parentDesignerItem = null;

            if (designerItems.All(x => !x.ID.Equals(parentItem.ID)))
            {
                if (parentItem.Data.ParentId.Equals(Guid.Empty)) //是根节点？
                {
                    parentDesignerItem = CreateRoot(parentItem, parentItem.Data.YIndex, parentItem.Data.XIndex);
                    if (parentDesignerItem != null)
                    {
                        designerItems.Add(parentDesignerItem);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                parentDesignerItem = parentItem;
            }

            var childs = _diagramControl.DesignerItems.Where(x => x.Data.ParentId.Equals(parentItem.ID));

            foreach (var childItem in childs)
            {
                if (designerItems.All(x => !x.ID.Equals(childItem.ID)))
                {
                    var childDesignerItem = CreateChild(parentDesignerItem, childItem);
                    if (childDesignerItem != null)
                    {
                        designerItems.Add(childDesignerItem);
                    }
                }
                CreateItems(childItem, designerItems);
            }
        }
        private DesignerItem CreateRoot/*创建根节点*/(DesignerItem item, double topOffset, double leftOffset)
        {
            var root = CreateDesignerItem(item, topOffset, leftOffset);
            root.CanCollapsed = false;
            root.IsExpanderVisible = false;
            return root;
        }
        private DesignerItem CreateChild(DesignerItem parent, DesignerItem childItem)
        {
            if (parent == null) return null;
            if (childItem.Data.Removed) return null;
            var child = CreateDesignerItem(childItem);/*创建子节点*/

            var source = GetItemConnector(parent, PARENT_CONNECTOR);
            var sink = GetItemConnector(child, "Left");
            if (source == null || sink == null) return null;
            #region 创建连线
            var connections = GetItemConnections(parent).Where(connection
                => connection.Source.Equals(source)
                && connection.Sink.Equals(sink)).ToList();
            if (connections.Count == 0 || connections.FirstOrDefault() == null)
            {
                var conn = new Connection(source, sink); /*创建连线*/
                _diagramControl.DesignerCanvas.Children.Add(conn); /*放到画布上*/
                Panel.SetZIndex(conn, -10000);
            }
            #endregion

            child.CanCollapsed = true;
            return child;/*返回创建的子节点*/
        }
        private DesignerItem CreateDesignerItem/*创建元素*/(DesignerItem item, double topOffset = 0d, double leftOffset = 0d, SolidColorBrush borderBrush = null/*节点边框颜色*/)
        {
            if (item.Data == null) return null;
            CreateDesignerItemContent(item, DEFAULT_FONT_COLOR_BRUSH, borderBrush);
            if (!item.Data.Removed)
            {
                _diagramControl.DesignerCanvas.Children.Add(item);
                Arrange();
                Canvas.SetTop(item, topOffset);
                Canvas.SetLeft(item, leftOffset);
            }
            return item;
        }
        private void CreateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush fontColorBrush, SolidColorBrush borderBrush = null)
        {
            if (item == null) return;
            if (item.Data != null && item.Data.Text == (GetItemText(item))) { return; }
            if (borderBrush == null) borderBrush = DEFAULT_BORDER_BRUSH;
            //var border = new Border()
            //{
            //    BorderThickness = new Thickness(DEFAULT_BORDER_THICKNESS),
            //    BorderBrush = borderBrush,
            //    Background = DEFAULT_BACKGROUND_BRUSH,
            //    VerticalAlignment = VerticalAlignment.Stretch,
            //    HorizontalAlignment = HorizontalAlignment.Stretch,
            //    IsHitTestVisible = false
            //};
            var textblock = new TextBlock()
            {
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 2, 5, 2),
                FontFamily = new FontFamily("Arial"),
                FontSize = FONT_SIZE,
                Foreground = fontColorBrush,
                DataContext = item.Data
            };
            textblock.SetBinding(TextBlock.TextProperty, new Binding("Text"));

            //border.Child = textblock;
            //item.Content = border;

            item.Content = textblock;

            //item.Width = MIN_ITEM_WIDTH;
            //SetWidth(item);
        }

        public void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/()
        {
            var roots = InitDesignerItems();
            if (roots == null) return;
            if (!roots.Any()) return;/*创建DesignerItems*/
            GenerateItems();
            ArrangeWithRootItems();/*将DesignerItems放到画布上，并且创建连线*/

            _diagramControl.DiagramManager.SetSelectItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.Data.ParentId == Guid.Empty));
        }
        #endregion

        #region Arrange

        public void Arrange()
        {
            var sv = Application.Current.MainWindow;
            if (sv == null) return;
            _diagramControl.DesignerCanvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _diagramControl.DesignerCanvas.Arrange(new Rect(0, 0, sv.ActualWidth, sv.ActualHeight));
        }
        public void ArrangeWithRootItems()
        {
            Arrange();
            var items = _diagramControl.DesignerItems.ToList();
            var roots = items.Where(x => x.Data.ParentId.Equals(Guid.Empty));
            foreach (var root in roots)
            {
                ArrangeWithRootItems(root);
            }
        }
        void ArrangeWithRootItems/*给定根节点，重新布局*/(DesignerItem rootItem/*根节点*/)
        {
            if (rootItem == null) return;
            SetWidth(rootItem);
            var directSubItems = GetDirectSubItems(rootItem);
            if (directSubItems == null) return;
            if (directSubItems.Count == 0) return;
            var rootSubItems =
                directSubItems.Where(x => x.Visibility.Equals(Visibility.Visible)).OrderBy(x => x.Data.YIndex).ToList();
            rootItem.Data.YIndex = (double)rootItem.GetValue(Canvas.TopProperty);
            rootItem.Data.XIndex = (double)rootItem.GetValue(Canvas.LeftProperty);
            double h1 = 0;
            for (var i = 0; i < rootSubItems.Count; i++)
            {
                if (i != 0) h1 += rootSubItems[i - 1].ActualHeight;
                //计算之前的所有子节点个数

                var list = new List<DesignerItem>();
                for (var j = 0; j < i; j++)
                {
                    GetAllSubItems(rootSubItems.ElementAt(j), list);
                }
                //var preChildCount = list.OrderBy(x => x.Data.YIndex).Count(x => x.Visibility.Equals(Visibility.Visible));

                var preChild = list.OrderBy(x => x.Data.YIndex).Where(x => x.Visibility.Equals(Visibility.Visible)).ToList();

                double h2 = 0;
                foreach (var designerItem in preChild)
                {
                    h2 += designerItem.ActualHeight;
                }

                //设置top
                //var top = rootItem.Data.YIndex + (preChildCount + i + 1) * CHILD_TOP_OFFSET;
                var top = rootItem.Data.YIndex + rootItem.ActualHeight + h1 + h2;
                var left = rootItem.Data.XIndex + GetOffset(rootItem);
                Canvas.SetTop(rootSubItems.ElementAt(i), top);
                rootSubItems.ElementAt(i).Data.YIndex = top;
                rootSubItems.ElementAt(i).Oldy = top;

                //设置left
                Canvas.SetLeft(rootSubItems.ElementAt(i), left);
                rootSubItems.ElementAt(i).Data.XIndex = left;
                rootSubItems.ElementAt(i).Oldy = left;
                SetItemFontColor(rootSubItems.ElementAt(i), DEFAULT_FONT_COLOR_BRUSH);
                SetWidth(rootSubItems.ElementAt(i));
                ArrangeWithRootItems(rootSubItems.ElementAt(i));
            }
        }

        public void SetWidth(DesignerItem designerItem)
        {
            designerItem.Width = GetWidth(designerItem);
        }

        double GetWidth(DesignerItem designerItem)
        {
            if (designerItem.Data != null && designerItem.Data.Text != null)
            {
                string text = designerItem.Data.Text;
                FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Arial"), FONT_SIZE, Brushes.Black);
                double width = formattedText.Width + 12;
                return width < MIN_ITEM_WIDTH ? MIN_ITEM_WIDTH : width;
            }
            else
            {
                return MIN_ITEM_WIDTH;
            }
        }
        //double GetWidth(string text)
        //{
        //    if (text != null)
        //    {
        //        FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
        //            FlowDirection.LeftToRight, new Typeface("Arial"), FONT_SIZE, Brushes.Black);
        //        double width = formattedText.Width + 12;
        //        return width < MIN_ITEM_WIDTH ? MIN_ITEM_WIDTH : width;
        //    }
        //    else
        //    {
        //        return MIN_ITEM_WIDTH;
        //    }
        //}

        //public void SetWidth(string text)
        //{
        //    var selectedItems = GetSelectedItems();
        //    if (selectedItems != null && selectedItems.Count() == 1)
        //    {
        //        var selectedItem = selectedItems.FirstOrDefault();
        //        if (selectedItem != null)
        //        {
        //            var width = GetWidth(text);
        //            selectedItem.Width = width;
        //            ArrangeWithRootItems();
        //        }
        //    }
        //}

        private double GetOffset(FrameworkElement item)
        {
            return item.Width.Equals(0) ? 30 : (item.Width * 0.1 + LEFT_OFFSET);
        }
        #endregion

        #region Style

        #region Highlight

        public DesignerItem HighlightParent/*拖动时高亮父节点*/(DesignerItem designerItem)
        {
            List<DesignerItem> subItems = new List<DesignerItem>();
            GetAllSubItems(designerItem, subItems);

            var selectedItems = GetSelectedItems();

            foreach (var item in _diagramControl.DesignerItems)
            {
                if (!subItems.Contains(item) && !selectedItems.Contains(item))
                    SetItemBorderStyle(item, DEFAULT_BORDER_BRUSH, new Thickness(DEFAULT_BORDER_THICKNESS),
                        DEFAULT_BACKGROUND_BRUSH);
                else
                {
                    SetItemBorderStyle(item, MOVE_ITEM_BORDER_THICKNESS, new Thickness(DEFAULT_BORDER_THICKNESS), SHADOW_BACKGROUND_BRUSH);
                    SetItemFontColor(item, SHADOW_FONT_COLOR_BRUSH);
                }
            }
            var parent = GetNewParentdDesignerItem(designerItem);
            if (parent != null)
            {
                SetItemBorderStyle(parent, HIGHLIGHT_BORDER_BRUSH, new Thickness(HIGHLIGHT_BORDER_THICKNESS),
                    DEFAULT_BACKGROUND_BRUSH);
                //SetBottomItemPosition(parent);
            }
            return parent;
        }

        //public void HighlightSelected/*高亮选中*/()
        //{
        //    var selectedItems = GetSelectedItems();
        //    foreach (var selectedItem in selectedItems)
        //    {
        //        SetItemBorderStyle(selectedItem, SELECTED_BORDER_BRUSH, new Thickness(HIGHLIGHT_BORDER_THICKNESS), HIGHLIGHT_BACKGROUND_BRUSH);
        //        SetItemFontColor(selectedItem, DEFAULT_FONT_COLOR_BRUSH);
        //    }

        //}

        #endregion

        #region Get Visual Item
        private Border GetBorder/*元素边框控件*/(DesignerItem item)
        {
            return item.Content as Border;
        }
        private TextBlock GetTextBlock/*元素文字控件*/(DesignerItem item)
        {
            var border = GetBorder(item);
            if (border == null) return null;
            return border.Child as TextBlock;
        }
        private string GetItemText/*取得元素文字内容*/(DesignerItem item)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return string.Empty;
            return textBlock.GetValue(TextBlock.TextProperty).ToString();
        }
        #endregion

        #region Set Visual Item

        public void SetItemText/*设定元素文字*/(DesignerItem item, string text)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return;
            textBlock.SetValue(TextBlock.TextProperty, text);
        }
        private void SetItemFontColor/*设定元素文字颜色*/(DesignerItem item, SolidColorBrush fontColorBrush)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return;
            textBlock.SetValue(TextBlock.ForegroundProperty, fontColorBrush);
        }
        private void SetItemBorderStyle/*设定边框样式*/(DesignerItem item, SolidColorBrush borderColor, Thickness borderThickness, SolidColorBrush backgroundbrBrush)
        {
            var border = GetBorder(item);
            if (border == null) return;
            border.BorderBrush = borderColor;
            border.BorderThickness = borderThickness;
            border.Background = backgroundbrBrush;
        }

        #endregion

        #region Reset Style

        //public void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer)
        //{
        //    foreach (var item in _diagramControl.DesignerItems.Where(item => !item.IsShadow))
        //    {
        //        SetItemBorderStyle(item, DEFAULT_BORDER_BRUSH, new Thickness(DEFAULT_BORDER_THICKNESS), DEFAULT_BACKGROUND_BRUSH);
        //        SetItemFontColor(item, DEFAULT_FONT_COLOR_BRUSH);
        //    }
        //}
        //public void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer, DesignerItem designerItem)
        //{
        //    foreach (var item in _diagramControl.DesignerItems.Where(item => !item.IsShadow && item != designerItem))
        //    {
        //        SetItemBorderStyle(item, DEFAULT_BORDER_BRUSH, new Thickness(DEFAULT_BORDER_THICKNESS), DEFAULT_BACKGROUND_BRUSH);
        //        SetItemFontColor(item, DEFAULT_FONT_COLOR_BRUSH);
        //    }
        //}

        #endregion

        #endregion

        #region Expand & Collapse

        public void ExpandAll/*展开所有*/()
        {
            var items = _diagramControl.DesignerItems;
            foreach (var item in items)
            {
                item.IsExpanded = true;
            }
        }
        public void CollapseAll/*折叠所有，除了根节点*/()
        {
            var items = _diagramControl.DesignerItems;
            foreach (var item in items.Where(item => item.CanCollapsed))
            {
                item.IsExpanded = false;
            }
        }
        public void HideOrExpandChildItems/*展开折叠*/(DesignerItem item)
        {
            if (item.IsExpanded == false)/*hide*/
            {
                var childs = new List<DesignerItem>();
                GetAllSubItems(item, childs);
                if (childs.Count == 0) return;
                foreach (var designerItem in childs)
                {
                    var connections = GetItemConnections(designerItem);
                    foreach (var connection in connections)
                    {
                        connection.Visibility = Visibility.Collapsed;
                    }
                    designerItem.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                var childs = new List<DesignerItem>();
                GetAllSubItems(item, childs);
                foreach (var designerItem in childs.Where(x => GetParentItem(x).IsExpanded))
                {
                    var connections = GetItemConnections(designerItem).Where(x => x.Source.ParentDesignerItem.IsExpanded);
                    foreach (var connection in connections)
                    {
                        connection.Visibility = Visibility.Visible;
                    }
                    designerItem.Visibility = Visibility.Visible;
                }
            }
            ArrangeWithRootItems();
        }
        #endregion

        #region Drag

        //public DesignerItem ChangeParent(DesignerItem selectedItem)
        //{
        //    var selectedItems = GetSelectedItems();
        //    var designerItemsToMove = selectedItems.Where(x => selectedItems.All(y => y.ID != x.Data.ParentId)).ToList();
        //    var newParent = GetNewParentdDesignerItem(selectedItem);
        //    foreach (var designerItemToMove in designerItemsToMove)
        //    {

        //        var originalParent = _diagramControl.DesignerItems.Where(x => x.Data.Id == designerItemToMove.Data.ParentId).ToList();
        //        if (newParent != null)//有新父节点
        //        {
        //            if (originalParent.Count() != 0)//有父节点，有连线
        //            {
        //                ChangeNewParent(originalParent, newParent, designerItemToMove);
        //            }
        //            else //没有父节点，也没有连线
        //            {
        //                CreateNewConnection(newParent, designerItemToMove);
        //            }
        //            _diagramControl.DesignerItems.Where(x => x.IsNewParent).ToList().ForEach(x => x.IsNewParent = false);
        //            newParent.IsNewParent = true;
        //        }
        //        else //没有新父节点，成为独立节点
        //        {
        //            RemoveConnection(selectedItem, designerItemToMove);//原先有父节点，则移除与原父节点之间的连线
        //            _diagramControl.DesignerItems.Where(x => x.IsNewParent).ToList().ForEach(x => x.IsNewParent = false);
        //        }
        //        //ResetBrushBorderFontStyle(_diagramControl.Designer, designerItemToMove);/*恢复边框字体样式*/
        //        designerItemToMove.Data.XIndex = Canvas.GetLeft(designerItemToMove);
        //        designerItemToMove.Data.YIndex = Canvas.GetTop(designerItemToMove);
        //    }
        //    return newParent;
        //}
        //void RemoveConnection(DesignerItem parent, DesignerItem designerItemToMove)
        //{
        //    designerItemToMove.Data.ParentId = Guid.Empty;
        //    if (parent == null) return;

        //    var connections = GetItemConnections(designerItemToMove)
        //        .Where(x => Equals(x.Source, GetItemConnector(parent, PARENT_CONNECTOR)))
        //        .ToList();
        //    foreach (var connection in connections)
        //    {
        //        var s = GetItemConnector(parent, PARENT_CONNECTOR);
        //        s.Connections.Remove(connection);
        //        connection.Source.Connections.Remove(connection);
        //        connection.Sink.Connections.Remove(connection);
        //        _diagramControl.DesignerCanvas.Children.Remove(connection);
        //    }

        //}

        //void ChangeNewParent(List<DesignerItem> originalParent, DesignerItem newParent, DesignerItem designerItemToMove)
        //{
        //    designerItemToMove.Data.ParentId = newParent.ID;
        //    var oldConnections = GetItemConnections(designerItemToMove).ToList();
        //    foreach (var designerItem in originalParent)
        //    {
        //        if (designerItem == null) continue;
        //        var oldConnector = GetItemConnector(designerItem, PARENT_CONNECTOR);
        //        if (oldConnector == null) continue;
        //        oldConnections.Where(x => Equals(x.Source, oldConnector)).ToList().ForEach(connection =>
        //        {
        //            connection.Source = GetItemConnector(newParent, PARENT_CONNECTOR);
        //        });
        //    }
        //}
        //void CreateNewConnection(DesignerItem parentDesignerItem, DesignerItem childDesignerItem)
        //{
        //    childDesignerItem.Data.ParentId = parentDesignerItem.ID;
        //    var source = GetItemConnector(parentDesignerItem, PARENT_CONNECTOR);
        //    var sink = GetItemConnector(childDesignerItem, "Left");
        //    var connection = new Connection(source, sink);
        //    _diagramControl.DesignerCanvas.Children.Add(connection);
        //}
        private void BringToFront/*将制定元素移到最前面*/(DesignerItem designerItem)
        {

            var canvas = designerItem.Parent as Canvas;
            if (canvas == null) return;

            List<UIElement> childrenSorted =
                (from UIElement item in canvas.Children
                 orderby Canvas.GetZIndex(item as UIElement) ascending
                 select item as UIElement).ToList();

            int i = 0;
            int j = 0;
            foreach (UIElement item in childrenSorted)
            {
                if (designerItem.Equals(item))
                {
                    int idx = Canvas.GetZIndex(item);
                    Canvas.SetZIndex(item, childrenSorted.Count - 1 + j++);
                }
                else
                {
                    Canvas.SetZIndex(item, i++);
                }
            }
        }
        private void BringToFront/*将制定元素移到最前面*/(UIElement element)
        {
            List<UIElement> childrenSorted =
                (from UIElement item in _diagramControl.DesignerCanvas.Children
                 orderby Panel.GetZIndex(item as UIElement) ascending
                 select item as UIElement).ToList();

            int i = 0;
            int j = 0;
            foreach (UIElement item in childrenSorted)
            {
                if (element.Equals(item))
                {
                    int idx = Panel.GetZIndex(item);
                    Panel.SetZIndex(item, childrenSorted.Count - 1 + j++);
                }
                else
                {
                    Panel.SetZIndex(item, i++);
                }
            }
        }
        public DesignerItem GetNewParentdDesignerItem/*取得元素上方最接近的元素*/(DesignerItem selectedItem)
        {
            var selectedItems = GetSelectedItems();
            //取得所有子节点，让parent不能为子节点
            var subitems = new List<DesignerItem>();
            foreach (var designerItem in selectedItems)
            {
                GetAllSubItems(designerItem, subitems);
            }
            subitems.AddRange(selectedItems);
            var pre = _diagramControl.DesignerItems.Where(x => x.Visibility.Equals(Visibility.Visible));
            var list = (from designerItem in pre
                        let parentTop = Canvas.GetTop(designerItem) + designerItem.ActualHeight - 13
                        let parentLeft = Canvas.GetLeft(designerItem) + designerItem.ActualWidth * 0.1
                        let parentRight = parentLeft + designerItem.ActualWidth
                        where Canvas.GetTop(selectedItem) >= parentTop /*top位置小于自己的top位置*/
                              && Canvas.GetLeft(selectedItem) >= parentLeft
                              && Canvas.GetLeft(selectedItem) <= parentRight
                              && !Equals(designerItem, selectedItem) /*让parent不能为自己*/
                              && !subitems.Contains(designerItem) /*让parent不能为子节点*/
                              && designerItem.IsShadow == false
                        select designerItem).ToList();
            if (!list.Any()) return null;
            var parent = list.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);
            return parent;
        }
        //public void HideItemConnection/*拖动元素，隐藏元素连线*/(DesignerItem item, DesignerItem parent)
        //{
        //    var selectedItems = GetSelectedItems();

        //    foreach (var connection in selectedItems.SelectMany(GetItemConnections))
        //    {
        //        if (connection.Sink.ParentDesignerItem.Equals(item) && connection.Source.ParentDesignerItem.Equals(parent))
        //        {
        //            var path = connection.Template.FindName("PART_ConnectionPath", connection) as Path;
        //            if (path != null)
        //            {
        //                path.Stroke = Brushes.Red;
        //            }
        //            BringToFront(connection);
        //        }
        //        else
        //        {
        //            //connection.Visibility = Visibility.Hidden;
        //        }
        //    }

        //}
        public void ShowItemConnection/*元素所有连线恢复显示*/()
        {
            var selectedItems = GetSelectedItems();
            foreach (var item in selectedItems)
            {
                foreach (var connection in GetItemConnections(item))
                {
                    //如果连线以此元素为起点，且此元素设置为【展开】，则显示连线
                    if (connection.Source.ParentDesignerItem.Equals(item) && item.IsExpanded)
                        connection.Visibility = Visibility.Visible;
                    //此元素有父节点，则处理与父节点之间的连线
                    var parent = GetParentItem(item);
                    if (parent != null)
                    {
                        //如果父元素被设置为【折叠】，则不处理。
                        if (!parent.IsExpanded) continue;
                        //如果连线以父元素为起点，则需要显示连线
                        if (connection.Source.ParentDesignerItem.Equals(parent))
                        {
                            connection.Visibility = Visibility.Visible;
                        }
                    }

                    var path = connection.Template.FindName("PART_ConnectionPath", connection) as Path;
                    if (path != null)
                    {
                        path.Stroke = Brushes.SkyBlue;
                        Panel.SetZIndex(connection, -1000);
                    }
                }
            }
        }
        public DesignerItem ChangeItemParent(DesignerItem designerItem, List<DesignerItem> shadows)
        {
            HideOthers(designerItem);
            var newParent = GetNewParentdDesignerItem(designerItem);
            if (shadows == null)
                shadows = CreateShadows(designerItem, designerItem, newParent);
            ConnectToParent(newParent, designerItem);
            MoveUpAndDown(newParent, designerItem);
            return newParent;
        }
        public List<DesignerItem> CreateShadows/*拖拽时产生影子*/(DesignerItem designerItem, DesignerItem dragItem/*拖动的节点*/, DesignerItem newParent)
        {
            var selectedItems = GetSelectedItems();
            var shadows = new List<DesignerItem>();
            foreach (var selectedItem in selectedItems)
            {
                var shadow = CreateShadow(selectedItem);
                //_diagramControl.DesignerCanvas.Children.Add(shadow);
                shadows.Add(shadow);
            }
            BringToFront(designerItem);

            return shadows;
        }
        /*
         * 链接新父节点及item
         */
        void ConnectToParent(DesignerItem newParent, DesignerItem dragItem)
        {
            if (newParent != null)
            {
                RemoveParentLine();
                CreateConnection(newParent, dragItem, Brushes.Red);
            }
            else
            {
                RemoveParentLine();
            }
        }
        void CreateConnection(DesignerItem newParent, DesignerItem dragItem, Brush color = null)
        {
            var source = GetItemConnector(newParent, PARENT_CONNECTOR);
            var sink = GetItemConnector(dragItem, CHILD_CONNECTOR);
            var connection = new Connection(source, sink);
            connection.toNewParent = true;
            source.Connections.Add(connection);
            _diagramControl.DesignerCanvas.Children.Add(connection);
            Arrange();
            if (color != null) SetConnectionColor(connection, color);
            BringToFront(connection);
        }
        private void RemoveParentLine()
        {
            var connections = GetConnections().Where(x => x.toNewParent).ToList();
            if (connections.Any())//如果有连线，则将连线链接到parent
            {
                connections.ForEach(RemoveConnection);
            }
        }
        void SetConnectionColor(Connection connection, Brush colorBrushes)
        {
            var path = connection.Template.FindName("PART_ConnectionPath", connection) as Path;
            if (path != null)
            {
                path.Stroke = colorBrushes;
            }
        }
        private DesignerItem CreateShadow/*拖动时创建的影子*/(DesignerItem item)
        {
            var shadow = new DesignerItem(_diagramControl) { IsShadow = true };
            shadow.ShadowOrignal = item;
            CreateDesignerItemContent(shadow, SHADOW_FONT_COLOR_BRUSH);
            //SetItemText(shadow, GetItemText(item));
            //SetItemBorderStyle(shadow, SHADOW_BORDER_BRUSH, new Thickness(DEFAULT_BORDER_THICKNESS), SHADOW_BACKGROUND_BRUSH);
            shadow.DataContext = item;

            Canvas.SetLeft(shadow, item.Oldx);
            Canvas.SetTop(shadow, item.Oldy);
            shadow.Oldx = item.Oldx;
            shadow.Oldy = item.Oldy;
            shadow.Data.Text = item.Data.Text;
            shadow.Data.XIndex = item.Oldx;
            shadow.Data.YIndex = item.Oldy;
            shadow.Width = item.Width;
            Panel.SetZIndex(shadow, -100);

            _diagramControl.DesignerCanvas.Children.Add(shadow);
            Arrange();

            /*
             * 拖动时，创建shadow
             * 将原先链接到item上的连线，链接到shadow上
             */

            shadow.Data.ParentId = item.Data.ParentId;
            foreach (var directSubItem in GetDirectSubItems(item))
            {
                directSubItem.Data.ParentId = shadow.ID;
            }
            var connections = GetItemConnections(item).ToList();
            foreach (var connection in connections)
            {
                if (Equals(connection.Source.ParentDesignerItem, item))
                {
                    connection.Source = GetItemConnector(shadow, PARENT_CONNECTOR);
                }
                else if (Equals(connection.Sink.ParentDesignerItem, item))
                {
                    connection.Sink = GetItemConnector(shadow, "Left");
                }
            }
            item.Data.ParentId = Guid.Empty;
            return shadow;
        }
        /*
         * 移除红色连线，item-newparent
         * 将shadows的连线恢复链接到items上
         * 将items链接到parent上
         * 移除shadows
         */
        public void FinishChangeParent(DesignerItem newParent)
        {
            ShowOthers();

            RemoveParentLine();
            var shadows = GetDesignerItems().Where(x => x.IsShadow).ToList();
            foreach (var shadow in shadows)
            {
                if (shadow.Data.ParentId == Guid.Empty)
                {
                    if (newParent != null) CreateConnection(newParent, shadow.ShadowOrignal);
                }

                var item = shadow.ShadowOrignal;
                item.Data.ParentId = newParent == null ? Guid.Empty : newParent.Data.Id;
                foreach (var directSubItem in GetDirectSubItems(shadow))
                {
                    directSubItem.Data.ParentId = item.ID;
                }
                var connections = GetItemConnections(shadow).ToList();
                foreach (var connection in connections)
                {
                    //以shadow为起点
                    if (Equals(connection.Source.ParentDesignerItem, shadow))
                    {
                        connection.Source = GetItemConnector(item, PARENT_CONNECTOR);
                    }
                    //以shadow为终点
                    else if (Equals(connection.Sink.ParentDesignerItem, shadow))
                    {
                        if (newParent != null)
                        {
                            connection.Sink = GetItemConnector(item, CHILD_CONNECTOR);
                            connection.Source = GetItemConnector(newParent, PARENT_CONNECTOR);
                        }
                        else
                        {
                            RemoveConnection(connection);
                        }
                    }
                }
            }
            _diagramControl.DesignerItems.Where(x => x.IsNewParent).ToList().ForEach(x => x.IsNewParent = false);
            RemoveShadows();
            ArrangeWithRootItems();/*重新布局*/
            ShowItemConnection();
        }
        void RemoveConnection(Connection connection)
        {
            connection.Source.Connections.Remove(connection);
            connection.Sink.Connections.Remove(connection);
            connection.Source = null;
            connection.Sink = null;
            _diagramControl.DesignerCanvas.Children.Remove(connection);
        }
        public void RemoveShadows()
        {
            foreach (var shadow in GetDesignerItems().Where(x => x.IsShadow))
            {
                _diagramControl.DesignerCanvas.Children.Remove(shadow);
            }
        }
        public void HideOthers(DesignerItem selectedItem)
        {
            var selectedItems = GetSelectedItems();
            selectedItem.IsExpanderVisible = false;
            foreach (var designerItem in selectedItems.Where(x => x.ID != selectedItem.ID))
            {
                designerItem.Visibility = Visibility.Hidden;
            }
        }
        public void ShowOthers()
        {
            var selectedItems = _diagramControl.DesignerItems;//GetSelectedItems();
            foreach (var selectedItem in selectedItems)
            {
                selectedItem.Visibility = Visibility.Visible;
                if (_diagramControl.DesignerItems.Any(x => x.Data.ParentId == selectedItem.Data.Id))
                {
                    selectedItem.IsExpanderVisible = selectedItem.CanCollapsed;
                }
            }
        }
        List<Connection> GetConnections()
        {
            List<Connection> list = new List<Connection>();
            var itemCount = VisualTreeHelper.GetChildrenCount(_diagramControl.DesignerCanvas);
            if (itemCount == 0) return list;
            for (int n = 0; n < itemCount; n++)
            {
                var c = VisualTreeHelper.GetChild(_diagramControl.DesignerCanvas, n);
                var child = c as Connection;
                if (child != null) list.Add(child);
            }
            return list;
        }
        public List<DesignerItem> GetDesignerItems/*取得画布所有元素*/()
        {
            var list = new List<DesignerItem>();

            var itemCount = VisualTreeHelper.GetChildrenCount(_diagramControl.DesignerCanvas);
            if (itemCount == 0) return list;
            for (int n = 0; n < itemCount; n++)
            {
                var c = VisualTreeHelper.GetChild(_diagramControl.DesignerCanvas, n);
                var child = c as DesignerItem;
                if (child != null) list.Add(child);
            }
            return list;
        }
        public void MoveUpAndDown(DesignerItem parent, DesignerItem selectedItem)
        {
            if (parent == null) return;
            var itemTop = Canvas.GetTop(selectedItem);
            var itemsOnCanvas = _diagramControl.DesignerCanvas.Children;
            var designerItemsOnCanvas = itemsOnCanvas.OfType<DesignerItem>().ToList();
            var allSubItems = new List<DesignerItem>();
            GetAllSubItems(selectedItem, allSubItems);
            allSubItems.AddRange(designerItemsOnCanvas.Where(x => x.IsShadow));
            var downItems = designerItemsOnCanvas.Where(x =>
                x.Oldy > itemTop
                && x.ID != selectedItem.ID);
            foreach (var designerItem in downItems)
            {
                if (!allSubItems.Contains(designerItem))
                {
                    Canvas.SetTop(designerItem, designerItem.Oldy + CHILD_TOP_OFFSET);
                }
                else
                {
                    allSubItems.ForEach(x =>
                    {
                        Canvas.SetTop(x, x.Oldy + CHILD_TOP_OFFSET);
                    });
                }
            }
            var upItems = designerItemsOnCanvas.Where(x =>
                x.Oldy < itemTop
                && x.Oldy > Canvas.GetTop(parent)
                && x.ID != selectedItem.ID);
            foreach (var designerItem in upItems)
            {
                if (!allSubItems.Contains(designerItem))
                {
                    Canvas.SetTop(designerItem, designerItem.Data.YIndex);
                }
                else
                {
                    allSubItems.ForEach(x =>
                    {
                        Canvas.SetTop(x, x.Data.YIndex);
                    });
                }
            }
        }

        #endregion

        #region Add,Edit,Delete,Copy
        public void AddDesignerItem(ItemDataBase item)
        {
            var parentDesignerItem = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == item.ParentId);
            var designerItem = new DesignerItem(item.Id, item.ParentId, item, _diagramControl);
            _diagramControl.DesignerItems.Add(designerItem);
            CreateChild(parentDesignerItem, designerItem);
            SetSelectItem(designerItem);
            Scroll(designerItem);
            BringToFront(designerItem);
            ArrangeWithRootItems();
        }
        public void Edit(DesignerItem item)
        {
            _diagramControl.IsOnEditing = true;
            var textBox = new TextBox();
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.MinHeight = 24;
            textBox.DataContext = item.Data;
            textBox.SetBinding(TextBox.TextProperty, new Binding("Text") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });

            Canvas.SetLeft(textBox, Canvas.GetLeft(item));
            Canvas.SetTop(textBox, Canvas.GetTop(item));

            _diagramControl.DesignerCanvas.Children.Remove(textBox);
            _diagramControl.DesignerCanvas.Children.Add(textBox);
            BringToFront(textBox);
            textBox.SelectAll();
            textBox.Focus();

            var t = textBox;
            EditorKeyBindings(t);

            t.MinWidth = MIN_ITEM_WIDTH;
            t.LostFocus += (sender, e) =>
            {
                _diagramControl.DesignerCanvas.Children.Remove(textBox);
                ArrangeWithRootItems();
                _diagramControl.IsOnEditing = false;
                GlobalInputBindingManager.Default.Recover();
            };
            t.TextChanged += (sender, e) => { t.Width = GetWidth(item); };
        }
        void EditorKeyBindings(TextBox t)
        {
            GlobalInputBindingManager.Default.Clear();

            KeyBinding kbCtrlEnter = new KeyBinding();
            kbCtrlEnter.Key = Key.Enter;
            kbCtrlEnter.Modifiers = ModifierKeys.Control;
            kbCtrlEnter.Command = new RelayCommand(() =>
            {
                t.Text += Environment.NewLine;
                t.SelectionStart = t.Text.Length;
            });
            t.InputBindings.Add(kbCtrlEnter);

            KeyBinding kbEnter = new KeyBinding();
            kbEnter.Key = Key.Enter;
            kbEnter.Command = new RelayCommand(() =>
            {
                if (_diagramControl.IsOnEditing)
                {
                    _diagramControl.DesignerCanvas.Children.Remove(t);
                    ArrangeWithRootItems();
                    _diagramControl.IsOnEditing = false;
                }
            });
            t.InputBindings.Add(kbEnter);

            KeyBinding kbDelete = new KeyBinding();
            kbDelete.Key = Key.Delete;
            kbDelete.Command = new RelayCommand(() =>
            {
                if (_diagramControl.IsOnEditing)
                {
                    DeleteText(t);
                }
            });
            t.InputBindings.Add(kbDelete);
        }
        void DeleteText(TextBox t)
        {
            if (_diagramControl.IsOnEditing)
            {
                var text = t.Text;
                var x = t.SelectedText;
                if (!string.IsNullOrEmpty(text))
                {
                    if (!string.IsNullOrEmpty(x))
                    {
                        t.Text = System.Text.RegularExpressions.Regex.Replace(text, x, "");
                    }
                    else
                    {
                        var index = t.SelectionStart;
                        if (index != text.Length)
                        {
                            t.Text = System.Text.RegularExpressions.Regex.Replace(text, text[index].ToString(), "");
                            t.SelectionStart = index;
                        }
                    }
                }


            }
        }
        #region Delete
        public void DeleteDesignerItem(ItemDataBase itemDataBase)
        {
            var child = GetAllChildItemDataBase(itemDataBase.Id);
            _diagramControl.RemovedItemDataBase.AddRange(child);
            _diagramControl.RemovedItemDataBase.Add(itemDataBase);
            _diagramControl.RemovedItemDataBase.ForEach(x => { x.Removed = true; });
            child.ForEach(c =>
            {
                DeleteItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ID == c.Id));
            });
            DeleteItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ID == itemDataBase.Id));

            ArrangeWithRootItems();

            var c1 = _diagramControl.DesignerItems.Where(x => x.Data.ParentId == itemDataBase.ParentId).ToList();
            DesignerItem selectedDesignerItem = null;
            selectedDesignerItem = c1.Any() ?
                c1.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b) :
                _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == itemDataBase.ParentId);
            _diagramControl.DiagramManager.SetSelectItem(selectedDesignerItem);
            ArrangeWithRootItems();
            Scroll(selectedDesignerItem);
        }
        List<ItemDataBase> GetAllChildItemDataBase(Guid id)
        {
            List<ItemDataBase> result = new List<ItemDataBase>();
            var child = _diagramControl.ItemDatas.Where(x => x.ParentId == id);
            foreach (var itemDataBase in child)
            {
                result.Add(itemDataBase);
                result.AddRange(GetAllChildItemDataBase(itemDataBase.Id));
            }
            return result;
        }
        void DeleteItem(DesignerItem item)
        {
            var connections = GetItemConnections(item).ToList();
            connections.ForEach(x => { _diagramControl.DesignerCanvas.Children.Remove(x); });
            var connectors = GetItemConnectors(item);
            connectors.ForEach(x => { x.Connections.Clear(); });
            _diagramControl.DesignerCanvas.Children.Remove(item);
            _diagramControl.DesignerItems.Remove(item);
        }
        #endregion
        #endregion

        #region 用数据源，构建节点元素

        private List<DesignerItem> InitDesignerItems()
        {
            if (_diagramControl.ItemDatas == null) return null;
            var roots = _diagramControl.ItemDatas.Where(x => x.ParentId == Guid.Empty).ToList();
            if (!roots.Any()) return null;
            List<DesignerItem> rootDesignerItems = new List<DesignerItem>();
            _diagramControl.DesignerItems.Clear();
            foreach (var root in roots)
            {
                var rootDesignerItem = CreateRootItem(root);
                rootDesignerItems.Add(rootDesignerItem);

                _diagramControl.DesignerItems.Add(rootDesignerItem);
                CreateChildDesignerItem(rootDesignerItem);
            }
            return rootDesignerItems;
        }
        private void CreateChildDesignerItem(DesignerItem parentDesignerItem)
        {
            var child = _diagramControl.ItemDatas.Where(x => x.ParentId == parentDesignerItem.ID && !x.Removed);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = CreateChildItem(parentDesignerItem.ID, userDataSource);
                _diagramControl.DesignerItems.Add(childDesignerItem);
                CreateChildDesignerItem(childDesignerItem);
            }
        }
        private DesignerItem CreateRootItem(ItemDataBase itemData)
        { return new DesignerItem(itemData.Id, itemData, _diagramControl); }
        private DesignerItem CreateChildItem(Guid parentId, ItemDataBase itemData)
        { return new DesignerItem(itemData.Id, parentId, itemData, _diagramControl); }

        #endregion

        public void SelectUpDown(DesignerItem selectedItem, bool selectUp)
        {
            if (selectedItem == null) return;
            var siblingDesignerItems = _diagramControl.DesignerItems.Where(x => x.Data.ParentId == selectedItem.Data.ParentId).ToList();
            DesignerItem selectedDesignerItem = null;

            if (selectUp)
            {
                var top = siblingDesignerItems
                    .Where(x => x.Data.YIndex < selectedItem.Data.YIndex).ToList();
                if (top.Any())
                {
                    selectedDesignerItem =
                        top.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);
                }
                else
                {
                    var parent =
                        _diagramControl.DesignerItems.Where(x => x.Data.Id == selectedItem.Data.ParentId).ToList();
                    if (parent.Count() == 1)
                        selectedDesignerItem = parent.FirstOrDefault();
                }
            }
            else //move down
            {
                var down = siblingDesignerItems
                    .Where(x => x.Data.YIndex > selectedItem.Data.YIndex).ToList();
                if (down.Any()) //1.优先找相邻节点，处于下方的节点
                {
                    selectedDesignerItem = down.Aggregate((a, b) => a.Data.YIndex < b.Data.YIndex ? a : b);
                }
                else //没有处于下方的相邻节点，2.有父亲节点，则找其父亲的，处于下方的相邻节点，3.如果没有父亲节点，就找子节点
                {
                    var parents =
                        _diagramControl.DesignerItems.Where(x => x.Data.Id == selectedItem.Data.ParentId).ToList();
                    if (parents.Count() == 1 && parents.FirstOrDefault() != null) //有父节点，父节点邻居，处于下方的节点
                    {
                        var parent = parents.FirstOrDefault();
                        if (parent != null)
                        {
                            var parentSibling =
                                _diagramControl.DesignerItems.Where(
                                    x => x.Data.ParentId == parent.Data.ParentId
                                         && x.Data.YIndex > parent.Data.YIndex).ToList();
                            if (parentSibling.Any())
                            {
                                selectedDesignerItem =
                                    parentSibling.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);
                            }
                        }
                    }
                    if (selectedDesignerItem == null)//没有父节点，找子节点
                    {
                        var child = _diagramControl.DesignerItems.Where(x => x.Data.ParentId == selectedItem.ID).ToList();
                        if (child.Any())
                        {
                            selectedDesignerItem = child.Aggregate((a, b) => a.Data.YIndex < b.Data.YIndex ? a : b);
                        }
                    }
                }
            }
            if (selectedDesignerItem != null) _diagramControl.DiagramManager.SetSelectItem(selectedDesignerItem);
            Scroll(selectedDesignerItem);
        }
        public void SelectRightLeft(DesignerItem selectedItem, bool selectRight)
        {
            DesignerItem selectedDesignerItem = null;
            if (selectedItem != null)
            {
                if (selectRight)
                {
                    var child = _diagramControl.DesignerItems.Where(x => x.Data.ParentId == selectedItem.ID).ToList();
                    if (child.Any())
                    {
                        selectedDesignerItem = child.Aggregate((a, b) => a.Data.YIndex < b.Data.YIndex ? a : b);
                    }
                }
                else
                {
                    var parent = _diagramControl.DesignerItems.Where(x => x.ID == selectedItem.Data.ParentId).ToList();
                    if (parent.Any())
                    {
                        selectedDesignerItem = parent.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);
                    }
                }
            }
            if (selectedDesignerItem != null) _diagramControl.DiagramManager.SetSelectItem(selectedDesignerItem);
        }
        List<DesignerItem> GetSelectedItems()
        {
            var selectedItems =
                _diagramControl.DesignerCanvas.SelectionService.CurrentSelection.ConvertAll(x => x as DesignerItem);
            //selectedItems.ForEach(BringToFront);
            return selectedItems;
        }
        void Scroll(DesignerItem designerItem)
        {
            if (designerItem == null) return;
            var sv = (ScrollViewer)_diagramControl.Template.FindName("DesignerScrollViewer", _diagramControl);
            sv.ScrollToVerticalOffset(Canvas.GetTop(designerItem));
            sv.ScrollToHorizontalOffset(Canvas.GetLeft(designerItem));
        }
    }
}