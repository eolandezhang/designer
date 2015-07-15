using DiagramDesigner.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace DiagramDesigner.Controls
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var designer = (DesignerCanvas)GetTemplateChild("Designer");
            if (designer != null)
            {
                DesignerCanvas = designer;
            }
            var diagramHeader = (GroupBox)GetTemplateChild("DiagramHeader");
            if (diagramHeader != null) diagramHeader.Header = DiagramHeader;

        }

        #endregion

        #region Fields & Properties

        //public readonly ObservableCollection<DesignerItem> DesignerItems; /*节点元素*/
        public List<ItemDataBase> RemovedItemDataBase = new List<ItemDataBase>();
        public DesignerCanvas DesignerCanvas { get; set; }
        public bool Suppress /*阻止通知*/ { get; set; }

        private string _dataInfo;
        public string DataInfo
        {
            get { return _dataInfo; }
            set
            {
                _dataInfo = value;
                OnPropertyChanged("DataInfo");
            }
        }

        private string _selectionInfo;
        public string SelectionInfo
        {
            get { return _selectionInfo; }
            set
            {
                _selectionInfo = value;
                OnPropertyChanged("SelectionInfo");
            }
        }
        public bool IsOnEditing;/*双击出现编辑框，标识编辑状态，此时回车按键按下之后，会阻止新增相邻节点命令*/
        public DiagramManager DiagramManager { get; set; }
        #endregion

        #region Dependency Property 用于数据绑定

        public static readonly DependencyProperty DesignerItemsProperty = DependencyProperty.Register(
            "DesignerItems", typeof(ObservableCollection<DesignerItem>), typeof(DiagramControl), new PropertyMetadata(new ObservableCollection<DesignerItem>()));

        public ObservableCollection<DesignerItem> DesignerItems
        {
            get { return (ObservableCollection<DesignerItem>)GetValue(DesignerItemsProperty); }
            set { SetValue(DesignerItemsProperty, value); }
        }

        #region ZoomBoxControlProperty 缩放控件，以后需要修改

        public static readonly DependencyProperty ZoomBoxControlProperty = DependencyProperty.Register(
            "ZoomBoxControl", typeof(ZoomBoxControl), typeof(DiagramControl),
            new PropertyMetadata(default(ZoomBoxControl),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;
                    if (diagramControl != null)
                    {
                        var scrollViewer =
                            diagramControl.Template.FindName("DesignerScrollViewer", diagramControl) as ScrollViewer;
                        diagramControl.ZoomBoxControl.ScrollViewer = scrollViewer;
                    }
                    if (diagramControl != null) diagramControl.ZoomBoxControl.OnApplyTemplate();
                }));

        public ZoomBoxControl ZoomBoxControl
        {
            get { return (ZoomBoxControl)GetValue(ZoomBoxControlProperty); }
            set { SetValue(ZoomBoxControlProperty, value); }
        }

        #endregion

        #region ItemDatasProperty 数据源

        public static readonly DependencyProperty ItemDatasProperty = DependencyProperty.Register(
            "ItemDatas", typeof(ObservableCollection<object>), typeof(DiagramControl),
            new FrameworkPropertyMetadata(new ObservableCollection<object>()));

        public ObservableCollection<object> ItemDatas
        {
            get { return (ObservableCollection<object>)GetValue(ItemDatasProperty); }
            set { SetValue(ItemDatasProperty, value); }
        }

        #endregion

        #region DataSourceProperty 数据源

        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
            "DataSource", typeof(object), typeof(DiagramControl), new PropertyMetadata(default(object)));

        public object DataSource
        {
            get { return (object)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        #endregion

        #region DiagramHeaderProperty 画板区域标题

        public static readonly DependencyProperty DiagramHeaderProperty = DependencyProperty.Register(
            "DiagramHeader", typeof(string), typeof(DiagramControl), new PropertyMetadata(default(string)));

        public string DiagramHeader
        {
            get { return (string)GetValue(DiagramHeaderProperty); }
            set { SetValue(DiagramHeaderProperty, value); }
        }

        #endregion

        #region SelectedItems 选中项,用于向界面返回选中项

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems", typeof(ObservableCollection<DesignerItem>), typeof(DiagramControl),
            new FrameworkPropertyMetadata(new ObservableCollection<DesignerItem>()));

        public ObservableCollection<DesignerItem> SelectedItems
        {
            get { return (ObservableCollection<DesignerItem>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof(DesignerItem), typeof(DiagramControl),
            new FrameworkPropertyMetadata(null));

        public DesignerItem SelectedItem
        {
            get { return (DesignerItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        #endregion

        #region DesignerItemTemplate

        public static readonly DependencyProperty DesignerItemTemplateProperty =
            DependencyProperty.RegisterAttached("DesignerItemTemplate", typeof(DataTemplate), typeof(DiagramControl));

        public static DataTemplate GetDesignerItemTemplate(UIElement element)
        {
            return (DataTemplate)element.GetValue(DesignerItemTemplateProperty);
        }

        public static void SetDesignerItemTemplate(UIElement element, DataTemplate value)
        {
            element.SetValue(DesignerItemTemplateProperty, value);
        }
        #endregion

        #region LoadDataCommand
        public static readonly DependencyProperty LoadDataCommandProperty = DependencyProperty.Register(
            "LoadDataCommand", typeof(ICommand), typeof(DiagramControl), new PropertyMetadata(default(ICommand),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;

                    if (diagramControl.LoadDataCommand != null)
                    {
                        //diagramControl.DesignerItems.Clear();
                        diagramControl.LoadDataCommand.Execute(null);
                        foreach (var designerItem in diagramControl.DesignerItems)
                        {
                            designerItem.DiagramControl = diagramControl;
                        }
                        diagramControl.DiagramManager.GenerateDesignerItems();

                    }
                }));

        public ICommand LoadDataCommand
        {
            get { return (ICommand)GetValue(LoadDataCommandProperty); }
            set { SetValue(LoadDataCommandProperty, value); }
        }
        #endregion

        #endregion

        #region Constructors

        public DiagramControl()
        {
            DiagramManager = new DiagramManager(this);
            DesignerItems = new ObservableCollection<DesignerItem>();
            DesignerItems.CollectionChanged += (s, e) =>
            {
                if (Suppress) return;
                //GetDataInfo();
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    var items = e.NewItems.Cast<DesignerItem>().ToList();
                    if (!items.Any()) return;
                    foreach (var designerItem in items)
                    {
                        designerItem.ContextMenu = DesignerItem.GetItemContextMenu(this);
                    }
                }
            };

        }

        //public void GetDataInfo()
        //{

        //    var list = DesignerItems.Select(designerItem => designerItem.Data).ToList();
        //    DataInfo = string.Format("Changed:{0},Added:{1},Removed:{2},Total:{3}",
        //        list.Count(x => x.Changed),
        //        list.Count(x => x.Added),
        //        RemovedItemDataBase.Count(),
        //        list.Count(x => !x.Removed));
        //}

        #endregion

        #region Commands


        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            if (!Suppress)
                handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}