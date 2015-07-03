using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using System;
using System.ComponentModel;
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
        public double oldx;
        public double oldy;
        private ItemDataBase _data;
        public ItemDataBase Data
        {
            get { return _data; }
            set
            {
                if (_data != value)
                {
                    _data = value;

                }
            }
        }

        #region ID
        private Guid id;
        public Guid ID
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        #region ParentID
        //分组时用的，并不是表示父节点ID
        public Guid ParentID
        {
            get { return (Guid)GetValue(ParentIDProperty); }
            set { SetValue(ParentIDProperty, value); }
        }
        public static readonly DependencyProperty ParentIDProperty = DependencyProperty.Register("ParentID", typeof(Guid), typeof(DesignerItem));
        #endregion

        #region IsGroup
        public bool IsGroup
        {
            get { return (bool)GetValue(IsGroupProperty); }
            set { SetValue(IsGroupProperty, value); }
        }
        public static readonly DependencyProperty IsGroupProperty =
            DependencyProperty.Register("IsGroup", typeof(bool), typeof(DesignerItem));
        #endregion

        #region IsSelected Property

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

        #region DragThumbTemplate Property

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

        #region ConnectorDecoratorTemplate Property

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

        #region IsDragConnectionOver

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

        //是否展开
        #region IsExpanded

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

        //折叠按钮是否显示
        #region IsExpanderVisible

        public static readonly DependencyProperty IsExpanderVisibleProperty = DependencyProperty.Register(
            "IsExpanderVisible", typeof(bool), typeof(DesignerItem), new FrameworkPropertyMetadata(false));

        public bool IsExpanderVisible
        {
            get { return (bool)GetValue(IsExpanderVisibleProperty); }
            set { SetValue(IsExpanderVisibleProperty, value); }
        }

        #endregion

        //是否可以折叠
        #region Collapsable

        public static readonly DependencyProperty CanCollapsedProperty = DependencyProperty.Register(
            "CanCollapsed", typeof(bool), typeof(DesignerItem), new FrameworkPropertyMetadata(true));

        public bool CanCollapsed
        {
            get { return (bool)GetValue(CanCollapsedProperty); }
            set { SetValue(CanCollapsedProperty, value); }
        }

        #endregion

        public bool IsShadow { get; set; }
        static DesignerItem()
        {
            // set the key to reference the style for this control
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DesignerItem), new FrameworkPropertyMetadata(typeof(DesignerItem)));
        }
        private DiagramControl _diagramControl;
        public DiagramControl DiagramControl
        {
            set { _diagramControl = value; }
            get
            {
                _diagramControl = GetDiagramControl();
                return _diagramControl;
            }
        }
        private DiagramControl GetDiagramControl()/*父控件*/
        {
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;
            if (designer != null)
            {
                var diagramControl = designer.TemplatedParent as DiagramControl;
                return diagramControl;
            }
            return null;
        }
        public DesignerItem(Guid id)
        {
            this.id = id;
            Loaded += DesignerItem_Loaded;
            Data = new CustomItemData(id);
        }
        public DesignerItem() : this(Guid.NewGuid()) { }
        public DesignerItem(Guid id, ItemDataBase itemData) : this(id) { Data = itemData; }
        public DesignerItem(Guid id, Guid parentItemId, ItemDataBase itemData) : this(id) { Data.ParentId = parentItemId; Data = itemData; }
        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            //base.OnPreviewMouseDown(e);
            DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;
            // update selection
            if (designer != null)
            {
                if ((Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Control)) != ModifierKeys.None)
                    if (IsSelected)
                    {
                        designer.SelectionService.RemoveFromSelection(this);
                    }
                    else
                    {
                        designer.SelectionService.AddToSelection(this);
                    }
                else if (!IsSelected)
                {
                    designer.SelectionService.SelectItem(this);
                }
                Focus();
                //if (DiagramControl != null) { DiagramControl.SelectedItem = this; }
            }
            e.Handled = false;
        }
        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            //DiagramManager.HighlightSelected(this);

            //DesignerCanvas designer = VisualTreeHelper.GetParent(this) as DesignerCanvas;
            //if (designer != null)
            //{
            //    MessageBox.Show((designer.SelectionService.CurrentSelection.Count).ToString());
            //}
        }
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
                            ControlTemplate template =
                                GetDragThumbTemplate(contentVisual);
                            if (template != null)
                                thumb.Template = template;
                        }
                    }
                }
            }
        }
        public object Clone()
        {
            var data = (ItemDataBase)Data.Clone();
            return new DesignerItem(data.Id) { Data = data, DiagramControl = GetDiagramControl() };
        }
    }
}
