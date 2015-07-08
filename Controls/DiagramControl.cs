using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;

namespace DiagramDesigner.Controls
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        public DiagramManager DiagramManager { get; set; }

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var designer = (DesignerCanvas)GetTemplateChild("Designer");
            if (designer != null)
            {
                Designer = designer;
                //designer.PreviewMouseMove += (s, e) =>
                //{
                //    var selectedItems =
                //        designer.SelectionService.CurrentSelection.ConvertAll(item => item as DesignerItem);
                //    if (selectedItems.Count() != 0)
                //    {
                //        SelectionInfo = "Selected:" + selectedItems.Count.ToString();
                //    }
                //    else
                //    {
                //        SelectionInfo = "Selected:0";
                //    }
                //};
            }
            var diagramHeader = (GroupBox)GetTemplateChild("DiagramHeader");
            if (diagramHeader != null) diagramHeader.Header = DiagramHeader;
        }

        #endregion

        #region Fields & Properties

        public readonly ObservableCollection<DesignerItem> DesignerItems; /*节点元素*/
        public List<ItemDataBase> RemovedItemDataBase = new List<ItemDataBase>();
        public DesignerCanvas Designer { get; set; }
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

        #endregion

        #region Dependency Property 用于数据绑定

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
            "ItemDatas", typeof(ObservableCollection<ItemDataBase>), typeof(DiagramControl),
            new FrameworkPropertyMetadata(new ObservableCollection<ItemDataBase>(),
                (d, e) =>
                {
                    var diagramControl = (DiagramControl)d;
                    if (diagramControl.ItemDatas == null) return;
                    if (diagramControl.Suppress) return;
                    var n = e.NewValue as ObservableCollection<ItemDataBase>;
                    if (n == null) return;

                    foreach (var itemDataBase in n.Where(x => x.ParentId == Guid.Empty))
                    {
                        diagramControl.DiagramManager.GenerateDesignerItems(itemDataBase.Id);
                    }

                    n.CollectionChanged += (sender, arg) =>
                    {
                        switch (arg.Action)
                        {
                            case NotifyCollectionChangedAction.Add:
                                {
                                    var items = arg.NewItems.Cast<ItemDataBase>();
                                    var f = items.FirstOrDefault();
                                    if (f != null)
                                    {
                                        diagramControl.DiagramManager.AddDesignerItem(f);
                                    }
                                }
                                break;
                            case NotifyCollectionChangedAction.Remove:
                                {
                                    var items = arg.OldItems.Cast<ItemDataBase>();
                                    var f = items.FirstOrDefault();
                                    if (f != null)
                                    {
                                        diagramControl.DiagramManager.RemoveDesignerItem(f);
                                    }
                                }
                                break;
                        }
                    };


                }));

        public ObservableCollection<ItemDataBase> ItemDatas
        {
            get { return (ObservableCollection<ItemDataBase>)GetValue(ItemDatasProperty); }
            set { SetValue(ItemDatasProperty, value); }
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

        #region SelectedItems 选中项

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
            "SelectedItems", typeof(ObservableCollection<DesignerItem>), typeof(DiagramControl),
            new FrameworkPropertyMetadata(new ObservableCollection<DesignerItem>()));

        public ObservableCollection<DesignerItem> SelectedItems
        {
            get { return (ObservableCollection<DesignerItem>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
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
                GetDataInfo();
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
        public void GetDataInfo()
        {

            var list = DesignerItems.Select(designerItem => designerItem.Data).ToList();
            DataInfo = string.Format("Changed:{0},Added:{1},Removed:{2},Total:{3}",
                list.Count(x => x.Changed),
                list.Count(x => x.Added),
                RemovedItemDataBase.Count(),
                list.Count(x => !x.Removed));
        }

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