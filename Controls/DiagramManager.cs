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
        private const double LEFT_OFFSET = 20;
        private const double MIN_ITEM_WIDTH = 150d;
        private const double FONT_SIZE = 12d;
        private static readonly SolidColorBrush SHADOW_FONT_COLOR_BRUSH = Brushes.Gray;
        private static readonly SolidColorBrush DEFAULT_FONT_COLOR_BRUSH = Brushes.Black;
        private const string PARENT_CONNECTOR = "Bottom";
        private const string CHILD_CONNECTOR = "Left";
        #endregion

        #region Get

        Connector GetItemConnector/*根据名称，取得元素连接点*/(DesignerItem item, string name)
        {
            var itemConnectorDecorator = item.Template.FindName("PART_ConnectorDecorator", item) as Control;
            if (itemConnectorDecorator == null) return null;
            var itemConnector = itemConnectorDecorator.Template.FindName(name, itemConnectorDecorator) as Connector;
            return itemConnector;
        }
        List<Connector> GetItemConnectors/*取得所有连接点*/(DesignerItem designerItem)
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
        IEnumerable<Connection> GetItemConnections/*取得所有连线*/(DesignerItem designerItem)
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
        List<DesignerItem> GetDirectSubItems/*取得直接子节点*/(DesignerItem item)
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
        List<DesignerItem> GetAllSubItems/*取得直接及间接的子节点*/(DesignerItem item/*某个节点*/)
        {
            var result = new List<DesignerItem>();
            var child = new List<DesignerItem>();
            var list = _diagramControl.DesignerItems
                .Where(x => x.Data.ParentId == item.ID && !x.Data.Removed)
                .OrderBy(x => x.Data.YIndex).ToList();
            foreach (var subItem in list.Where(subItem => !result.Contains(subItem)))
            {
                child.Add(subItem);
                result.Add(subItem);
                foreach (var designerItem in child)
                {
                    result.AddRange(GetAllSubItems(designerItem));
                }
            }
            return result;
        }

        #endregion

        #region Set

        public void SetSelectItem(DesignerItem designerItem)
        {
            _diagramControl.DesignerCanvas.SelectionService.ClearSelection();
            _diagramControl.DesignerCanvas.SelectionService.SelectItem(designerItem);
        }

        #endregion

        void GenerateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush fontColorBrush)
        {
            if (item == null) return;
            if (item.Data != null && item.Data.Text == (GetItemText(item))) { return; }
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
            item.Content = textblock;
        }

        #region Draw
        public void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/()
        {
            var roots = InitDesignerItems();/*利用数据源ItemDatas创建DesignerItem*/
            if (roots == null) return;
            if (!roots.Any()) return;/*创建DesignerItems*/
            DrawItems();
            ArrangeWithRootItems();/*将DesignerItems放到画布上，并且创建连线*/
            SetSelectItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.Data.ParentId == Guid.Empty));
        }
        void DrawItems()
        {
            _diagramControl.DesignerCanvas.Children.Clear();
            if (_diagramControl.DesignerItems == null) return;
            if (!_diagramControl.DesignerItems.Any()) return;
            var roots = _diagramControl.DesignerItems.Where(x => x.Data.ParentId.Equals(Guid.Empty)).ToList();
            roots.ForEach(root =>
            {
                DrawDesignerItems(root);
            });
        }
        List<DesignerItem> DrawDesignerItems(DesignerItem parentItem)
        {
            var designerItems = new List<DesignerItem>();
            if (parentItem == null) return designerItems;
            if (designerItems.All(x => !x.ID.Equals(parentItem.ID))
                && parentItem.Data.ParentId.Equals(Guid.Empty))
            { DrawRoot(parentItem, parentItem.Data.YIndex, parentItem.Data.XIndex); }
            var childs = _diagramControl.DesignerItems.Where(x => x.Data.ParentId.Equals(parentItem.ID));
            foreach (var childItem in childs)
            {
                if (designerItems.All(x => !x.ID.Equals(childItem.ID))) { DrawChild(parentItem, childItem); }
                designerItems.AddRange(DrawDesignerItems(childItem));
            }
            return designerItems;
        }
        void DrawRoot/*创建根节点*/(DesignerItem item, double topOffset, double leftOffset)
        {
            DrawDesignerItem(item, topOffset, leftOffset);
            item.CanCollapsed = false;
            item.IsExpanderVisible = false;
        }
        void DrawChild /*创建非根节点时，同时创建与父节点之间的连线*/(DesignerItem parent, DesignerItem childItem)
        {
            if (parent == null) return;
            if (childItem.Data.Removed) return;
            DrawDesignerItem(childItem);/*创建子节点*/
            var source = GetItemConnector(parent, PARENT_CONNECTOR);
            var sink = GetItemConnector(childItem, "Left");
            if (source == null || sink == null) return;
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
            childItem.CanCollapsed = true;
        }
        void DrawDesignerItem/*创建元素*/(DesignerItem item, double topOffset = 0d, double leftOffset = 0d)
        {
            if (item.Data == null) return;
            GenerateDesignerItemContent(item, DEFAULT_FONT_COLOR_BRUSH);
            if (!item.Data.Removed)
            {
                _diagramControl.DesignerCanvas.Children.Add(item);
                Arrange();
                Canvas.SetTop(item, topOffset);
                Canvas.SetLeft(item, leftOffset);
            }
        }
        #endregion

        #region Arrange Items
        /* Updates the DesiredSize of a UIElement.
         * Parent elements call this method 
         * from their own MeasureCore implementations 
         * to form a recursive（递归） layout update.
         */
        public void Measure()
        {
            _diagramControl.DesignerCanvas.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        }
        /* Positions child elements and determines a size for a UIElement.
        * Parent elements call this method
        * from their own MeasureCore implementations 
        * to form a recursive（递归） layout update.
        */
        public void Arrange()
        {
            var sv = Application.Current.MainWindow;
            if (sv == null) return;
            _diagramControl.DesignerCanvas.Arrange(new Rect(0, 0, sv.ActualWidth, sv.ActualHeight));
        }
        public void ArrangeWithRootItems()
        {
            Measure();
            var items = _diagramControl.DesignerItems.ToList();
            var roots = items.Where(x => x.Data.ParentId.Equals(Guid.Empty));
            foreach (var root in roots)
            {
                //设定节点宽度
                SetWidth(root);
                //设定节点位置
                root.Data.YIndex = Canvas.GetTop(root);
                root.Oldy = Canvas.GetTop(root);
                root.Data.XIndex = Canvas.GetLeft(root);
                root.Oldx = Canvas.GetLeft(root);
                ArrangeWithRootItems(root);
            }
        }
        void ArrangeWithRootItems/*递归方法，给定根节点，重新布局*/(DesignerItem designerItem/*根节点*/)
        {
            if (designerItem == null) return;
            var directSubItems = GetDirectSubItems(designerItem);
            if (directSubItems == null || directSubItems.Count == 0) return;
            var subItems = directSubItems
                .Where(x => x.Visibility.Equals(Visibility.Visible)).
                OrderBy(x => x.Data.YIndex).ToList();
            double h1 = 0;/*父节点的直接子节点总高度*/
            for (var i = 0; i < subItems.Count; i++)
            {
                if (i != 0) h1 += subItems[i - 1].ActualHeight;
                var list = new List<DesignerItem>();
                for (var j = 0; j < i; j++) { list.AddRange(GetAllSubItems(subItems.ElementAt(j)).Where(item => !list.Contains(item))); }
                var preChilds = list.OrderBy(x => x.Data.YIndex).Where(x => x.Visibility.Equals(Visibility.Visible));
                var h2 = preChilds.Sum(preChild => preChild.ActualHeight);/*父节点的直接子节点的所有子节点的总高度*/
                #region 设定节点位置
                //上
                var top = designerItem.Data.YIndex + designerItem.ActualHeight + h1 + h2;
                Canvas.SetTop(subItems.ElementAt(i), top);
                subItems.ElementAt(i).Data.YIndex = top;
                subItems.ElementAt(i).Oldy = top;
                //左
                var left = designerItem.Data.XIndex + GetOffset(designerItem);
                Canvas.SetLeft(subItems.ElementAt(i), left);
                subItems.ElementAt(i).Data.XIndex = left;
                subItems.ElementAt(i).Oldx = left;
                #endregion
                //设定节点宽度
                SetWidth(subItems.ElementAt(i));
                ArrangeWithRootItems(subItems.ElementAt(i));/*递归*/
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
        private double GetOffset(FrameworkElement item)
        {
            return item.Width.Equals(0) ? 30 : (item.Width * 0.1 + LEFT_OFFSET);
        }
        #endregion

        #region Style

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
                childs = GetAllSubItems(item);
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
                childs = GetAllSubItems(item);
                var parents = _diagramControl.DesignerItems.Where(x => x.ID == item.Data.ParentId).ToList();
                foreach (var p in parents)
                {
                    var parent = p;
                    foreach (var designerItem in childs.Where(x => parent.IsExpanded))
                    {
                        var connections = GetItemConnections(designerItem).Where(x => x.Source.ParentDesignerItem.IsExpanded);
                        foreach (var connection in connections)
                        {
                            connection.Visibility = Visibility.Visible;
                        }
                        designerItem.Visibility = Visibility.Visible;
                    }
                }
            }
            ArrangeWithRootItems();
        }
        #endregion

        #region Drag

        #region Public DragThumb 中调用
        public DesignerItem ChangeParent(DesignerItem designerItem)
        {
            _diagramControl.DesignerItems.Where(x => !x.IsNewParent).ToList().ForEach(x => x.IsNewParent = false);
            HideOthers(designerItem);
            var newParent = GetNewParent(designerItem);
            return newParent;
        }
        public List<DesignerItem> CreateShadows/*拖拽时产生影子*/(DesignerItem dragItem/*拖动的节点*/, DesignerItem newParent)
        {
            var selectedItems = GetSelectedItems();
            var shadows = selectedItems.Select(CreateShadow).ToList();
            BringToFront(dragItem);
            return shadows;
        }
        public void CreateHelperConnection(DesignerItem newParent, DesignerItem dragItem)
        {
            RemoveHelperConnection();
            if (newParent == null) return;
            var source = GetItemConnector(newParent, PARENT_CONNECTOR);
            var sink = GetItemConnector(dragItem, CHILD_CONNECTOR);
            var connection = new Connection(source, sink);
            connection.toNewParent = true;
            source.Connections.Add(connection);
            _diagramControl.DesignerCanvas.Children.Add(connection);
            SetConnectionColor(connection, Brushes.Red);
            BringToFront(connection);
        }
        public void MoveUpAndDown(DesignerItem parent, DesignerItem selectedItem)
        {
            if (parent == null) return;
            _diagramControl.DesignerItems.Where(x=>!Equals(x, selectedItem)).ToList().ForEach(x =>
            {
                Canvas.SetTop(x,x.Data.YIndex);
                Canvas.SetLeft(x,x.Data.XIndex);
            });
            var itemTop = Canvas.GetTop(selectedItem) - selectedItem.ActualHeight/2;
            var itemsOnCanvas = _diagramControl.DesignerCanvas.Children;
            var designerItemsOnCanvas = itemsOnCanvas.OfType<DesignerItem>().ToList();
            var downItems = designerItemsOnCanvas.Where(x =>
                x.Oldy > itemTop
                && x.ID != selectedItem.ID
                && x.Data.ParentId != Guid.Empty
               ).ToList();
            foreach (var designerItem in downItems)
            {
                var item = designerItem.IsShadow ? designerItem.ShadowOrignal : designerItem;


                var p = GetTopestParentAfterSpecifiedParent(parent, item);
                if (p != null)
                {
                    if (Equals(p, selectedItem))
                    {
                        var shadow = designerItemsOnCanvas.FirstOrDefault(x => x.IsShadow && x.ShadowOrignal.Equals(p));
                        if (shadow != null) Canvas.SetTop(shadow, shadow.Oldy + selectedItem.ActualHeight);
                        var list = designerItemsOnCanvas.Where(x =>
                           x.Oldy > p.Oldy
                           && x.ID != selectedItem.ID
                           ).ToList();

                        list.ForEach(x =>
                        {
                            Canvas.SetTop(x, x.Oldy + selectedItem.ActualHeight);
                        });
                    }
                    else
                    {
                        Canvas.SetTop(p, p.Oldy + selectedItem.ActualHeight);
                        var list = designerItemsOnCanvas.Where(x =>
                            x.Oldy > p.Oldy
                            && x.ID != selectedItem.ID
                            ).ToList();

                        list.ForEach(x =>
                        {
                            Canvas.SetTop(x, x.Oldy + selectedItem.ActualHeight);
                        });
                    }
                }
                else
                {
                    Canvas.SetTop(designerItem, designerItem.Oldy + selectedItem.ActualHeight);
                    var list = designerItemsOnCanvas.Where(x =>
                        x.Oldy > designerItem.Oldy
                        && x.ID != selectedItem.ID
                        ).ToList();

                    list.ForEach(x =>
                    {
                        Canvas.SetTop(x, x.Oldy + selectedItem.ActualHeight);
                    });
                }
            }
            var upItems = designerItemsOnCanvas.Where(x =>
                x.Oldy < itemTop
                && x.Oldy > Canvas.GetTop(parent)
                && x.ID != selectedItem.ID
                 && x.Data.ParentId != Guid.Empty
                ).ToList();
            foreach (var designerItem in upItems)
            {
                Canvas.SetTop(designerItem, designerItem.Data.YIndex);
                var item = designerItem.IsShadow ? designerItem.ShadowOrignal : designerItem;
                var x1 = GetAllSubItems(item);
                if (x1 == null || !x1.Any()) continue;
                var list = designerItemsOnCanvas.Where(x =>
                    x.Oldy <= x1.Aggregate((a, b) => a.Oldy > b.Oldy ? a : b).Oldy
                    && x.ID != selectedItem.ID
                    ).ToList();
                list.ForEach(x => { Canvas.SetTop(x, x.Data.YIndex); });
            }
        }
        public void FinishChangeParent(DesignerItem newParent)
        {
            ShowOthers();/*恢复显示选中元素,之前调用了HideOthers隐藏了除了drag item以外的selected items*/
            RemoveHelperConnection();/*移除找parent的辅助红线*/
            ChangeShadowConnectionsToOriginalItem();/*将连接到shadow上的连线，恢复到item上*/
            _diagramControl.DesignerItems.ToList().ForEach(x => { x.IsNewParent = false; x.Data.YIndex = Canvas.GetTop(x); });
            ConnectToNewParent(newParent);/*根据取得的newParent,改变特定item的连线*/
            RemoveShadows();/*移除所有shadow*/
            ArrangeWithRootItems();/*重新布局*/
        }
        #endregion

        #region Item finder
        DesignerItem GetNewParent/*取得元素上方最接近的元素*/(DesignerItem selectedItem)
        {
            var selectedItems = GetSelectedItems();
            //取得所有子节点，让parent不能为子节点
            var subitems = new List<DesignerItem>();
            foreach (var designerItem in selectedItems)
            {
                subitems.AddRange(GetAllSubItems(designerItem));
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
            _diagramControl.DesignerItems.ToList().ForEach(x => { x.IsNewParent = false; });
            parent.IsNewParent = true;
            return parent;
        }
        DesignerItem GetTopestParentAfterSpecifiedParent/*取得parent之后的，item的最高根节点*/(DesignerItem parent, DesignerItem item)
        {
            DesignerItem result = null;
            var p = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == item.Data.ParentId && x.Data.ParentId != Guid.Empty);/*树状列表，节点只有一个根节点*/
            if (p != null && !p.Equals(parent))//无父节点，或者到达指定根节点，则返回上一个节点。
            {
                result = p;
                var x = GetTopestParentAfterSpecifiedParent(parent, p);
                if (x != null) result = x;
            }
            return result;
        }
        #endregion

        #region Connection operations
        void ChangeOriginalItemConnectionToShadow/*将item上的连线，连接到shadow上*/(DesignerItem item, DesignerItem shadow)
        {
            if (item == null || shadow == null) return;
            Measure();//shadow放置到canvas上之后，需要重新测量，才能取得connector
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
        }
        void ChangeShadowConnectionsToOriginalItem/*将连接到shadow上的连线，恢复到item上*/()
        {
            var shadows = GetDesignerItems().Where(x => x.IsShadow).ToList();
            foreach (var shadow in shadows)
            {
                var connections = GetItemConnections(shadow).ToList();
                connections.ForEach(connection =>
                {
                    if/*以shadow为起点*/(Equals(connection.Source.ParentDesignerItem, shadow))
                    {
                        connection.Source = GetItemConnector(shadow.ShadowOrignal, PARENT_CONNECTOR);
                    }
                    else if /*以shadow为终点*/(Equals(connection.Sink.ParentDesignerItem, shadow))
                    {
                        connection.Sink = GetItemConnector(shadow.ShadowOrignal, CHILD_CONNECTOR);
                    }
                });
            }
        }
        void ConnectToNewParent/*根据取得的newParent,改变特定item的连线*/(DesignerItem newParent)
        {
            var items = GetSelectedItems();
            var itemsToChangeParent = items.Where(a => items.All(y => y.ID != a.Data.ParentId));/*在选中的集合a中，只改变“父节点不在集合a中的”节点*/
            foreach (var designerItem in itemsToChangeParent)
            {
                designerItem.Data.ParentId = newParent == null ? Guid.Empty : newParent.ID;
                var connections = GetItemConnections(designerItem).Where(x => Equals(x.Sink.ParentDesignerItem, designerItem)).ToList();
                if (connections.Any())//有连线
                {
                    connections.ForEach(connection =>
                    {
                        if (newParent != null)/*有新父节点，则改变连线到新父节点*/ { connection.Source = GetItemConnector(newParent, PARENT_CONNECTOR); }
                        else/*没有新父节点，则移除连线*/ { RemoveConnection(connection); }
                    });
                }
                else if /*没有连线，但是有新父节点*/(newParent != null) { CreateConnection(newParent, designerItem); }
            }
        }
        void CreateConnection(DesignerItem newParent, DesignerItem designerItem)
        {
            var source = GetItemConnector(newParent, PARENT_CONNECTOR);
            var sink = GetItemConnector(designerItem, CHILD_CONNECTOR);
            var connection = new Connection(source, sink);
            source.Connections.Add(connection);
            _diagramControl.DesignerCanvas.Children.Add(connection);
        }
        void RemoveConnection(Connection connection)
        {
            connection.Source.Connections.Remove(connection);
            connection.Sink.Connections.Remove(connection);
            connection.Source = null;
            connection.Sink = null;
            _diagramControl.DesignerCanvas.Children.Remove(connection);
        }
        void RemoveHelperConnection/*移除找parent的辅助红线*/()
        {
            var connections = GetConnections().Where(x => x.toNewParent).ToList();
            if (connections.Any())//如果有连线，则将连线链接到parent
            {
                connections.ForEach(RemoveConnection);
            }
        }
        void SetConnectionColor/*设定连线颜色*/(Connection connection, Brush colorBrushes)
        {
            Measure();
            var path = connection.Template.FindName("PART_ConnectionPath", connection) as Path;
            if (path != null)
            {
                path.Stroke = colorBrushes;
            }
        }
        #endregion

        #region Selected items operations
        void HideOthers/*隐藏了除了drag item以外的selected items*/(DesignerItem selectedItem)
        {
            var selectedItems = GetSelectedItems();
            selectedItem.IsExpanderVisible = false;
            foreach (var designerItem in selectedItems.Where(x => x.ID != selectedItem.ID))
            {
                designerItem.Visibility = Visibility.Hidden;
            }
        }
        void ShowOthers/*恢复显示选中元素,之前调用了HideOthers隐藏了除了drag item以外的selected items*/()
        {
            var selectedItems = _diagramControl.DesignerItems;//GetSelectedItems();
            foreach (var selectedItem in selectedItems)
            {
                selectedItem.Visibility = Visibility.Visible;
                if (_diagramControl.DesignerItems.Any(x => x.Data.ParentId == selectedItem.Data.Id))/*如果有子节点*/
                {
                    selectedItem.IsExpanderVisible = selectedItem.CanCollapsed;/*可折叠，则显示折叠/展开按钮*/
                }
            }
        }
        #endregion

        #region Canvas items operations
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
        List<DesignerItem> GetDesignerItems/*取得画布所有元素*/()
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
        DesignerItem CreateShadow/*拖动时创建的影子*/(DesignerItem item)
        {
            var shadow = new DesignerItem(_diagramControl)
            {
                IsShadow = true,
                ShadowOrignal = item,
                DataContext = item,
                Oldx = item.Oldx,
                Oldy = item.Oldy,
                Data = { Text = item.Data.Text, XIndex = item.Oldx, YIndex = item.Oldy },
                Width = item.Width
            };
            Canvas.SetLeft(shadow, item.Oldx);
            Canvas.SetTop(shadow, item.Oldy);
            Panel.SetZIndex(shadow, -100);
            GenerateDesignerItemContent(shadow, SHADOW_FONT_COLOR_BRUSH);
            _diagramControl.DesignerCanvas.Children.Add(shadow);
            ChangeOriginalItemConnectionToShadow(item, shadow);
            return shadow;
        }
        void RemoveShadows/*移除画布上所有shadow*/()
        {
            foreach (var shadow in GetDesignerItems().Where(x => x.IsShadow))
            {
                _diagramControl.DesignerCanvas.Children.Remove(shadow);
            }
        }
        #endregion

        #endregion

        #region Add,Edit,Delete,Copy
        public void AddDesignerItem(ItemDataBase item)
        {
            var parentDesignerItem = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == item.ParentId);
            var designerItem = new DesignerItem(item.Id, item.ParentId, item, _diagramControl);
            _diagramControl.DesignerItems.Add(designerItem);
            DrawChild(parentDesignerItem, designerItem);
            SetSelectItem(designerItem);
            ArrangeWithRootItems();
            BringToFront(designerItem);
            Scroll(designerItem);
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
            SetSelectItem(selectedDesignerItem);
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

        #region Create items from datasource
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

        #region Select Operations
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
            if (selectedDesignerItem != null) SetSelectItem(selectedDesignerItem);
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
            if (selectedDesignerItem != null) SetSelectItem(selectedDesignerItem);
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
            sv.ScrollToVerticalOffset(Canvas.GetTop(designerItem) - 500);
            sv.ScrollToHorizontalOffset(Canvas.GetLeft(designerItem) - 500);
        }
        #endregion
    }
}