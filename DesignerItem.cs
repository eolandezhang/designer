using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner
{
    //These attributes identify the types of the named parts that are used for templating
    [TemplatePart(Name = "PART_DragThumb", Type = typeof(DragThumb))]
    [TemplatePart(Name = "PART_ResizeDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ConnectorDecorator", Type = typeof(Control))]
    [TemplatePart(Name = "PART_ContentPresenter", Type = typeof(ContentPresenter))]
    public class DesignerItem : ContentControl, ISelectable, IGroupable, ICloneable
    {
        #region Fields
        #region 位置
        public double Oldx;/*记录拖拽前的位置*/
        public double Oldy;
        #endregion
        public DesignerItem ShadowOrignal;/*当此节点为shadow时，记录shadow的原节点*/
        public DiagramControl DiagramControl;
        #endregion

        #region Property
        public string ItemId { get; set; }
        public string ItemParentId { get; set; }
        //public Guid ID { get; set; }
        public ItemDataBase Data { get; set; }/*存放数据*/
        #region IsSelected Property 被选中的
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty =
          DependencyProperty.Register("IsSelected",
                                       typeof(bool),
                                       typeof(DesignerItem),
                                       new FrameworkPropertyMetadata(false));
        #endregion
        #region IsExpanded Property 是否展开

        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded", typeof(bool), typeof(DesignerItem),
            new PropertyMetadata(true, (d, e) =>
            {
                var designerItem = d as DesignerItem;
                if (designerItem != null)
                {
                    var canvas = designerItem.Parent as DesignerCanvas;
                    if (canvas != null)
                    {
                        var diagramControl = canvas.TemplatedParent as DiagramControl;
                        if (diagramControl != null)
                            diagramControl.DiagramManager.HideOrExpandChildItems(designerItem);
                    }
                }
            }));


        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        #endregion
        #region IsExpanderVisible Property 折叠按钮是否显示

        public static readonly DependencyProperty IsExpanderVisibleProperty = DependencyProperty.Register(
            "IsExpanderVisible", typeof(bool), typeof(DesignerItem), new FrameworkPropertyMetadata(false));

        public bool IsExpanderVisible
        {
            get { return (bool)GetValue(IsExpanderVisibleProperty); }
            set { SetValue(IsExpanderVisibleProperty, value); }
        }

        #endregion
        #region CanCollapsed Property 是否可以折叠

        public static readonly DependencyProperty CanCollapsedProperty = DependencyProperty.Register(
            "CanCollapsed", typeof(bool), typeof(DesignerItem), new FrameworkPropertyMetadata(true));

        public bool CanCollapsed
        {
            get { return (bool)GetValue(CanCollapsedProperty); }
            set { SetValue(CanCollapsedProperty, value); }
        }

        #endregion
        #region IsNewParent Property 是否是新父节点

        public static readonly DependencyProperty IsNewParentProperty = DependencyProperty.Register(
            "IsNewParent", typeof(bool), typeof(DesignerItem), new FrameworkPropertyMetadata(false));

        public bool IsNewParent
        {
            get { return (bool)GetValue(IsNewParentProperty); }
            set { SetValue(IsNewParentProperty, value); }
        }

        #endregion
        #region ItemContextMenu Property 右键菜单
        public static readonly DependencyProperty ItemContextMenuProperty =
            DependencyProperty.RegisterAttached("ItemContextMenu", typeof(ContextMenu), typeof(DesignerItem));

        public static ContextMenu GetItemContextMenu(UIElement element)
        {
            return (ContextMenu)element.GetValue(ItemContextMenuProperty);
        }

        public static void SetItemContextMenu(UIElement element, ContextMenu value)
        {
            element.SetValue(ItemContextMenuProperty, value);
        }
        #endregion
        #region IsShadow Property 标识是否是拖拽阴影

        public static readonly DependencyProperty IsShadowProperty = DependencyProperty.Register(
            "IsShadow", typeof(bool), typeof(DesignerItem), new PropertyMetadata(false));

        public bool IsShadow
        {
            get { return (bool)GetValue(IsShadowProperty); }
            set { SetValue(IsShadowProperty, value); }
        }

        #endregion
        #region IsDragItemChild 拖拽元素的子元素，灰色边框样式

        public static readonly DependencyProperty IsDragItemChildProperty = DependencyProperty.Register(
            "IsDragItemChild", typeof(bool), typeof(DesignerItem), new PropertyMetadata(default(bool)));

        public bool IsDragItemChild
        {
            get { return (bool)GetValue(IsDragItemChildProperty); }
            set { SetValue(IsDragItemChildProperty, value); }
        }
        #endregion
        #region 树状图，不使用的属性
        #region ParentID Property 分组时用的，并不是表示父节点ID
       
        public string ParentID
                {
                    get { return (string)GetValue(ParentIDProperty); }
                    set { SetValue(ParentIDProperty, value); }
                }
        public static readonly DependencyProperty ParentIDProperty = DependencyProperty.Register("ParentID", typeof(string), typeof(DesignerItem));
        #endregion
        #region IsGroup Property 分组
        public bool IsGroup
        {
            get { return (bool)GetValue(IsGroupProperty); }
            set { SetValue(IsGroupProperty, value); }
        }
        public static readonly DependencyProperty IsGroupProperty =
            DependencyProperty.Register("IsGroup", typeof(bool), typeof(DesignerItem));
        #endregion
        #region DragThumbTemplate Property 拖拽模板

        // can be used to replace the default template for the DragThumb
        public static readonly DependencyProperty DragThumbTemplateProperty =
            DependencyProperty.RegisterAttached("DragThumbTemplate", typeof(ControlTemplate), typeof(DesignerItem));

        public static ControlTemplate GetDragThumbTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(DragThumbTemplateProperty);
        }

        public static void SetDragThumbTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(DragThumbTemplateProperty, value);
        }

        #endregion
        #region ConnectorDecoratorTemplate Property 连接点模板

        // can be used to replace the default template for the ConnectorDecorator
        public static readonly DependencyProperty ConnectorDecoratorTemplateProperty =
            DependencyProperty.RegisterAttached("ConnectorDecoratorTemplate", typeof(ControlTemplate), typeof(DesignerItem));

        public static ControlTemplate GetConnectorDecoratorTemplate(UIElement element)
        {
            return (ControlTemplate)element.GetValue(ConnectorDecoratorTemplateProperty);
        }

        public static void SetConnectorDecoratorTemplate(UIElement element, ControlTemplate value)
        {
            element.SetValue(ConnectorDecoratorTemplateProperty, value);
        }

        #endregion
        #region IsDragConnectionOver Property

        // while drag connection procedure is ongoing and the mouse moves over 
        // this item this value is true; if true the ConnectorDecorator is triggered
        // to be visible, see template
        public bool IsDragConnectionOver
        {
            get { return (bool)GetValue(IsDragConnectionOverProperty); }
            set { SetValue(IsDragConnectionOverProperty, value); }
        }
        public static readonly DependencyProperty IsDragConnectionOverProperty =
            DependencyProperty.Register("IsDragConnectionOver",
                                         typeof(bool),
                                         typeof(DesignerItem),
                                         new FrameworkPropertyMetadata(false));

        #endregion
        #endregion
        #endregion

        #region Constructors
        public DesignerItem(string id, DiagramControl diagramControl)
        {
            this.ItemId = id;
            ItemParentId = string.Empty;
            Loaded += DesignerItem_Loaded;
            Data = new CustomItemData(id);
            Data.DiagramControl = diagramControl;
            DiagramControl = diagramControl;
            Focusable = false;
            MouseDoubleClick += (sender, e) =>
             {
                 diagramControl.DiagramManager.Edit(this);
             };

        }
        public DesignerItem(DiagramControl diagramControl)
            : this(Guid.NewGuid().ToString(), diagramControl) { }
        public DesignerItem(string id, ItemDataBase itemData, DiagramControl diagramControl)
            : this(id, diagramControl)
        {
            Data = itemData;
            itemData.DiagramControl = diagramControl;
        }
        public DesignerItem(string id, string parentItemId, ItemDataBase itemData, DiagramControl diagramControl)
            : this(id, diagramControl)
        {
            ItemParentId = parentItemId;
            Data.ItemParentId = parentItemId; Data = itemData;
            itemData.DiagramControl = diagramControl;
        }
        static DesignerItem()
        {
            // set the key to reference the style for this control
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DesignerItem), new FrameworkPropertyMetadata(typeof(DesignerItem)));
        }
        #endregion

        #region override
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            //base.OnPreviewMouseDown(e);
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;
            // update selection
            if (designer != null)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                {
                    if (IsSelected)
                    {
                        designer.SelectionService.RemoveFromSelection(this);

                        DiagramControl.SelectedItem = null;
                        DiagramControl.SelectedItems.Remove(this);
                    }
                    else
                    {
                        designer.SelectionService.AddToSelection(this);
                        DiagramControl.SelectedItem = this;
                        DiagramControl.SelectedItems.Add(this);
                    }
                }
                else if (!IsSelected)
                {
                    designer.SelectionService.SelectItem(this);

                    DiagramControl.SelectedItem = this;
                    DiagramControl.SelectedItems.Add(this);
                }
                Focus();
            }
            e.Handled = false;



        }
        #endregion

        void DesignerItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (Template != null)
            {
                ContentPresenter contentPresenter =
                    Template.FindName("PART_ContentPresenter", this) as ContentPresenter;
                if (contentPresenter != null)
                {
                    UIElement contentVisual = VisualTreeHelper.GetChild(contentPresenter, 0) as UIElement;
                    if (contentVisual != null)
                    {
                        DragThumb thumb = this.Template.FindName("PART_DragThumb", this) as DragThumb;
                        if (thumb != null)
                        {
                            ControlTemplate template = GetDragThumbTemplate(contentVisual);
                            if (template != null) thumb.Template = template;
                        }
                    }
                }
            }
        }

        public void SetTemplate()
        {
            if (Template != null)
            {
                ContentPresenter contentPresenter =
                    Template.FindName("PART_ContentPresenter", this) as ContentPresenter;
                if (contentPresenter != null)
                {
                    if (contentPresenter.ContentTemplate == null)
                    {
                        contentPresenter.ContentTemplate = DiagramControl.GetDesignerItemTemplate(this.DiagramControl);
                        UpdateLayout();
                    }
                }
            }
        }

        public object Clone()
        {
            var data = (ItemDataBase)Data.Clone();
            return new DesignerItem(data.ItemId, DiagramControl) { Data = data, DiagramControl = DiagramControl };
        }
    }
}
