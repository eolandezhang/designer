using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DiagramDesigner.Controls
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        public DiagramManager DiagramManager;
        #region Fields & Properties
        public readonly ObservableCollection<DesignerItem> DesignerItems;/*节点元素*/
        public DesignerCanvas Designer { get; set; }
        private bool Suppress /*阻止通知*/{ get; set; }
        private DesignerItem Copy;

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
            "ZoomBoxControl", typeof(ZoomBoxControl), typeof(DiagramControl), new PropertyMetadata(default(ZoomBoxControl),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;
                    if (diagramControl != null)
                    {
                        var scrollViewer = diagramControl.Template.FindName("DesignerScrollViewer", diagramControl) as ScrollViewer;
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
            "ItemDatas", typeof(ObservableCollection<ItemDataBase>), typeof(DiagramControl), new PropertyMetadata(new ObservableCollection<ItemDataBase>(),
                (d, e) =>
                {

                    var diagramControl = d as DiagramControl;
                    if (diagramControl == null) return;
                    if (diagramControl.Suppress) return;
                    if (diagramControl.ItemDatas != null) { diagramControl.GenerateDesignerItems(); }
                }));

        public ObservableCollection<ItemDataBase> ItemDatas
        {
            get { return (ObservableCollection<ItemDataBase>)GetValue(ItemDatasProperty); }
            set
            {
                ItemDatas.Clear();
                SetValue(ItemDatasProperty, value);
            }
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

        #region SelectedItem 选中项

        //public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        //   "SelectedItem", typeof(DesignerItem), typeof(DiagramControl), new PropertyMetadata(default(DesignerItem)));

        //public DesignerItem SelectedItem
        //{
        //    get { return (DesignerItem)GetValue(SelectedItemProperty); }
        //    set { SetValue(SelectedItemProperty, value); }
        //}

        public DesignerItem SelectedItem
        {
            get;
            set;
        }

        #endregion

        //public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        //    "SelectedItems", typeof(List<DesignerItem>), typeof(DiagramControl), new PropertyMetadata(default(List<DesignerItem>)));

        //public List<DesignerItem> SelectedItems
        //{
        //    get { return (List<DesignerItem>)GetValue(SelectedItemsProperty); }
        //    set { SetValue(SelectedItemsProperty, value); }
        //}

        #endregion

        #region Constructors
        public DiagramControl()
        {
            DiagramManager = new DiagramManager(this);
            DesignerItems = new ObservableCollection<DesignerItem>();
            DesignerItems.CollectionChanged += (s, e) =>
            {
                if (!Suppress)
                {
                    BindData();
                    GetDataInfo();
                }
            };
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, ExcuteCopy));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, ExcutePaste));
        }
        private void ExcutePaste(object sender, ExecutedRoutedEventArgs e)
        {
            PasteCommand.Execute(null);
        }

        private void ExcuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            CopyCommand.Execute(null);
        }

        void GetDataInfo()
        {
            var list = DesignerItems.Select(designerItem => designerItem.Data).ToList();
            DataInfo = string.Format("Changed:{0},Added:{1},Removed:{2},Total:{3}",
                         list.Count(x => x.Changed),
                         list.Count(x => x.Added),
                         list.Count(x => x.Removed),
                         list.Count(x => !x.Removed));
        }
        #endregion

        #region Methods

        DesignerItem InitData/*如果画布无节点则自动添加一个节点*/()
        {
            var id = Guid.NewGuid();
            var newItem = new DesignerItem(id, new CustomItemData(id, Guid.Empty, GetText(), "", false, false, 5d, 5d));
            DesignerItems.Add(newItem);
            return newItem;
        }

        void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/()
        {
            Suppress = true;/*利用数据源创建节点时不执行CollectionChange事件*/

            var roots = InitDesignerItems();
            if (!roots.Any()) InitData();/*创建DesignerItems*/
            BindData();/*将DesignerItems放到画布上，并且创建连线*/
            SelectedItem = roots.FirstOrDefault();
            //DiagramManager.HighlightSelected(SelectedItem);
            Suppress = false;
            GetDataInfo();
        }

        public void BindData/*将DesignerItems放到画布上，并且创建连线*/()
        {
            var designer = Designer;
            if (designer == null) return;

            var designerItems = DiagramManager.GenerateItems();
            if (designerItems == null) return;
            if (!designerItems.Any()) return;
            DiagramManager.ArrangeWithRootItems();
            Suppress = true;
            var items = DiagramManager.GetDesignerItems();
            if (items == null) return;
            items.ForEach(x => x.Data.DiagramControl = this);
            items.ForEach(x =>
            {
                x.MouseDoubleClick += (sender, e) =>
                {
                    _itemTextEditor = ItemTextEditor;
                    DiagramManager.Edit(Designer, x, _itemTextEditor);
                };
            });
            Suppress = false;
        }

        #endregion

        #region 双击编辑

        TextBox _itemTextEditor;
        TextBox ItemTextEditor
        {
            get
            {
                if (_itemTextEditor != null) return _itemTextEditor;
                var t = new TextBox();
                t.LostFocus += (sender, e) =>
                {
                    Designer.Children.Remove(ItemTextEditor);
                    var data = DesignerItems.FirstOrDefault(x => x.ID == SelectedItem.ID);
                    if (data == null) return;
                    data.Data.Text = ItemTextEditor.Text;
                    data.Data.Changed = true;
                    DiagramManager.SetItemText(data, ItemTextEditor.Text);
                    GetDataInfo();
                };
                return t;
            }
        }

        #endregion

        #region 用数据源，构建节点元素

        private List<DesignerItem> InitDesignerItems()
        {

            var roots = ItemDatas.Where(x => x.ParentId == Guid.Empty);
            if (roots == null || !roots.Any()) return null;
            List<DesignerItem> rootDesignerItems = new List<DesignerItem>();
            DesignerItems.Clear();
            foreach (var root in roots)
            {
                var rootDesignerItem = CreateRootItem(root);
                rootDesignerItems.Add(rootDesignerItem);

                DesignerItems.Add(rootDesignerItem);
                CreateChildDesignerItem(rootDesignerItem);
            }
            return rootDesignerItems;
        }
        private void CreateChildDesignerItem(DesignerItem parentDesignerItem)
        {
            var child = ItemDatas.Where(x => x.ParentId == parentDesignerItem.ID && !x.Removed);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = CreateChildItem(parentDesignerItem.ID, userDataSource);
                DesignerItems.Add(childDesignerItem);
                CreateChildDesignerItem(childDesignerItem);
            }
        }
        private DesignerItem CreateRootItem(ItemDataBase itemData)
        { return new DesignerItem(itemData.Id, itemData); }
        private DesignerItem CreateChildItem(Guid parentId, ItemDataBase itemData)
        { return new DesignerItem(itemData.Id, parentId, itemData); }

        #endregion

        #region Commands
        public ICommand AddRootDesignerItemCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var n5 = Guid.NewGuid();
                    var newitem = new DesignerItem(n5, new CustomItemData(n5, Guid.Empty, GetText(), "", true, false, Designer.ActualWidth / 2, Designer.ActualHeight / 2));

                    DesignerItems.Add(newitem);
                    DiagramManager.SetSelectItem(newitem);
                });
            }
        }
        public ICommand AddSiblingCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var designerItem = DiagramManager.GetSelectedItem();
                    if (designerItem != null)
                    {
                        if (designerItem == null) return;
                        if (designerItem.Data == null) return;
                        if (designerItem.Data.ParentId.Equals(Guid.Empty)) { AddAfterCommand.Execute(null); return; }
                        var n5 = Guid.NewGuid();
                        if (DesignerItems.Any(x => x.ID.Equals(n5))) { return; }
                        var parent = DesignerItems.FirstOrDefault(x => x.ID == designerItem.Data.ParentId);
                        if (parent == null) return;
                        var newitem = new DesignerItem(n5, new CustomItemData(n5, parent.ID, GetText(), "", true, false, 0, double.MaxValue));
                        DesignerItems.Add(newitem);
                        DiagramManager.SetSelectItem(newitem);
                        GetDataInfo();
                    }
                });
            }
        }
        public ICommand AddAfterCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var parentDesignerItem = DiagramManager.GetSelectedItem();
                    if (parentDesignerItem != null)
                    {
                        if (parentDesignerItem == null) return;
                        if (parentDesignerItem.Data == null) return;
                        var n5 = Guid.NewGuid();
                        if (DesignerItems.Any(x => x.ID.Equals(n5))) { return; }
                        var newitem = new DesignerItem(n5, parentDesignerItem.ID,
                            new CustomItemData(n5, parentDesignerItem.ID, GetText(), "", true, false, 0, double.MaxValue));
                        DesignerItems.Add(newitem);

                        DiagramManager.SetSelectItem(newitem);

                        GetDataInfo();
                    }
                });
            }
        }
        public ICommand RemoveCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    var d = DiagramManager.GetSelectedItem();
                    if (d != null)
                    {
                        if (d == null) return;
                        if (d.Data == null) return;
                        var item = this.DesignerItems.FirstOrDefault(x => x.ID == d.ID);
                        if (item == null) return;
                        Suppress = true;
                        var list = new List<DesignerItem>();
                        //删除子节点
                        DiagramManager.GetAllSubItems(d, list);
                        foreach (var designerItem in list)
                        {
                            designerItem.Data.Removed = true;
                            designerItem.Visibility = Visibility.Collapsed;
                        }
                        item.Data.Removed = true;
                        item.Visibility = Visibility.Collapsed;
                        Suppress = false;

                        #region 移除连线
                        var connections = DiagramManager.GetItemConnections(d);
                        var sink = connections.Where(x => x.Sink.ParentDesignerItem.Equals(d));
                        foreach (var connection in sink)
                        {
                            Designer.Children.Remove(connection);
                            connection.Visibility = Visibility.Collapsed;
                        }
                        if (!Suppress)
                            BindData();
                        if (d.Data.ParentId != Guid.Empty)
                        {
                            var parent = DesignerItems.FirstOrDefault(x => x.ID == d.Data.ParentId);
                            SelectedItem = parent;
                        }
                        #endregion

                        GetDataInfo();

                    }

                });
            }
        }
        public ICommand CollapseCommand
        {
            get
            {
                return new RelayCommand(() => { DiagramManager.CollapseAll(); });
            }
        }
        public ICommand ExpandCommand
        {
            get
            {
                return new RelayCommand(() => { DiagramManager.ExpandAll(); });
            }
        }
        public ICommand ReloadCommand
        {
            get
            {
                return new RelayCommand(GenerateDesignerItems);
            }
        }
        public ICommand CopyCommand
        {
            get
            {
                return new RelayCommand(() =>
                    {
                        var selectedItem = DiagramManager.GetSelectedItem();
                        if (selectedItem != null)
                            Copy = (DesignerItem)SelectedItem.Clone();

                    });
            }
        }
        public ICommand PasteCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (Copy != null)
                    {
                        var selectedItem = DiagramManager.GetSelectedItem();
                        if (selectedItem != null)
                        {
                            Copy.Data.ParentId = selectedItem.ID;
                            DesignerItems.Add(Copy);
                            Copy = null;
                        }
                    }
                });
            }
        }
        public ICommand SaveCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    Suppress = true;
                    ItemDatas.Clear();
                    foreach (var item in DesignerItems)
                    {
                        ItemDatas.Add(item.Data);
                    }
                    BindData();
                    Suppress = false;

                });
            }
        }
        private string GetText()
        {
            return "Item-" + DesignerItems.Count();
        }
        #endregion

        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var designer = (DesignerCanvas)GetTemplateChild("Designer");
            if (designer != null)
            {
                Designer = designer;
                designer.PreviewMouseMove += (s, e) =>
                {
                    var selectedItems = designer.SelectionService.CurrentSelection.ConvertAll((item) => { return item as DesignerItem; });
                    if (selectedItems != null && selectedItems.Count() != 0)
                    {
                        SelectionInfo = "Selected:" + selectedItems.Count.ToString();
                    }
                    else
                    {
                        SelectionInfo = "Selected:0";
                    }
                };

            }
            var diagramHeader = (GroupBox)GetTemplateChild("DiagramHeader");
            if (diagramHeader != null) diagramHeader.Header = DiagramHeader;
            BindData();


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
