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

        public DiagramManager(DiagramControl diagramControl)
        {
            _diagramControl = diagramControl;
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
        private static readonly SolidColorBrush HighlightBackgroundBrush = Brushes.LightSkyBlue;
        private static readonly SolidColorBrush DefaultBackgroundBrush = Brushes.GhostWhite;
        static readonly SolidColorBrush ShadowBorderBrush = Brushes.LightGray;
        private static readonly SolidColorBrush ShadowBackgroundBrush = Brushes.LightGray;
        private static readonly SolidColorBrush ShadowFontColorBrush = Brushes.Gray;
        private static readonly SolidColorBrush DefaultFontColorBrush = Brushes.Black;
        private const string ParentConnector = "Bottom";

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
            _diagramControl.Designer.SelectionService.ClearSelection();
            _diagramControl.Designer.SelectionService.SelectItem(designerItem);
        }

        #endregion

        #region Create
        public List<DesignerItem>/*根节点*/ GenerateItems()
        {
            _diagramControl.Designer.Children.Clear();
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

            var source = GetItemConnector(parent, ParentConnector);
            var sink = GetItemConnector(child, "Left");
            if (source == null || sink == null) return null;
            #region 创建连线
            var connections = GetItemConnections(parent).Where(connection
                => connection.Source.Equals(source)
                && connection.Sink.Equals(sink)).ToList();
            if (connections.Count == 0 || connections.FirstOrDefault() == null)
            {
                var conn = new Connection(source, sink); /*创建连线*/
                _diagramControl.Designer.Children.Add(conn); /*放到画布上*/
                Panel.SetZIndex(conn, -10000);
            }
            #endregion

            child.CanCollapsed = true;
            return child;/*返回创建的子节点*/
        }
        private DesignerItem CreateDesignerItem/*创建元素*/(DesignerItem item, double topOffset = 0d, double leftOffset = 0d, SolidColorBrush borderBrush = null/*节点边框颜色*/)
        {
            if (item.Data == null) return null;
            CreateDesignerItemContent(item, DefaultFontColorBrush, borderBrush);
            if (!item.Data.Removed)
            {
                _diagramControl.Designer.Children.Add(item);
                _diagramControl.Designer.Measure(Size.Empty);
                Canvas.SetTop(item, topOffset);
                Canvas.SetLeft(item, leftOffset);
            }
            return item;
        }
        private void CreateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush fontColorBrush, SolidColorBrush borderBrush = null)
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
                FontSize = 12d,
                Foreground = fontColorBrush
            };
            border.Child = textblock;
            item.Content = border;
            item.Width = MinItemWidth;
        }
        public void BindData/*将DesignerItems放到画布上，并且创建连线*/()
        {
            var designerItems = GenerateItems();
            if (designerItems == null) return;
            if (!designerItems.Any()) return;
            ArrangeWithRootItems();
            designerItems.ForEach(x =>
            {
                x.Data.DiagramControl = _diagramControl;
                x.MouseDoubleClick += (sender, e) =>
                {
                    SetSelectItem(x);
                    Edit(x);
                };
            });
        }

        public void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/(Guid id)
        {
            //_diagramControl.Suppress = true;/*利用数据源创建节点时不执行CollectionChange事件*/
            var roots = InitDesignerItems();
            if (roots == null) return;
            if (!roots.Any()) return;/*创建DesignerItems*/
            BindData();/*将DesignerItems放到画布上，并且创建连线*/
            //_diagramControl.Suppress = false;
            if (id != Guid.Empty)
                _diagramControl.DiagramManager.SetSelectItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ID == id));
            _diagramControl.GetDataInfo();
        }


        #endregion

        #region Arrange

        public void ArrangeWithRootItems()
        {
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

            foreach (var item in _diagramControl.DesignerItems
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
            var parent = GetNewParentdDesignerItem(designerItem);
            if (parent != null)
            {
                SetItemBorderStyle(parent, HighlightBorderBrush, new Thickness(HighlightBorderThickness),
                    DefaultBackgroundBrush);
                //SetBottomItemPosition(parent);
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

        public void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer)
        {
            foreach (var item in _diagramControl.DesignerItems.Where(item => !item.IsShadow))
            {
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }
        public void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer, DesignerItem designerItem)
        {
            foreach (var item in _diagramControl.DesignerItems.Where(item => !item.IsShadow && item != designerItem))
            {
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }

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
        private void BringToFront/*将制定元素移到最前面*/(UIElement element)
        {
            List<UIElement> childrenSorted =
                (from UIElement item in _diagramControl.Designer.Children
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
        private DesignerItem GetNewParentdDesignerItem/*取得元素上方最接近的元素*/(DesignerItem item)
        {
            //取得所有子节点，让parent不能为子节点
            var subitems = new List<DesignerItem>();
            GetAllSubItems(item, subitems);

            var pre = _diagramControl.DesignerItems.Where(x => x.Visibility.Equals(Visibility.Visible));
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
            var parent = list.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);
            return parent;
        }
        private void ChangeParent(DesignerItem item)
        {
            //找到上方最接近的节点，取得其下方的连接点
            if (item.Data.XIndex.Equals(item.oldx) && item.Data.YIndex.Equals(item.oldy)) return;
            var oldParent = _diagramControl.DesignerItems.Where(x => x.Data.Id == item.Data.ParentId).ToList();
            var parent = GetNewParentdDesignerItem(item);

            if (parent != null)
            {
                item.Data.ParentId = parent.ID;
                var oldConnections = GetItemConnections(item).ToList();
                if (oldConnections.Count() != 0 && oldParent.Count() != 0)
                {

                    foreach (var designerItem in oldParent)
                    {
                        if (designerItem != null)
                        {
                            var oldConnector = GetItemConnector(designerItem, ParentConnector);
                            if (oldConnector != null)
                            {
                                var connections = oldConnections
                                    .Where(x => Equals(x.Source, oldConnector))
                                    .ToList();
                                foreach (var connection in connections)
                                {
                                    if (connection != null)
                                    {
                                        connection.Source = GetItemConnector(parent, ParentConnector);
                                        _diagramControl.Designer.Children.Remove(connection);
                                        _diagramControl.Designer.Children.Add(connection);
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    var source = GetItemConnector(parent, ParentConnector);
                    var sink = GetItemConnector(item, "Left");
                    var connection = new Connection(source, sink);
                    _diagramControl.Designer.Children.Remove(connection);
                    _diagramControl.Designer.Children.Add(connection);
                }

            }
            else
            {
                item.Data.ParentId = Guid.Empty;

                if (oldParent.Count != 0)
                {
                    var connections =
                    GetItemConnections(item)
                        .Where(x => Equals(x.Source, GetItemConnector(oldParent.FirstOrDefault(), ParentConnector)))
                        .ToList();
                    foreach (var connection in connections)
                    {
                        foreach (var designerItem in oldParent)
                        {
                            var s = GetItemConnector(designerItem, ParentConnector);
                            s.Connections.Remove(connection);
                        }
                        connection.Source.Connections.Remove(connection);
                        connection.Sink.Connections.Remove(connection);
                        _diagramControl.Designer.Children.Remove(connection);
                    }
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
        public DesignerItem CreateShadows/*拖拽时产生影子*/(DesignerItem designerItem)
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

            return shadow;
        }
        private DesignerItem CreateShadow/*拖动时创建的影子*/(DesignerItem item)
        {
            var copy = new DesignerItem(_diagramControl) { IsExpanderVisible = false, IsShadow = true };
            CreateDesignerItemContent(copy, ShadowFontColorBrush);
            SetItemText(copy, GetItemText(item));
            SetItemBorderStyle(copy, ShadowBorderBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
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

        #endregion

        #region Item Operation

        // private string GetText() { return "Item-" + _diagramControl.DesignerItems.Count(); }
        public void Edit(DesignerItem item)
        {
            TextBox textBox = new TextBox();
            _diagramControl.Designer.Children.Remove(textBox);
            var left = Canvas.GetLeft(item);
            var top = Canvas.GetTop(item);
            textBox.Text = item.Data.Text;
            textBox.Height = 22;
            textBox.SetValue(Canvas.LeftProperty, left);
            textBox.SetValue(Canvas.TopProperty, top);
            _diagramControl.Designer.Children.Add(textBox);
            BringToFront(textBox);
            textBox.Focus();
            textBox.SelectAll();
            var t = textBox;
            t.LostFocus += (sender, e) =>
            {
                _diagramControl.Designer.Children.Remove(textBox);
                item.Data.Text = textBox.Text;
                item.Data.Changed = true;
                SetItemText(item, textBox.Text);
                _diagramControl.GetDataInfo();
            };
        }
        #region Remove
        public void RemoveDesignerItem(ItemDataBase itemDataBase)
        {
            var child = GetAllChildItemDataBase(itemDataBase.Id);
            _diagramControl.RemovedItemDataBase.AddRange(child);
            _diagramControl.RemovedItemDataBase.Add(itemDataBase);
            _diagramControl.RemovedItemDataBase.ForEach(x => { x.Removed = true; });
            child.ForEach(c =>
            {
                RemoveItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ID == c.Id));
            });
            RemoveItem(_diagramControl.DesignerItems.FirstOrDefault(x => x.ID == itemDataBase.Id));

            ArrangeWithRootItems();

            var c1 = _diagramControl.DesignerItems.Where(x => x.Data.ParentId == itemDataBase.ParentId).ToList();
            DesignerItem selectedDesignerItem = null;
            if (c1.Any())
            {
                selectedDesignerItem = c1.Aggregate((a, b) => a.Data.YIndex > b.Data.YIndex ? a : b);

            }
            else
            {
                selectedDesignerItem = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == itemDataBase.ParentId);

            }
            _diagramControl.DiagramManager.SetSelectItem(selectedDesignerItem);
            Scroll(selectedDesignerItem);
        }



        void RemoveItem(DesignerItem item)
        {
            var connections = GetItemConnections(item).ToList();
            connections.ForEach(x => { _diagramControl.Designer.Children.Remove(x); });
            var connectors = GetItemConnectors(item);
            connectors.ForEach(x => { x.Connections.Clear(); });
            _diagramControl.Designer.Children.Remove(item);
            _diagramControl.DesignerItems.Remove(item);
        }
        #endregion
        #region Add

        public void AddDesignerItem(ItemDataBase item)
        {
            var parentDesignerItem = _diagramControl.DesignerItems.FirstOrDefault(x => x.ID == item.ParentId);
            var designerItem = new DesignerItem(item.Id, item.ParentId, item, _diagramControl);
            _diagramControl.DesignerItems.Add(designerItem);
            CreateChild(parentDesignerItem, designerItem);
            ArrangeWithRootItems();
            SetSelectItem(designerItem);
            Scroll(designerItem);
        }
        #endregion
        public List<ItemDataBase> GetAllChildItemDataBase(Guid id)
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

        void Scroll(DesignerItem designerItem)
        {
            var sv = (ScrollViewer)_diagramControl.Template.FindName("DesignerScrollViewer", _diagramControl);
            sv.ScrollToVerticalOffset(Canvas.GetTop(designerItem));
            sv.ScrollToHorizontalOffset(Canvas.GetLeft(designerItem));
        }
    }
}