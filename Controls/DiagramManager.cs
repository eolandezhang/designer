using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public CommandManager CommandManager { get; set; }
        public DiagramManager(DiagramControl diagramControl)
        {
            _diagramControl = diagramControl;
            CommandManager = new CommandManager(this);
        }

        #region Setters
        private const double ChildTopOffset = 35;
        private const double LeftOffset = 20;
        private const int DefaultBorderThickness = 2;
        private const int HighlightBorderThickness = 2;
        private const double MinItemWidth = 150d;
        static readonly SolidColorBrush DefaultBorderBrush = Brushes.SkyBlue;
        static readonly SolidColorBrush SelectedBorderBrush = Brushes.DeepSkyBlue;
        static readonly SolidColorBrush HighlightBorderBrush = Brushes.IndianRed;
        private static readonly SolidColorBrush HighlightBackgroundBrush = Brushes.DeepSkyBlue;
        private static readonly SolidColorBrush DefaultBackgroundBrush = Brushes.SkyBlue;
        static readonly SolidColorBrush ShadowBorderBrush = Brushes.LightGray;
        private static readonly SolidColorBrush ShadowBackgroundBrush = Brushes.LightGray;
        private static readonly SolidColorBrush ShadowFontColorBrush = Brushes.Gray;
        private static readonly SolidColorBrush DefaultFontColorBrush = Brushes.Black;
        #endregion

        #region Get

        private List<DesignerItem> GetRootItem/*取得画布上的根节点*/()
        {
            return (from designerItem in GetDesignerItems()
                    let sink = GetItemConnector(designerItem, "Left")
                    where sink != null && sink.Connections.Count == 0
                    select designerItem).ToList();
        }
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
        public List<DesignerItem> GetDesignerItems/*取得画布所有元素*/()
        {
            var list = new List<DesignerItem>();

            var itemCount = VisualTreeHelper.GetChildrenCount(_diagramControl.Designer);
            if (itemCount == 0) return list;
            for (int n = 0; n < itemCount; n++)
            {
                var c = VisualTreeHelper.GetChild(_diagramControl.Designer, n);
                var child = c as DesignerItem;
                if (child != null) list.Add(child);
            }
            return list;
        }
        private List<DesignerItem> GetDirectSubItems/*取得直接子节点*/(DesignerItem item)
        {
            //var child = _diagramControl.DesignerItems.Where(
            //    x => x.ParentID == item.ID
            //        &&x.Data.Removed==false
            //    ).OrderBy(x => x.Data.YIndex).ToList();
            var connections = GetItemConnections(item);
            var list = (from itemConnection in connections
                        where Equals(itemConnection.Source.ParentDesignerItem, item)
                        && itemConnection.Source != null && itemConnection.Sink != null
                        select itemConnection.Sink.ParentDesignerItem).OrderBy(x => x.Data.YIndex).ToList();
            var hasChild = list.Any(x => x.Data.Removed == false);

            if (item.CanCollapsed == false)
            {
                item.IsExpanderVisible = false;
            }
            else if (hasChild)
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
            foreach (var subItem in
                (from itemConnection in GetItemConnections(item)
                 where Equals(itemConnection.Source.ParentDesignerItem, item)
                 && itemConnection.Sink.ParentDesignerItem.Data.Removed == false
                 select itemConnection.Sink.ParentDesignerItem).OrderBy(x => x.Data.YIndex))
            {
                if (subitems.Contains(subItem)) continue;
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
            var connector = GetItemConnector(item, "Left");
            var connection = connector.Connections.FirstOrDefault(x => x.Sink != null && x.Source != null);
            if (connection != null)
            {
                return connection.Source.ParentDesignerItem;
            }
            return null;
        }
        public DesignerItem GetSelectedItem()
        {
            List<DesignerItem> selectedItems = _diagramControl.Designer.SelectionService.CurrentSelection.ConvertAll((a) => a as DesignerItem);
            if (selectedItems.Count == 1)
            {
                return selectedItems.FirstOrDefault();
            }
            return null;
        }

        #endregion

        #region Set

        public void SetSelectItem(DesignerItem designerItem)
        {
            _diagramControl.Designer.SelectionService.ClearSelection();
            _diagramControl.Designer.SelectionService.SelectItem(designerItem);
        }

        #endregion

        #region Create
        public List<DesignerItem>/*根节点*/ GenerateItems()
        {
            _diagramControl.Designer.Children.Clear();
            if (_diagramControl.DesignerItems.Count == 0) return null;
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
                }
            }
            else
            {
                parentDesignerItem = designerItems.FirstOrDefault(x => x.ID.Equals(parentItem.ID));
            }

            var childs = _diagramControl.DesignerItems.Where(x => x.Data.ParentId.Equals(parentItem.ID));

            foreach (var childItem in childs)
            {
                DesignerItem childDesignerItem;
                if (designerItems.All(x => !x.ID.Equals(childItem.ID)))
                {
                    childDesignerItem = CreateChild(parentDesignerItem, childItem);
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
            SetItemFontColor(root, DefaultFontColorBrush);
            root.CanCollapsed = false;
            root.IsExpanderVisible = false;
            return root;
        }
        private DesignerItem CreateChild(DesignerItem parent, DesignerItem childItem)
        {
            if (parent == null) return null;

            #region 起点 Connector

            var parentConnectorDecorator = parent.Template.FindName("PART_ConnectorDecorator", parent) as Control;
            if (parentConnectorDecorator == null) return null;
            var source = parentConnectorDecorator.Template.FindName("Bottom", parentConnectorDecorator) as Connector;

            #endregion

            #region 终点 Connector

            var child = CreateDesignerItem(
                childItem,
                Canvas.GetTop(parent) + ChildTopOffset,
                Canvas.GetLeft(parent) + GetOffset(parent),
                DefaultBorderBrush);/*创建子节点*/

            var childConnectorDecorator = child.Template.FindName("PART_ConnectorDecorator", child) as Control;
            if (childConnectorDecorator == null) return null;
            var sink = childConnectorDecorator.Template.FindName("Left", childConnectorDecorator) as Connector;

            #endregion

            #region 创建连线

            if (source == null || sink == null) return null;

            var connections = GetItemConnections(parent).ToList();
            var c = connections.Where(connection => connection.Source.Equals(source) && connection.Sink.Equals(sink)).ToList();
            //var c = connections.Where(x => x.Source.Equals(source) && x.Sink.Equals(sink)).ToList();
            if (c.Count == 0 || c.FirstOrDefault() == null)
            {
                var conn = new Connection(source, sink); /*创建连线*/
                if (!childItem.Data.Removed)
                {
                    _diagramControl.Designer.Children.Add(conn); /*放到画布上*/
                }
            }
            else if (c.Count == 1)
            {
                var cn = c.FirstOrDefault();
                if (cn != null)
                {
                    if (!childItem.Data.Removed)
                    {
                        _diagramControl.Designer.Children.Add(cn);
                    }
                }
            }
            else if (c.Count > 1)//正常情况不会发生
            {
                foreach (var connection in c)
                {
                    connection.Source = null;
                    connection.Sink = null;
                    var conn = new Connection(source, sink); /*创建连线*/
                    if (!childItem.Data.Removed)
                    {
                        _diagramControl.Designer.Children.Add(conn); /*放到画布上*/
                    }
                }
            }
            #endregion

            child.CanCollapsed = true;
            return child;/*返回创建的子节点*/
        }
        private DesignerItem CreateDesignerItem/*创建元素*/(DesignerItem item, double topOffset, double leftOffset, SolidColorBrush borderBrush = null/*节点边框颜色*/)
        {
            var newItem = item;
            if (newItem.Data == null) return null;
            CreateDesignerItemContent(item, borderBrush);
            item.Width = MinItemWidth;
            newItem.SetValue(Canvas.TopProperty, topOffset);
            newItem.SetValue(Canvas.LeftProperty, leftOffset);
            if (!newItem.Data.Removed)
            {
                _diagramControl.Designer.Children.Add(newItem);
            }
            _diagramControl.Designer.Measure(Size.Empty);
            return newItem;
        }
        private void CreateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush borderBrush = null)
        {
            if (item == null) return;
            if (item.Data != null && item.Data.Text == (GetItemText(item))) { return; }
            if (borderBrush == null) borderBrush = DefaultBorderBrush;
            var border = new Border()
            {
                BorderThickness = new Thickness(DefaultBorderThickness),
                BorderBrush = borderBrush,
                Background = DefaultBackgroundBrush,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                IsHitTestVisible = false
            };
            var textblock = new TextBlock()
            {
                Name = "Text",
                Text = item.Data == null ? "" : item.Data.Text,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 2, 5, 2),
                FontFamily = new FontFamily("Arial"),
                FontSize = 12d
            };
            border.Child = textblock;
            item.Content = border;
        }
        public void BindData/*将DesignerItems放到画布上，并且创建连线*/()
        {
            var designer = _diagramControl.Designer;
            if (designer == null) return;

            var designerItems = GenerateItems();
            if (designerItems == null) return;
            if (!designerItems.Any()) return;
            ArrangeWithRootItems();
            _diagramControl.Suppress = true;
            var items = GetDesignerItems();
            if (items == null) return;
            items.ForEach(x => x.Data.DiagramControl = _diagramControl);
            items.ForEach(x =>
            {
                x.MouseDoubleClick += (sender, e) =>
                {
                    SetSelectItem(x);
                    Edit(designer, x);
                };
            });
            _diagramControl.Suppress = false;
        }

        public void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/()
        {
            _diagramControl.Suppress = true;/*利用数据源创建节点时不执行CollectionChange事件*/

            var roots = InitDesignerItems();
            if (!roots.Any()) InitData();/*创建DesignerItems*/
            BindData();/*将DesignerItems放到画布上，并且创建连线*/
            _diagramControl.SelectedItem = roots.FirstOrDefault();
            _diagramControl.Suppress = false;
            _diagramControl.GetDataInfo();
        }


        #endregion

        #region Arrange

        public void ArrangeWithRootItems()
        {
            var items = GetDesignerItems();
            if (items == null) return;
            var roots = items.Where(x => x.Data.ParentId.Equals(Guid.Empty));
            foreach (var root in roots)
            {
                if (root != null)
                {
                    ArrangeWithRootItems(root);
                }
            }
        }
        void ArrangeWithRootItems/*给定根节点，重新布局*/(DesignerItem rootItem/*根节点*/)
        {
            if (rootItem == null) return;
            var directSubItems = GetDirectSubItems(rootItem);
            if (directSubItems == null) return;
            if (directSubItems.Count == 0) return;
            var rootSubItems =
                directSubItems.Where(x => x.Visibility.Equals(Visibility.Visible)).ToList();
            rootItem.Data.YIndex = (double)rootItem.GetValue(Canvas.TopProperty);
            rootItem.Data.XIndex = (double)rootItem.GetValue(Canvas.LeftProperty);
            for (var i = 0; i < rootSubItems.Count; i++)
            {
                //计算之前的所有子节点个数
                var list = new List<DesignerItem>();
                for (var j = 0; j < i; j++)
                {
                    GetAllSubItems(rootSubItems.ElementAt(j), list);
                }
                var preChildCount = list.OrderBy(x => x.Data.YIndex).Count(x => x.Visibility.Equals(Visibility.Visible));

                //设置top
                var top = rootItem.Data.YIndex + (preChildCount + i + 1) * ChildTopOffset;
                var left = rootItem.Data.XIndex + GetOffset(rootItem);
                rootSubItems.ElementAt(i).SetValue(Canvas.TopProperty, top);
                rootSubItems.ElementAt(i).Data.YIndex = top;
                //设置left
                rootSubItems.ElementAt(i).SetValue(Canvas.LeftProperty, left);
                rootSubItems.ElementAt(i).Data.XIndex = left;
                SetItemFontColor(rootSubItems.ElementAt(i), DefaultFontColorBrush);
                ArrangeWithRootItems(rootSubItems.ElementAt(i));
            }

            SendConnectionsToBack(_diagramControl.Designer);
        }
        private double GetOffset(FrameworkElement item)
        {
            return item.Width.Equals(0) ? 30 : (item.Width * 0.1 + LeftOffset);
        }
        #endregion

        #region Style

        #region Highlight

        public void HighlightParent/*拖动时高亮父节点*/(DesignerItem designerItem)
        {
            List<DesignerItem> subItems = new List<DesignerItem>();
            GetAllSubItems(designerItem, subItems);

            foreach (var item in GetDesignerItems()
                .Where(item => item.IsShadow == false && !item.Equals(designerItem)))
            {
                if (!subItems.Contains(item))
                    SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness),
                        DefaultBackgroundBrush);
                else
                {
                    SetItemBorderStyle(item, ShadowBackgroundBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
                    SetItemFontColor(item, ShadowFontColorBrush);
                }
            }
            var parent = GetTopSibling(designerItem);
            if (parent != null)
            {
                SetItemBorderStyle(parent, HighlightBorderBrush, new Thickness(HighlightBorderThickness),
                    DefaultBackgroundBrush);

            }
        }
        public void HighlightSelected/*高亮选中*/(DesignerItem item)
        {
            SetItemBorderStyle(item, SelectedBorderBrush, new Thickness(HighlightBorderThickness), HighlightBackgroundBrush);
            SetItemFontColor(item, DefaultFontColorBrush);
        }

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

        private void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer, DesignerItem selectedItem)
        {
            foreach (var item in GetDesignerItems().Where(item => !item.Equals(selectedItem)))
            {
                if (item.IsShadow) continue;
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }
        public void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer)
        {
            foreach (var item in GetDesignerItems())
            {
                if (item.IsShadow) continue;
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }

        #endregion

        #endregion

        #region Expand & Collapse

        public void ExpandAll/*展开所有*/()
        {
            var items = GetDesignerItems();
            foreach (var item in items)
            {
                item.IsExpanded = true;
            }
        }
        public void CollapseAll/*折叠所有，除了根节点*/()
        {
            var items = GetDesignerItems();
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

        public void FinishDraging/*改变父节点，移除影子，显示连线，重新布局*/(DesignerItem designerItem)
        {
            if (designerItem == null) return;
            RemoveShadows(_diagramControl.Designer);/*移除影子*/
            ChangeParent(designerItem);/*改变父节点*/
            var selectedItems = _diagramControl.Designer.SelectionService.CurrentSelection.ConvertAll((a) => a as DesignerItem);
            ShowItemConnection(selectedItems);/*拖动完毕，显示连线*/
            ResetBrushBorderFontStyle(_diagramControl.Designer, designerItem);/*恢复边框字体样式*/
            designerItem.Data.XIndex = Canvas.GetLeft(designerItem);
            designerItem.Data.YIndex = Canvas.GetTop(designerItem);
            ArrangeWithRootItems();/*重新布局*/
        }
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
        private void BringToFront/*将制定元素移到最前面*/(Canvas canvas, UIElement element)
        {
            List<UIElement> childrenSorted =
                (from UIElement item in canvas.Children
                 orderby Canvas.GetZIndex(item as UIElement) ascending
                 select item as UIElement).ToList();

            int i = 0;
            int j = 0;
            foreach (UIElement item in childrenSorted)
            {
                if (element.Equals(item))
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
        private void SendConnectionsToBack(Canvas canvas)
        {
            var childrens = canvas.Children;
            var connectionList = childrens.OfType<Connection>().ToList();
            foreach (var uiElement in connectionList)
            {
                Panel.SetZIndex(uiElement, -10000);
            }
        }
        private DesignerItem GetTopSibling/*取得元素上方最接近的元素*/(DesignerItem item)
        {
            var canvas = item.Parent as Canvas;
            if (canvas == null) return null;

            //取得所有子节点，让parent不能为子节点
            var subitems = new List<DesignerItem>();
            GetAllSubItems(item, subitems);

            var pre = GetDesignerItems().Where(x => x.Visibility.Equals(Visibility.Visible));
            var list = (from designerItem in pre
                        let top = Canvas.GetTop(designerItem)
                        let left = Canvas.GetLeft(designerItem)
                        let right = left + designerItem.ActualWidth
                        where top <= Canvas.GetTop(item) /*top位置小于自己的top位置*/
                        && left <= Canvas.GetLeft(item)
                        && right >= Canvas.GetLeft(item)
                        && !Equals(designerItem, item) /*让parent不能为自己*/
                        && !subitems.Contains(designerItem) /*让parent不能为子节点*/
                        && designerItem.IsShadow == false
                        select designerItem).ToList();

            if (!list.Any()) return null;
            var parent = list.OrderByDescending(x => x.Data.YIndex).First();
            return parent;
        }
        private void ChangeParent(DesignerItem item)
        {
            //找到上方最接近的节点，取得其下方的连接点
            if (item.Data.XIndex.Equals(item.oldx) && item.Data.YIndex.Equals(item.oldy)) return;

            var parent = GetTopSibling(item);
            if (parent != null)
            {
                var source = GetItemConnector(parent, "Bottom");
                if (source != null)
                {
                    //如果父节点折叠，则展开它
                    if (source.ParentDesignerItem.IsExpanded == false)
                    {
                        source.ParentDesignerItem.IsExpanded = true;
                    }
                    // 取得当前节点的连接线
                    var sink = GetItemConnector(item, "Left");
                    if (sink == null) return;
                    if (source.Equals(sink)) return;
                    var connections = sink.Connections.Where(x => x.Source != null && x.Sink != null).ToList();
                    if (!connections.Any())
                    {
                        sink.Connections.Clear();//从终点中移除所有连线
                        var conn = new Connection(source, sink);
                        if (!source.Connections.Contains(conn))
                        {
                            source.Connections.Add(conn);
                        }
                        if (!sink.Connections.Contains(conn))
                        {
                            sink.Connections.Add(conn);
                        }
                        _diagramControl.Designer.Children.Add(conn);
                        item.Data.ParentId = parent.ID;
                        _diagramControl.Designer.Measure(Size.Empty);
                    }
                    else
                    {
                        foreach (var c in connections)
                        {
                            c.Source.Connections.Remove(c);//从起点中移除此连线
                            var originalParent = c.Source.ParentDesignerItem;
                            var childs = GetDirectSubItems(originalParent);

                            if (childs.Count == 0 || childs.Any(x => x.Data.Removed == false))
                            {
                                originalParent.IsExpanderVisible = false;
                            }
                            else
                            {
                                originalParent.IsExpanderVisible = true;
                            }
                        }

                        var connection = connections.First();
                        connection.Source = source;
                        item.Data.ParentId = parent.ID;
                    }

                }
                ArrangeWithRootItems();
            }
            else
            {
                //如果没有找到父节点
                //则将其与原父节点的连线删除
                //将其父节点设定为无
                var sink = GetItemConnector(item, "Left");/*左连接点*/
                if (sink != null)
                {
                    var connections = sink.Connections;
                    foreach (var connection in connections)
                    {
                        connection.Source.Connections.Remove(connection);
                        _diagramControl.Designer.Children.Remove(connection);
                    }
                    sink.Connections.Clear();
                    item.Data.ParentId = Guid.Empty;
                }
            }
        }
        protected void HideItemConnection/*拖动元素，隐藏元素连线*/(DesignerItem item)
        {
            var itemTop = (double)item.GetValue(Canvas.TopProperty);
            var itemLeft = (double)item.GetValue(Canvas.LeftProperty);
            if (itemTop.Equals(item.oldy) && itemLeft.Equals(item.oldx)) return;

            foreach (var connection in GetItemConnections(item))
            {
                connection.Visibility = Visibility.Hidden;
            }

        }
        private void ShowItemConnection/*元素所有连线恢复显示*/(List<DesignerItem> items)
        {
            foreach (var item in items)
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
                }
            }
        }
        public List<DesignerItem> CreateShadows/*拖拽时产生影子*/(List<DesignerItem> designerItems)
        {
            var shadows = new List<DesignerItem>();
            foreach (var designerItem in designerItems)
            {
                DesignerItem shadow = null;
                if (_diagramControl.Designer != null)
                {
                    shadow = CreateShadow(designerItem);
                    _diagramControl.Designer.Children.Add(shadow);
                    //隐藏折叠按钮
                    designerItem.IsExpanderVisible = false;
                }
                HideItemConnection(designerItem);/*拖动时隐藏连线*/
                BringToFront(designerItem);
                shadows.Add(shadow);
            }
            return shadows;
        }
        private DesignerItem CreateShadow/*拖动时创建的影子*/(DesignerItem item)
        {
            var copy = new DesignerItem { IsExpanderVisible = false, IsShadow = true };
            CreateDesignerItemContent(copy);
            SetItemText(copy, GetItemText(item));
            SetItemBorderStyle(copy, ShadowBorderBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
            SetItemFontColor(copy, ShadowFontColorBrush);
            copy.SetValue(Canvas.LeftProperty, item.oldx);
            copy.SetValue(Canvas.TopProperty, item.oldy);
            copy.Width = item.Width;
            Panel.SetZIndex(copy, -100);
            return copy;
        }
        private void RemoveShadows(Canvas canvas)
        {
            foreach (var designerItem in GetDesignerItems().Where(x => x.IsShadow))
            {
                canvas.Children.Remove(designerItem);
            }
        }

        #endregion

        #region Item Operation

        public void AddSibling()
        {
            var designerItem = GetSelectedItem();
            if (designerItem != null)
            {
                if (designerItem.Data == null) return;
                if (designerItem.Data.ParentId.Equals(Guid.Empty)) { AddAfter(); return; }
                var n5 = Guid.NewGuid();
                if (_diagramControl.DesignerItems.Any(x => x.ID.Equals(n5))) { return; }
                var parent = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == designerItem.Data.ParentId);
                if (parent == null) return;
                var newitem = new DesignerItem(n5, new ItemDataBase(n5, parent.ID, GetText(), true, false, 0, double.MaxValue));
                _diagramControl.DesignerItems.Add(newitem);
                SetSelectItem(newitem);
                _diagramControl.GetDataInfo();
            }
        }
        public void AddAfter()
        {
            var parentDesignerItem = GetSelectedItem();
            if (parentDesignerItem != null)
            {
                if (parentDesignerItem.Data == null) return;
                var n5 = Guid.NewGuid();
                if (_diagramControl.DesignerItems.Any(x => x.ID.Equals(n5))) { return; }
                var newitem = new DesignerItem(n5, parentDesignerItem.ID,
                    new ItemDataBase(n5, parentDesignerItem.ID, GetText(), true, false, 0, double.MaxValue));
                _diagramControl.DesignerItems.Add(newitem);
                SetSelectItem(newitem);
                _diagramControl.GetDataInfo();
            }
        }
        public void Remove()
        {
            var d = GetSelectedItem();
            if (d != null)
            {
                if (d.Data == null) return;
                var item = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == d.ID);
                if (item == null) return;
                _diagramControl.Suppress = true;
                var list = new List<DesignerItem>();
                //删除子节点
                GetAllSubItems(d, list);
                foreach (var designerItem in list)
                {
                    designerItem.Data.Removed = true;
                    designerItem.Visibility = Visibility.Collapsed;
                }
                item.Data.Removed = true;
                item.Visibility = Visibility.Collapsed;
                _diagramControl.Suppress = false;

                #region 移除连线
                var connections = GetItemConnections(d);
                var sink = connections.Where(x => x.Sink.ParentDesignerItem.Equals(d));
                foreach (var connection in sink)
                {
                    _diagramControl.Designer.Children.Remove(connection);
                    connection.Visibility = Visibility.Collapsed;
                }
                if (!_diagramControl.Suppress)
                    BindData();
                if (d.Data.ParentId != Guid.Empty)
                {
                    var parent = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == d.Data.ParentId);
                    SetSelectItem(parent);
                }
                #endregion

                _diagramControl.GetDataInfo();

            }
        }
        public void Copy()
        {
            var selectedItem = GetSelectedItem();
            if (selectedItem != null)
                _diagramControl.Copy = (DesignerItem)selectedItem.Clone();
        }
        public void Paste()
        {
            if (_diagramControl.Copy != null)
            {
                var selectedItem = GetSelectedItem();
                if (selectedItem != null)
                {
                    _diagramControl.Copy.Data.ParentId = selectedItem.ID;
                    _diagramControl.DesignerItems.Add(_diagramControl.Copy);
                    _diagramControl.Copy = null;
                }
            }
        }
        public void Save()
        {
            _diagramControl.Suppress = true;
            _diagramControl.ItemDatas.Clear();
            foreach (var item in _diagramControl.DesignerItems)
            {
                _diagramControl.ItemDatas.Add(item.Data);
            }
            BindData();
            _diagramControl.Suppress = false;
        }
        private string GetText() { return "Item-" + _diagramControl.DesignerItems.Count(); }
        public void Edit(Canvas designer, DesignerItem item)
        {
            TextBox textBox = new TextBox();
            designer.Children.Remove(textBox);
            var left = Canvas.GetLeft(item);
            var top = Canvas.GetTop(item);
            textBox.Text = item.Data.Text;
            textBox.Height = 22;
            textBox.SetValue(Canvas.LeftProperty, left);
            textBox.SetValue(Canvas.TopProperty, top);
            designer.Children.Add(textBox);
            BringToFront(designer, textBox);
            textBox.Focus();
            textBox.SelectAll();
            var t = textBox;
            t.LostFocus += (sender, e) =>
            {
                designer.Children.Remove(textBox);
                item.Data.Text = textBox.Text;
                item.Data.Changed = true;
                SetItemText(item, textBox.Text);
                _diagramControl.GetDataInfo();
            };
        }

        #endregion

        #region 用数据源，构建节点元素
        void InitData/*如果画布无节点则自动添加一个节点*/()
        {
            var id = Guid.NewGuid();
            var newItem = new DesignerItem(id, new ItemDataBase(id, Guid.Empty, GetText(), false, false, 5d, 5d));
            _diagramControl.DesignerItems.Add(newItem);
        }
        private List<DesignerItem> InitDesignerItems()
        {

            var roots = _diagramControl.ItemDatas.Where(x => x.ParentId == Guid.Empty);
            if (roots == null || !roots.Any()) return null;
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
        { return new DesignerItem(itemData.Id, itemData); }
        private DesignerItem CreateChildItem(Guid parentId, ItemDataBase itemData)
        { return new DesignerItem(itemData.Id, parentId, itemData); }

        #endregion
    }
}
