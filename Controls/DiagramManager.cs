using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DiagramDesigner
{
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
        List<Connection> GetItemConnections/*取得所有连线*/(DesignerItem designerItem)
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
                _diagramControl.DesignerItems.Where(x => x.ItemParentId == item.ItemId).OrderBy(x => x.YIndex).ToList();

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
                .Where(x => x.ItemParentId == item.ItemId)
                .OrderBy(x => x.YIndex).ToList();
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
            _diagramControl.SelectedItem = designerItem;
            _diagramControl.SelectedItems.Clear();
            _diagramControl.SelectedItems.Add(designerItem);
        }

        #endregion

        void GenerateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush fontColorBrush)
        {
            if (item == null) return;
            var textblock = new TextBlock()
            {
                Name = "DesignerItemText",
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
            //var roots = InitDesignerItems();/*利用数据源ItemDatas创建DesignerItem*/
            //if (roots == null) return;
            //if (!roots.Any()) return;/*创建DesignerItems*/
            DrawItems();

            ArrangeWithRootItems();/*将DesignerItems放到画布上，并且创建连线*/

            SetSelectItem(_diagramControl.DesignerItems.FirstOrDefault(x => String.IsNullOrEmpty(x.ItemParentId)));
        }
        void DrawItems()
        {
            _diagramControl.DesignerCanvas.Children.Clear();
            if (_diagramControl.DesignerItems == null) return;
            if (!_diagramControl.DesignerItems.Any()) return;
            var roots = _diagramControl.DesignerItems.Where(x => String.IsNullOrEmpty(x.ItemParentId)).ToList();
            roots.ForEach(root =>
            {
                DrawDesignerItems(root);
            });
        }
        List<DesignerItem> DrawDesignerItems(DesignerItem parentItem)
        {
            var designerItems = new List<DesignerItem>();
            if (parentItem == null) return designerItems;
            if (designerItems.All(x => !x.ItemId.Equals(parentItem.ItemId))
                && String.IsNullOrEmpty(parentItem.ItemParentId))
            { DrawRoot(parentItem, parentItem.YIndex, parentItem.XIndex); }
            var childs = _diagramControl.DesignerItems.Where(x => x.ItemParentId == (parentItem.ItemId));
            foreach (var childItem in childs)
            {
                if (designerItems.All(x => !x.ItemId.Equals(childItem.ItemId))) { DrawChild(parentItem, childItem); }
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
            _diagramControl.DesignerCanvas.Children.Add(item);
            _diagramControl.UpdateLayout();
            Canvas.SetTop(item, topOffset);
            Canvas.SetLeft(item, leftOffset);
        }

        #endregion

        #region Arrange Items
        public void ArrangeWithRootItems()
        {
            var items = _diagramControl.DesignerItems.ToList();
            var roots = items.Where(x => string.IsNullOrEmpty(x.ItemParentId));
            foreach (var root in roots)
            {
                //设定节点宽度
                SetWidth(root);
                //设定节点位置
                root.YIndex = Canvas.GetTop(root);
                root.Oldy = Canvas.GetTop(root);
                root.XIndex = Canvas.GetLeft(root);
                root.Oldx = Canvas.GetLeft(root);
                ArrangeWithRootItems(root);
            }
        }
        void ArrangeWithRootItems/*递归方法，给定根节点，重新布局*/(DesignerItem designerItem/*根节点*/)
        {
            if (designerItem == null) return;
            designerItem.SetTemplate();
            var directSubItems = GetDirectSubItems(designerItem);
            if (directSubItems == null || directSubItems.Count == 0) return;
            var subItems = directSubItems
                .Where(x => x.Visibility.Equals(Visibility.Visible)).
                OrderBy(x => x.YIndex).ToList();
            double h1 = 0;/*父节点的直接子节点总高度*/
            for (var i = 0; i < subItems.Count; i++)
            {
                if (i != 0) h1 += subItems[i - 1].ActualHeight;
                var list = new List<DesignerItem>();
                for (var j = 0; j < i; j++) { list.AddRange(GetAllSubItems(subItems.ElementAt(j)).Where(item => !list.Contains(item))/**/); }
                var preChilds = list.OrderBy(x => x.YIndex).Where(x => x.Visibility.Equals(Visibility.Visible));
                var h2 = preChilds.Sum(preChild => preChild.ActualHeight);/*父节点的直接子节点的所有子节点的总高度*/
                #region 设定节点位置
                //上
                var top = designerItem.YIndex + designerItem.ActualHeight + h1 + h2;
                Canvas.SetTop(subItems.ElementAt(i), top);
                subItems.ElementAt(i).YIndex = top;
                subItems.ElementAt(i).Oldy = top;
                //左
                var left = designerItem.XIndex + GetOffset(designerItem);
                Canvas.SetLeft(subItems.ElementAt(i), left);
                subItems.ElementAt(i).XIndex = left;
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
            if (designerItem.Data != null && designerItem.Text != null)
            {
                string text = designerItem.Text;
                FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Arial"), FONT_SIZE, Brushes.Black);
                double width = formattedText.Width + 12;
                double height = formattedText.Height;
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

        private TextBlock GetTextBlock/*元素文字控件*/(DesignerItem item)
        {
            return item.Content as TextBlock;
        }
        #endregion

        #region Set Visual Item

        private void SetItemFontColor/*设定元素文字颜色*/(DesignerItem item, SolidColorBrush fontColorBrush)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return;
            textBlock.SetValue(TextBlock.ForegroundProperty, fontColorBrush);
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
                var parents = _diagramControl.DesignerItems.Where(x => x.ItemId == item.ItemParentId).ToList();
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

        #region 拖拽时，子元素及边框边框变灰
        public void SetDragItemChildFlag()
        {
            GetSelectedItems().ForEach(selectedItem =>
            {
                GetAllSubItems(selectedItem).ForEach(x =>
                {
                    x.IsDragItemChild = true;
                    SetItemFontColor(x, SHADOW_FONT_COLOR_BRUSH);
                    GetItemConnections(x).ForEach(c =>
                    {
                        SetConnectionColor(c, Brushes.LightGray);
                    });
                });

            });
        }
        public void RestoreDragItemChildFlag()
        {
            GetSelectedItems().ForEach(selectedItem =>
            {
                GetAllSubItems(selectedItem).ForEach(x =>
                {
                    x.IsDragItemChild = false;
                    SetItemFontColor(x, DEFAULT_FONT_COLOR_BRUSH);
                    GetItemConnections(x).ForEach(c =>
                    {
                        SetConnectionColor(c, Brushes.LightSkyBlue);
                    });
                });

            });
        }
        #endregion
        public DesignerItem ChangeParent(DesignerItem designerItem)
        {
            HideOthers(designerItem);
            var newParent = GetNewParent(designerItem);
            _diagramControl.DesignerItems.ToList().ForEach(x => { x.IsNewParent = false; });
            if (newParent != null) newParent.IsNewParent = true;
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


            var itemTop = Canvas.GetTop(selectedItem) - selectedItem.ActualHeight / 2;
            var itemsOnCanvas = _diagramControl.DesignerCanvas.Children;
            var designerItemsOnCanvas = itemsOnCanvas.OfType<DesignerItem>().ToList();
            var downItems = designerItemsOnCanvas.Where(x =>
                x.Oldy > itemTop
                && x.ItemId != selectedItem.ItemId
               ).ToList();/*比元素大的，全部向下移*/

            foreach (var designerItem in downItems)
            {
                Canvas.SetTop(designerItem, designerItem.Oldy + selectedItem.ActualHeight);
            }
            var upItems = designerItemsOnCanvas.Where(x =>
                x.Oldy < itemTop
                && x.Oldy > Canvas.GetTop(parent)
                && x.ItemId != selectedItem.ItemId
                ).ToList();/*比父节点大的，比元素小的，恢复位置*/
            foreach (var designerItem in upItems)
            {
                //Canvas.SetTop(designerItem, designerItem.Data.YIndex);
                //var item = designerItem.IsShadow ? designerItem.ShadowOrignal : designerItem;
                //var x1 = GetAllSubItems(item);
                //if (x1 == null || !x1.Any()) continue;
                //var list = designerItemsOnCanvas.Where(x =>
                //    x.Oldy <= x1.Aggregate((a, b) => a.Oldy > b.Oldy ? a : b).Oldy
                //    && x.ID != selectedItem.ID
                //    ).ToList();
                //list.ForEach(x => { Canvas.SetTop(x, x.Data.YIndex); });

                Canvas.SetTop(designerItem, designerItem.Oldy);
                var item = designerItem.IsShadow ? designerItem.ShadowOrignal : designerItem;
                var x1 = GetAllSubItems(item);
                if (x1 == null || !x1.Any()) continue;
                var list = designerItemsOnCanvas.Where(x =>
                    x.Oldy <= x1.Aggregate((a, b) => a.Oldy > b.Oldy ? a : b).Oldy
                    && x.ItemId != selectedItem.ItemId
                    ).ToList();
                list.ForEach(x => { Canvas.SetTop(x, x.Oldy); });
            }
        }
        public void FinishChangeParent(DesignerItem newParent)
        {
            ShowOthers();/*恢复显示选中元素,之前调用了HideOthers隐藏了除了drag item以外的selected items*/
            RemoveHelperConnection();/*移除找parent的辅助红线*/
            ChangeShadowConnectionsToOriginalItem();/*将连接到shadow上的连线，恢复到item上*/
            _diagramControl.DesignerItems.ToList().ForEach(x => { x.IsNewParent = false; x.YIndex = Canvas.GetTop(x); });
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
            var parent = list.Aggregate((a, b) => a.YIndex > b.YIndex ? a : b);
            return parent;
        }
        #endregion

        #region Connection operations
        void ChangeOriginalItemConnectionToShadow/*将item上的连线，连接到shadow上*/(DesignerItem item, DesignerItem shadow)
        {
            if (item == null || shadow == null) return;
            _diagramControl.UpdateLayout();

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
            var itemsToChangeParent = items.Where(a => items.All(y => y.ItemId != a.ItemParentId));/*在选中的集合a中，只改变“父节点不在集合a中的”节点*/
            foreach (var designerItem in itemsToChangeParent)
            {
                designerItem.ItemParentId = newParent == null ? null : newParent.ItemId;
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
            _diagramControl.UpdateLayout();
            var path = connection.Template.FindName("PART_ConnectionPath", connection) as Path;
            if (path != null)
            {
                path.Stroke = colorBrushes;
                //path.StrokeThickness = 2;
            }
        }
        #endregion

        #region Selected items operations
        void HideOthers/*隐藏了除了drag item以外的selected items*/(DesignerItem selectedItem)
        {
            var selectedItems = GetSelectedItems();
            selectedItem.IsExpanderVisible = false;
            foreach (var designerItem in selectedItems.Where(x => x.ItemId != selectedItem.ItemId))
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
                if (_diagramControl.DesignerItems.Any(x => x.ItemParentId == selectedItem.ItemId))/*如果有子节点*/
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
                Text = item.Text,
                XIndex = item.XIndex,
                YIndex = item.YIndex,
                //Data = { Text = item.Data.Text, XIndex = item.Oldx, YIndex = item.Oldy },
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

        #region Add,Edit,Delete,Cut,Copy,Paste
        /*public void AddDesignerItem(ItemDataBase item)
        {
            var parentDesignerItem = _diagramControl.DesignerItems.FirstOrDefault(x => x.ItemId == item.ItemParentId);
            var designerItem = new DesignerItem(item.ItemId, item.ItemParentId, item, _diagramControl);
            _diagramControl.DesignerItems.Add(designerItem);
            DrawChild(parentDesignerItem, designerItem);
            SetSelectItem(designerItem);
            ArrangeWithRootItems();
            Scroll(designerItem);
        }*/
        #region Edit
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

            var kbCtrlEnter = new KeyBinding
            {
                Key = Key.Enter,
                Modifiers = ModifierKeys.Control,
                Command = new RelayCommand(() =>
                {
                    t.Text += Environment.NewLine;
                    t.SelectionStart = t.Text.Length;
                })
            };
            t.InputBindings.Add(kbCtrlEnter);

            var kbEnter = new KeyBinding
            {
                Key = Key.Enter,
                Command = new RelayCommand(() =>
                {
                    if (_diagramControl.IsOnEditing)
                    {
                        _diagramControl.DesignerCanvas.Children.Remove(t);
                        ArrangeWithRootItems();
                        _diagramControl.IsOnEditing = false;
                    }
                })
            };
            t.InputBindings.Add(kbEnter);
        }
        #endregion
        #region Delete
        /*public void DeleteDesignerItem(ItemDataBase itemDataBase)
        {
            var child = GetAllChildItemDataBase(itemDataBase.ItemId);
            _diagramControl.RemovedItemDataBase.AddRange(child);
            _diagramControl.RemovedItemDataBase.Add(itemDataBase);
            child.ForEach(c =>
            {
                DeleteItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ItemId == c.ItemId));
            });
            DeleteItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ItemParentId == itemDataBase.ItemId));

            var c1 = _diagramControl.DesignerItems.Where(x => x.ItemParentId == itemDataBase.ItemParentId).ToList();
            DesignerItem selectedDesignerItem = null;
            selectedDesignerItem = c1.Any() ?
                c1.Aggregate((a, b) => a.YIndex > b.YIndex ? a : b) :
                _diagramControl.DesignerItems.FirstOrDefault(x => x.ItemParentId == itemDataBase.ItemParentId);
            SetSelectItem(selectedDesignerItem);
            ArrangeWithRootItems();
            Scroll(selectedDesignerItem);
        }
        List<ItemDataBase> GetAllChildItemDataBase(string id)
        {
            List<ItemDataBase> result = new List<ItemDataBase>();
            var child = _diagramControl.ItemDatas.Where(x => x.ItemParentId == id);
            foreach (var itemDataBase in child)
            {
                result.Add(itemDataBase);
                result.AddRange(GetAllChildItemDataBase(itemDataBase.ItemId));
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
        }*/
        #endregion
        #region Copy&Paste
        List<ItemDataBase> _clipbrd = new List<ItemDataBase>();//clipboard 复制，剪切板

        public void Cut()
        {
            _clipbrd.Clear();
            //foreach (var selectedItem in GetSelectedItems())
            //{
            //    _clipbrd.Add(selectedItem.Data);
            //    _diagramControl.ItemDatas.Remove(selectedItem.Data);
            //}

        }
        public void Copy()
        {
            _clipbrd.Clear();
            //foreach (var selectedItem in GetSelectedItems())
            //{
            //    _clipbrd.Add(selectedItem.Data);
            //}
        }
        public void Paste()
        {

        }
        #endregion
        #endregion



        #region Select Operations
        public void SelectUpDown(DesignerItem selectedItem, bool selectUp)
        {
            if (selectedItem == null) return;
            var siblingDesignerItems = _diagramControl.DesignerItems.Where(x => x.ItemParentId == selectedItem.ItemParentId).ToList();
            DesignerItem selectedDesignerItem = null;

            if (selectUp)
            {
                var top = siblingDesignerItems
                    .Where(x => x.YIndex < selectedItem.YIndex).ToList();
                if (top.Any())
                {
                    selectedDesignerItem =
                        top.Aggregate((a, b) => a.YIndex > b.YIndex ? a : b);
                }
                else
                {
                    var parent =
                        _diagramControl.DesignerItems.Where(x => x.ItemId == selectedItem.ItemParentId).ToList();
                    if (parent.Count() == 1)
                        selectedDesignerItem = parent.FirstOrDefault();
                }
            }
            else //move down
            {
                var down = siblingDesignerItems
                    .Where(x => x.YIndex > selectedItem.YIndex).ToList();
                if (down.Any()) //1.优先找相邻节点，处于下方的节点
                {
                    selectedDesignerItem = down.Aggregate((a, b) => a.YIndex < b.YIndex ? a : b);
                }
                else //没有处于下方的相邻节点，2.有父亲节点，则找其父亲的，处于下方的相邻节点，3.如果没有父亲节点，就找子节点
                {
                    var parents =
                        _diagramControl.DesignerItems.Where(x => x.ItemId == selectedItem.ItemParentId).ToList();
                    if (parents.Count() == 1 && parents.FirstOrDefault() != null) //有父节点，父节点邻居，处于下方的节点
                    {
                        var parent = parents.FirstOrDefault();
                        if (parent != null)
                        {
                            var parentSibling =
                                _diagramControl.DesignerItems.Where(
                                    x => x.ItemParentId == parent.ItemParentId
                                         && x.YIndex > parent.YIndex).ToList();
                            if (parentSibling.Any())
                            {
                                selectedDesignerItem =
                                    parentSibling.Aggregate((a, b) => a.YIndex > b.YIndex ? a : b);
                            }
                        }
                    }
                    if (selectedDesignerItem == null)//没有父节点，找子节点
                    {
                        var child = _diagramControl.DesignerItems.Where(x => x.ItemParentId == selectedItem.ItemId).ToList();
                        if (child.Any())
                        {
                            selectedDesignerItem = child.Aggregate((a, b) => a.YIndex < b.YIndex ? a : b);
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
                    var child = _diagramControl.DesignerItems.Where(x => x.ItemParentId == selectedItem.ItemId).ToList();
                    if (child.Any())
                    {
                        selectedDesignerItem = child.Aggregate((a, b) => a.YIndex < b.YIndex ? a : b);
                    }
                }
                else
                {
                    var parent = _diagramControl.DesignerItems.Where(x => x.ItemId == selectedItem.ItemParentId).ToList();
                    if (parent.Any())
                    {
                        selectedDesignerItem = parent.Aggregate((a, b) => a.YIndex > b.YIndex ? a : b);
                    }
                }
            }
            if (selectedDesignerItem != null) SetSelectItem(selectedDesignerItem);
        }
        List<DesignerItem> GetSelectedItems()
        {
            var selectedItems =
                _diagramControl.DesignerCanvas.SelectionService.CurrentSelection.ConvertAll(x => x as DesignerItem);
            return selectedItems;
        }
        void Scroll(DesignerItem designerItem)
        {
            if (designerItem == null) return;
            var sv = (ScrollViewer)_diagramControl.Template.FindName("DesignerScrollViewer", _diagramControl);
            sv.ScrollToVerticalOffset(Canvas.GetTop(designerItem) - 400);
            sv.ScrollToHorizontalOffset(Canvas.GetLeft(designerItem) - 400);
        }

        #endregion
    }
}