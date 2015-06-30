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

        public static DesignerItem/*根节点*/ GenerateItems(Canvas canvas, IList<DesignerItem> dataSource/*数据源*/)
        {
            if (dataSource.Count == 0) return null;
            var root = dataSource.FirstOrDefault(x => x.Data.ParentId.Equals(Guid.Empty));
            if (root == null) return null;
            IList<DesignerItem> designerItems = new List<DesignerItem>();
            CreateItems(canvas, dataSource, root, designerItems);
            return designerItems.FirstOrDefault(x => x.Data.ParentId.Equals(Guid.Empty));
        }

        public static void ArrangeWithRootItems(Canvas canvas)
        {
            var items = GetDesignerItems(canvas);
            var root = items.FirstOrDefault(x => x.Data.ParentId.Equals(Guid.Empty));
            if (root != null)
            {
                ArrangeWithRootItems(root);
            }

        }

        static void ArrangeWithRootItems/*给定根节点，重新布局*/(DesignerItem rootItem/*根节点*/)
        {
            if (rootItem == null) return;
            var directSubItems = GetDirectSubItems(rootItem);
            if (directSubItems == null) return;
            if (directSubItems.Count == 0) return;
            var rootSubItems =
                directSubItems.Where(x => x.Visibility.Equals(Visibility.Visible)).ToList();
            var parentTop = (double)rootItem.GetValue(Canvas.TopProperty);
            var parentLeft = (double)rootItem.GetValue(Canvas.LeftProperty);
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
                rootSubItems.ElementAt(i).SetValue(Canvas.TopProperty, parentTop + (preChildCount + i + 1) * ChildTopOffset);
                //设置left
                rootSubItems.ElementAt(i).SetValue(Canvas.LeftProperty, parentLeft + GetOffset(rootItem));
                SetItemFontColor(rootSubItems.ElementAt(i), DefaultFontColorBrush);
                ArrangeWithRootItems(rootSubItems.ElementAt(i));
            }
            var designer = rootItem.Parent as Canvas;
            if (designer == null) return;
            SendConnectionsToBack(designer);
        }
        static void ArrangeItems/*拖动其中一个控件，重新布局*/(DesignerItem designerItem)
        {
            var designer = designerItem.Parent as Canvas;
            if (designer == null) return;
            var root = GetRootItem(designer);
            ArrangeWithRootItems(root);
        }
        public static void HighlightParent/*拖动时高亮父节点*/(DesignerItem designerItem, Canvas designer)
        {
            var parent = GetTopSibling(designerItem);
            if (parent == null)
            {
                ShadowChildItemsBrushBorderFontStyle(designerItem);
                return;
            }
            foreach (var item in GetDesignerItems(designer).Where(item => item.IsShadow == false && !item.Equals(designerItem)))
            {
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
            }
            SetItemBorderStyle(parent, HighlightBorderBrush, new Thickness(HighlightBorderThickness), DefaultBackgroundBrush);
            ShadowChildItemsBrushBorderFontStyle(designerItem);
            SetDragItemStyle(designerItem);
        }
        public static void HighlightSelected/*高亮选中*/(DesignerItem item)
        {
            if (item == null) return;
            var canvas = item.Parent as Canvas;
            if (canvas == null) return;
            ResetBrushBorderFontStyle(canvas, item);/*重置所有节点样式*/
            SetItemBorderStyle(item, SelectedBorderBrush, new Thickness(HighlightBorderThickness), HighlightBackgroundBrush);

        }
        public static void HideOrExpandChildItems/*展开折叠*/(DesignerItem item)
        {
            var isExpanded = (bool)item.GetValue(DesignerItem.IsExpandedProperty);
            if (isExpanded == false)/*hide*/
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
            ArrangeItems(item);
        }
        public static DesignerItem GenerateShadow/*拖拽时产生影子*/(DesignerItem designerItem, double originalLeft, double originalTop)
        {
            var canvas = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;
            DesignerItem shadow = null;
            if (canvas != null)
            {
                shadow = CreateShadow(designerItem, originalLeft, originalTop);
                canvas.Children.Add(shadow);
                //隐藏折叠按钮
                designerItem.IsExpanderVisible = false;
            }
            HideItemConnection(designerItem, originalLeft, originalTop);/*拖动时隐藏连线*/
            BringToFront(designerItem);

            return shadow;
        }
        public static void FinishDraging/*改变父节点，移除影子，显示连线，重新布局*/(DesignerItem designerItem, double originalLeft, double originalTop)
        {
            if (designerItem == null) return;
            var canvas = VisualTreeHelper.GetParent(designerItem) as DesignerCanvas;
            if (canvas != null)
            {
                RemoveShadows(canvas);/*移除影子*/

                var left = (double)designerItem.GetValue(Canvas.LeftProperty);
                var top = (double)designerItem.GetValue(Canvas.TopProperty);
                if (!left.Equals(originalLeft) && !top.Equals(originalTop)) /*位置不变，则不改变父节点*/
                {
                    ChangeParent(designerItem);/*改变父节点*/
                }
                ShowItemConnection(designerItem);/*拖动完毕，显示连线*/
                ResetBrushBorderFontStyle(canvas, designerItem);/*恢复边框字体样式*/
                //designerItem.Data.YIndex = (double)designerItem.GetValue(Canvas.TopProperty);
                UpdateYIndex(canvas);
                ArrangeWithRootItems(canvas);/*重新布局*/
            }
        }
        public static void ExpandAll/*展开所有*/(Canvas canvas)
        {
            var items = GetDesignerItems(canvas);
            foreach (var item in items)
            {
                item.IsExpanded = true;
            }
        }
        public static void CollapseAll/*折叠所有，除了根节点*/(Canvas canvas)
        {
            var items = GetDesignerItems(canvas);
            foreach (var item in items.Where(item => item.CanCollapsed))
            {
                item.IsExpanded = false;
            }
        }


        protected class Sibling
        {
            public double Top { get; set; }
            public DesignerItem Item { get; set; }
        }
        protected static DesignerItem GetRootItem/*取得画布上的根节点*/(Canvas canvas)
        {
            return (from designerItem in GetDesignerItems(canvas)
                    let sink = GetItemConnector(designerItem, "Left")
                    where sink != null && sink.Connections.Count == 0
                    select designerItem).FirstOrDefault();
        }
        private static double GetOffset(FrameworkElement item)
        {
            return item.Width.Equals(0) ? 30 : (item.Width * 0.1 + LeftOffset);
        }
        protected static Connector GetItemConnector/*根据名称，取得元素连接点*/(DesignerItem item, string name)
        {
            var itemConnectorDecorator = item.Template.FindName("PART_ConnectorDecorator", item) as Control;
            if (itemConnectorDecorator == null) return null;
            var itemConnector = itemConnectorDecorator.Template.FindName(name, itemConnectorDecorator) as Connector;
            return itemConnector;
        }
        protected static List<Connector> GetItemConnectors/*取得所有连接点*/(DesignerItem item)
        {
            var connectors = new List<Connector>();

            var leftItemConnector = GetItemConnector(item, "Left");
            if (leftItemConnector != null) connectors.Add(leftItemConnector);

            var bottomItemConnector = GetItemConnector(item, "Bottom");
            if (bottomItemConnector != null) connectors.Add(bottomItemConnector);

            var topItemConnector = GetItemConnector(item, "Top");
            if (topItemConnector != null) connectors.Add(topItemConnector);

            var rightItemConnector = GetItemConnector(item, "Right");
            if (rightItemConnector != null) connectors.Add(rightItemConnector);
            return connectors;
        }
        public static IEnumerable<Connection> GetItemConnections/*取得所有连线*/(DesignerItem item)
        {
            var connections = new List<Connection>();
            var list = GetItemConnectors(item);
            if (list.Count == 0) return connections;
            foreach (var c in list.Select(connector => connector.Connections.Where(x => x.Source != null && x.Sink != null)).Where(c => c.Any()))
            {
                connections.AddRange(c);
            }
            return connections;
        }
        protected static Border GetBorder/*元素边框控件*/(DesignerItem item)
        {
            var border = item.Content as Border;
            return border;
            //var parentContent = item.Content as ContentControl;
            //if (parentContent == null) return null;
            //if (parentContent.Content == null) return null;
            //return parentContent.Content as Border;
        }
        protected static TextBlock GetTextBlock/*元素文字控件*/(DesignerItem item)
        {
            var border = GetBorder(item);
            if (border == null) return null;
            return border.Child as TextBlock;
        }
        protected static string GetItemText/*取得元素文字内容*/(DesignerItem item)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return string.Empty;
            return textBlock.GetValue(TextBlock.TextProperty).ToString();
        }
        public static void SetItemText/*设定元素文字*/(DesignerItem item, string text)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return;
            textBlock.SetValue(TextBlock.TextProperty, text);
        }
        protected static void SetItemFontColor/*设定元素文字颜色*/(DesignerItem item, SolidColorBrush fontColorBrush)
        {
            var textBlock = GetTextBlock(item);
            if (textBlock == null) return;
            textBlock.SetValue(TextBlock.ForegroundProperty, fontColorBrush);
        }
        protected static void SetItemBorderStyle/*设定边框样式*/(DesignerItem item, SolidColorBrush borderColor, Thickness borderThickness, SolidColorBrush backgroundbrBrush)
        {
            var border = GetBorder(item);
            if (border == null) return;
            border.BorderBrush = borderColor;
            border.BorderThickness = borderThickness;
            border.Background = backgroundbrBrush;
        }
        protected static DesignerItem CreateDesignerItem/*创建元素*/(Canvas canvas, DesignerItem item, double topOffset, double leftOffset, SolidColorBrush borderBrush = null/*节点边框颜色*/)
        {
            var newItem = item;
            if (newItem.Data == null) return null;
            CreateDesignerItemContent(item, borderBrush);
            item.Width = MinItemWidth;
            newItem.SetValue(Canvas.TopProperty, topOffset);
            newItem.SetValue(Canvas.LeftProperty, leftOffset);
            if (!newItem.Data.Removed)
            {
                canvas.Children.Add(newItem);
            }
            canvas.Measure(Size.Empty);
            return newItem;
        }
        protected static void CreateDesignerItemContent/*创建元素内容，固定结构*/(DesignerItem item, SolidColorBrush borderBrush = null)
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
            //var contentControl = new ContentControl
            //{
            //    Content = border,
            //    IsHitTestVisible = false,
            //    HorizontalAlignment = HorizontalAlignment.Stretch
            //};
            item.Content = border;// contentControl;
        }

        public static List<DesignerItem> GetDesignerItems/*取得画布所有元素*/(Canvas canvas)
        {
            var list = new List<DesignerItem>();

            var itemCount = VisualTreeHelper.GetChildrenCount(canvas);
            if (itemCount == 0) return null;
            for (int n = 0; n < itemCount; n++)
            {
                var c = VisualTreeHelper.GetChild(canvas, n);
                var child = c as DesignerItem;
                if (child != null) list.Add(child);
            }
            return list;
        }
        protected static List<DesignerItem> GetDirectSubItems/*取得直接子节点*/(DesignerItem item)
        {
            var canvas = item.Parent as Canvas;
            if (canvas == null) return null;
            var connections = GetItemConnections(item);
            var list = (from itemConnection in connections
                        where Equals(itemConnection.Source.ParentDesignerItem, item)
                        && itemConnection.Source != null && itemConnection.Sink != null
                        select itemConnection.Sink.ParentDesignerItem).OrderBy(x => x.Data.YIndex).ToList();
            item.IsExpanderVisible = list.Any();
            if (item.Equals(GetRootItem(canvas)))
                item.IsExpanderVisible = false;
            return list;
        }
        public static void GetAllSubItems/*取得直接及间接的子节点*/(DesignerItem item/*某个节点*/, List<DesignerItem> subitems/*其所有子节点*/)
        {
            var canvas = item.Parent as Canvas;
            if (canvas == null) return;
            var child = new List<DesignerItem>();
            foreach (var subItem in
                (from itemConnection in GetItemConnections(item)
                 where Equals(itemConnection.Source.ParentDesignerItem, item)
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
        protected static DesignerItem GetParentItem/*父节点*/(DesignerItem item)
        {
            var connector = GetItemConnector(item, "Left");
            var connection = connector.Connections.FirstOrDefault(x => x.Sink != null && x.Source != null);
            if (connection != null)
            {
                return connection.Source.ParentDesignerItem;
            }
            return null;
        }
        public static void BringToFront/*将制定元素移到最前面*/(DesignerItem designerItem)
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
        public static void BringToFront/*将制定元素移到最前面*/(Canvas canvas, UIElement element)
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
        public static void UpdateYIndex/*按照Top位置排序*/(Canvas canvas)
        {
            List<DesignerItem> designerItems = new List<DesignerItem>();
            GetAllSubItems(GetRootItem(canvas), designerItems);
            foreach (var item in designerItems)
            {
                item.Data.YIndex = Canvas.GetTop(item);
            }
        }
        public static void UpdateYIndex/*按照Top位置排序*/(DesignerItem designerItem)
        {
            var canvas = designerItem.Parent as Canvas;
            if (canvas == null) return;
            List<DesignerItem> designerItems = new List<DesignerItem>();
            GetAllSubItems(GetRootItem(canvas), designerItems);
            foreach (var item in designerItems)
            {
                item.Data.YIndex = Canvas.GetTop(item);
            }
        }



        protected static void SendConnectionsToBack(Canvas canvas)
        {
            var childrens = canvas.Children;
            var connectionList = childrens.OfType<Connection>().ToList();
            foreach (var uiElement in connectionList)
            {
                Panel.SetZIndex(uiElement, -10000);
            }
        }
        private static void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer, DesignerItem selectedItem)
        {
            foreach (var item in GetDesignerItems(designer).Where(item => !item.Equals(selectedItem)))
            {
                if (item.IsShadow) continue;
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }
        public static void ResetBrushBorderFontStyle/*恢复所有元素边框样式*/(Canvas designer)
        {
            foreach (var item in GetDesignerItems(designer))
            {
                if (item.IsShadow) continue;
                SetItemBorderStyle(item, DefaultBorderBrush, new Thickness(DefaultBorderThickness), DefaultBackgroundBrush);
                SetItemFontColor(item, DefaultFontColorBrush);
            }
        }

        protected static DesignerItem GetTopSibling/*取得元素上方最接近的元素*/(DesignerItem item)
        {
            var canvas = item.Parent as Canvas;
            if (canvas == null) return null;

            //取得所有子节点，让parent不能为子节点
            var subitems = new List<DesignerItem>();
            GetAllSubItems(item, subitems);

            var topPosition = (double)item.GetValue(Canvas.TopProperty);/*鼠标拖动后的位置*/
            var leftPosition = (double)item.GetValue(Canvas.LeftProperty);/*鼠标拖动后的位置*/


            var pre = GetDesignerItems(canvas).Where(x => x.Visibility.Equals(Visibility.Visible));
            var list = (from designerItem in pre
                        let top = (double)designerItem.GetValue(Canvas.TopProperty)
                        let left = (double)designerItem.GetValue(Canvas.LeftProperty)
                        where top <= topPosition /*top位置小于自己的top位置*/
                        && left <= leftPosition
                        && !Equals(designerItem, item) /*让parent不能为自己*/
                        && !subitems.Contains(designerItem) /*让parent不能为子节点*/
                        && designerItem.IsShadow == false
                        select new Sibling() { Top = top, Item = designerItem }).ToList();

            if (!list.Any()) return null;
            var parent = list.OrderByDescending(x => x.Top).First();
            return parent.Item;
        }
        protected static void ChangeParent(DesignerItem item)
        {
            var canvas = item.Parent as Canvas;
            if (canvas == null) return;

            //找到上方最接近的节点，取得其下方的连接点

            var parent = GetTopSibling(item);
            if (parent != null)
            {
                var source = GetItemConnector(parent, "Bottom"); ;
                if (source != null)
                {
                    //如果父节点折叠，则展开它
                    if (source.ParentDesignerItem.IsExpanded == false)
                    {
                        source.ParentDesignerItem.IsExpanded = true;
                    }
                    // 取得当前节点的连接线
                    var sink = GetItemConnector(item, "Left"); ;
                    if (sink == null) return;
                    if (source.Equals(sink)) return;
                    var connections = sink.Connections.Where(x => x.Source != null && x.Sink != null).ToList();
                    if (!connections.Any()) return;
                    var connection = connections.First();
                    connection.Source = source;
                    item.Data.ParentId = parent.ID;
                }
            }
        }
        protected static void HideItemConnection/*拖动元素，隐藏元素连线*/(DesignerItem item, double left, double top)
        {
            var itemTop = (double)item.GetValue(Canvas.TopProperty);
            var itemLeft = (double)item.GetValue(Canvas.LeftProperty);
            if (itemTop.Equals(top) && itemLeft.Equals(left)) return;

            foreach (var connection in GetItemConnections(item))
            {
                connection.Visibility = Visibility.Hidden;
            }

        }
        protected static void ShowItemConnection/*元素所有连线恢复显示*/(DesignerItem item)
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
        protected static void RemoveShadows(Canvas canvas)
        {
            var list = GetDesignerItems(canvas).Where(x => x.IsShadow);
            foreach (var designerItem in list)
            {
                canvas.Children.Remove(designerItem);
            }
        }
        protected static void SetDragItemStyle(DesignerItem item)
        {
            SetItemBorderStyle(item, ShadowBackgroundBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
            SetItemFontColor(item, ShadowFontColorBrush);
        }
        protected static DesignerItem CreateShadow/*拖动时创建的影子*/(DesignerItem item, double left, double top)
        {
            var copy = new DesignerItem { IsExpanderVisible = false, IsShadow = true };
            CreateDesignerItemContent(copy);
            SetItemText(copy, GetItemText(item));
            SetItemBorderStyle(copy, ShadowBorderBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
            SetItemFontColor(copy, ShadowFontColorBrush);
            copy.SetValue(Canvas.LeftProperty, left);
            copy.SetValue(Canvas.TopProperty, top);
            copy.Width = item.Width;
            Panel.SetZIndex(copy, -100);
            return copy;
        }
        protected static void ShadowChildItemsBrushBorderFontStyle/*拖拽时，子元素变灰色*/(DesignerItem item)
        {
            List<DesignerItem> subItems = new List<DesignerItem>();
            GetAllSubItems(item, subItems);
            foreach (var designerItem in subItems)
            {
                SetItemBorderStyle(designerItem, ShadowBackgroundBrush, new Thickness(DefaultBorderThickness), ShadowBackgroundBrush);
                SetItemFontColor(designerItem, ShadowFontColorBrush);
            }
        }
        protected static void CreateItems(Canvas canvas, IList<DesignerItem> dataSource, DesignerItem parentItem, IList<DesignerItem> designerItems)
        {
            if (parentItem == null) return;
            if (designerItems == null) return;
            DesignerItem parentDesignerItem = null;

            if (designerItems.All(x => !x.ID.Equals(parentItem.ID)))
            {
                if (parentItem.Data.ParentId.Equals(Guid.Empty)) //是根节点？
                {
                    parentDesignerItem = CreateRoot(canvas, parentItem, 5d, 5d);
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

            var childs = dataSource.Where(x => x.Data.ParentId.Equals(parentItem.ID));

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
                CreateItems(canvas, dataSource, childItem, designerItems);
            }
        }
        protected static DesignerItem CreateRoot/*创建根节点*/(Canvas canvas, DesignerItem item, double topOffset, double leftOffset)
        {
            var allItems = GetDesignerItems(canvas);
            if (allItems == null)
            {
                // 检查Canvas中是否已经有内容
                var root = CreateDesignerItem(canvas, item, topOffset, leftOffset);
                SetItemFontColor(root, DefaultFontColorBrush);
                root.CanCollapsed = false;
                root.IsExpanderVisible = false;
                return root; // 没有节点时才能创建根节点
            }
            return null;
        }
        protected static DesignerItem CreateChild(DesignerItem parent, DesignerItem childItem)
        {
            if (parent == null) return null;

            var canvas = parent.Parent as Canvas;
            if (canvas == null) return null;

            #region 起点 Connector

            var parentConnectorDecorator = parent.Template.FindName("PART_ConnectorDecorator", parent) as Control;
            if (parentConnectorDecorator == null) return null;
            var source = parentConnectorDecorator.Template.FindName("Bottom", parentConnectorDecorator) as Connector;

            #endregion

            #region 终点 Connector

            var child = CreateDesignerItem(
                canvas,
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
                    canvas.Children.Add(conn); /*放到画布上*/
                }
            }
            else if (c.Count == 1)
            {
                var cn = c.FirstOrDefault();
                if (cn != null)
                {
                    if (!childItem.Data.Removed)
                    {
                        canvas.Children.Add(cn);
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
                        canvas.Children.Add(conn); /*放到画布上*/
                    }
                }
            }
            #endregion

            child.CanCollapsed = true;
            return child;/*返回创建的子节点*/
        }



        public static void Edit(Canvas designer, DesignerItem item, TextBox textBox)
        {
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
        }
    }
}
