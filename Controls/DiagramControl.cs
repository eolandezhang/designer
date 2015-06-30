using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DiagramDesigner.Controls;

namespace DiagramDesigner
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        #region Property

        public DesignerCanvas Designer { get; set; }
        public bool PreventNotify { get; set; }
        //public IDataSourceRepository DataSourceRepository { get; set; }

        #region SelectedItem
        private DesignerItem _selectedItem;
        public DesignerItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                if (Equals(SelectedItem, value)) return;
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }
        #endregion

        #region DiagramDataEditorControl

        public static readonly DependencyProperty DiagramDataEditorControlProperty = DependencyProperty.Register(
            "DiagramDataEditorControl", typeof(DiagramDataEditorControl), typeof(DiagramControl), new FrameworkPropertyMetadata(null,
                (d, e) =>
                {
                    var c = d as DiagramControl;
                    if (c.SelectedItem != null)
                        c.DiagramDataEditorControl.ItemData = c.SelectedItem.Data;
                }));

        public DiagramDataEditorControl DiagramDataEditorControl
        {
            get { return (DiagramDataEditorControl)GetValue(DiagramDataEditorControlProperty); }
            set { SetValue(DiagramDataEditorControlProperty, value); }
        }

        #endregion

        #region DataSource

        private ObservableCollection<DesignerItem> DataSource;

        //public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
        //    "Items", typeof(ObservableCollection<DesignerItem>), typeof(DiagramControl), new FrameworkPropertyMetadata(new ObservableCollection<DesignerItem>(),
        //        (d, e) =>
        //        {
        //            var diagramControl = d as DiagramControl;
        //            if (diagramControl != null) diagramControl.BindData();
        //        },
        //        (d, baseValue) => baseValue ?? new ObservableCollection<DesignerItem>()));

        //public ObservableCollection<DesignerItem> DataSource
        //{
        //    get { return (ObservableCollection<DesignerItem>)GetValue(DataSourceProperty); }
        //    set { SetValue(DataSourceProperty, value); }
        //}

        public static readonly DependencyProperty ItemDatasProperty = DependencyProperty.Register(
            "ItemDatas", typeof(ObservableCollection<IItemData>), typeof(DiagramControl), new FrameworkPropertyMetadata(new ObservableCollection<IItemData>(),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;
                    if (diagramControl != null)
                        diagramControl.LoadDataSource(diagramControl.ItemDatas);
                }));

        public ObservableCollection<IItemData> ItemDatas
        {
            get { return (ObservableCollection<IItemData>)GetValue(ItemDatasProperty); }
            set
            {
                ItemDatas.Clear();
                SetValue(ItemDatasProperty, value);
            }
        }

        #endregion

        #region DiagramHeaderProperty

        public static readonly DependencyProperty DiagramHeaderProperty = DependencyProperty.Register(
            "DiagramHeader", typeof(string), typeof(DiagramControl), new PropertyMetadata(default(string)));

        public string DiagramHeader
        {
            get { return (string)GetValue(DiagramHeaderProperty); }
            set { SetValue(DiagramHeaderProperty, value); }
        }

        #endregion

        public static readonly DependencyProperty AddSiblingCommandProperty = DependencyProperty.Register(
            "AddSiblingCommand", typeof(ICommand), typeof(DiagramControl), new PropertyMetadata(default(ICommand)));

        public ICommand AddSiblingCommand
        {
            get { return (ICommand)GetValue(AddSiblingCommandProperty); }
            set { SetValue(AddSiblingCommandProperty, value); }
        }

        #endregion

        #region Constructors

        public DiagramControl()
        {
            DataSource = new ObservableCollection<DesignerItem>();
            RegistPropertyChanged();
            RegistCollectionChanged();
        }

        #endregion

        #region Method

        void InitData()
        {
            if (!DataSource.Any())
            {
                PreventNotify = true;
                var id = Guid.NewGuid();
                var newItem = new DesignerItem(id, new CustomItemData(this, id, Guid.Empty, GetText(), "", false, false));
                DataSource.Add(newItem);
                SelectedItem = newItem;
                PreventNotify = false;
            }
        }
        /// <summary>
        /// 用数据初始化
        /// </summary>
        public void LoadDataSource(ObservableCollection<IItemData> data)
        {
            PreventNotify = true;
            var root = InitDesignerItems(data);
            if (root == null)
            {
                InitData(); //如果DataSource中无数据，则自动创建一个根节点
            }
            else
            {
                SelectedItem = root;
            }
            PreventNotify = false;
        }

        void RegistPropertyChanged()
        {
            PropertyChanged += (sender, e) =>
            {
                switch (e.PropertyName)
                {
                    case "SelectedItem":
                        if (SelectedItem != null)
                        {
                            DiagramManager.HighlightSelected(SelectedItem);
                            if (DiagramDataEditorControl != null)
                            {
                                DiagramDataEditorControl.DiagramControl = this;
                                DiagramDataEditorControl.ItemData = SelectedItem.Data;

                            }
                        }
                        break;
                }
            };
        }

        void RegistCollectionChanged()
        {
            DataSource.CollectionChanged += (s, e) =>
            {
                if (!PreventNotify)
                    BindData();
            };
        }

        public void BindData()
        {
            var designer = Designer;
            if (designer == null) return;
            designer.Children.Clear();
            if (!DataSource.Any()) return;
            var designerItems = DiagramManager.GenerateItems(designer, DataSource);
            if (designerItems == null) return;
            DiagramManager.ArrangeWithRootItems(designer);
            var items = DiagramManager.GetDesignerItems(designer);
            if (items != null)
            {
                items.ForEach(x =>
                {
                    x.PreviewMouseLeftButtonDown += (s, e) =>
                    {
                        SelectedItem = designer.SelectionService.SelectedItem;
                    };
                    x.MouseDoubleClick += (sender, e) =>
                    {
                        _itemTextEditor = ItemTextEditor;
                        DiagramManager.Edit(Designer, SelectedItem, _itemTextEditor);
                    }
                    ;
                });
            }
            if (DataSource.Count == 1)
                SelectedItem = DataSource.FirstOrDefault();

            AddSiblingCommand = new RelayCommand(() =>
            {
                if (SelectedItem != null)
                {
                    MessageBox.Show("Add Sibling:" + SelectedItem.Data.Text);
                }
            });
        }

        #endregion

        #region 节点操作

        public void AddSibling(DesignerItem designerItem)
        {
            if (designerItem == null) return;
            if (designerItem.Data == null) return;
            if (designerItem.Data.ParentId.Equals(Guid.Empty))
            {
                AddAfter(designerItem);
                return;
            }
            var n5 = Guid.NewGuid();
            if (DataSource.Any(x => x.ID.Equals(n5)))
            {
                return;
            }
            var parent = DataSource.FirstOrDefault(x => x.ID == designerItem.Data.ParentId);
            if (parent == null) return;
            var newitem = new DesignerItem(n5, parent.ID, new CustomItemData(this, n5, parent.ID, GetText(), true, false));
            DataSource.Add(newitem);
            SelectedItem = newitem;
            UpdateItemDatas();
        }
        public void AddAfter(DesignerItem parentDesignerItem)
        {
            if (parentDesignerItem == null) return;
            if (parentDesignerItem.Data == null) return;
            var n5 = Guid.NewGuid();
            if (DataSource.Any(x => x.ID.Equals(n5)))
            {
                return;
            }
            var newitem = new DesignerItem(n5, parentDesignerItem.ID,
                new CustomItemData(this, n5, parentDesignerItem.ID, GetText(), true, false));
            DataSource.Add(newitem);
            SelectedItem = newitem;
            DiagramManager.UpdateYIndex(newitem);
            UpdateItemDatas();
        }
        private string GetText()
        {
            return "Item-" + DataSource.Count();
        }

        public void Delete(DesignerItem d)
        {
            if (d == null) return;
            if (d.Data.ParentId == Guid.Empty) return;
            if (d.Data == null) return;
            var item = this.DataSource.FirstOrDefault(x => x.ID == d.ID);
            if (item == null) return;
            PreventNotify = true;
            var list = new List<DesignerItem>();
            DiagramManager.GetAllSubItems(d, list);

            foreach (var designerItem in list)
            {
                var item1 = designerItem;
                //DataSource.Remove(item1);
                item1.Data.Removed = true;
                item1.Visibility = Visibility.Collapsed;
            }
            //DataSource.Remove(item);
            item.Data.Removed = true;
            item.Visibility = Visibility.Collapsed;
            PreventNotify = false;
            var connections = DiagramManager.GetItemConnections(d);
            var sink = connections.Where(x => x.Sink.ParentDesignerItem.Equals(d));
            foreach (var connection in sink)
            {
                //connection.Sink = null;
                Designer.Children.Remove(connection);
                connection.Visibility = Visibility.Collapsed;
            }
            if (!PreventNotify)
                BindData();
            var parent = DataSource.FirstOrDefault(x => x.ID == d.Data.ParentId);
            SelectedItem = parent;

            UpdateItemDatas();
        }

        public void Collapse()
        {
            if (Designer == null) return;
            DiagramManager.CollapseAll(Designer);
        }
        public void Expand()
        {
            if (Designer == null) return;
            DiagramManager.ExpandAll(Designer);
        }

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
                    var data = DataSource.FirstOrDefault(x => x.ID == SelectedItem.ID);
                    if (data == null) return;
                    data.Data.Text = ItemTextEditor.Text;
                    data.Data.Changed = true;
                    DiagramManager.SetItemText(data, ItemTextEditor.Text);
                    UpdateItemDatas();
                };
                return t;
            }
        }

        #endregion

        #region 数据源初始化节点
        public DesignerItem CreateRootItem(Guid id, IItemData itemData)
        {
            return new DesignerItem(id, itemData);
        }

        public DesignerItem CreateChildItem(Guid parentId, Guid childId, IItemData itemData)
        {
            return new DesignerItem(childId, parentId, itemData);
        }
        #endregion

        public DesignerItem InitDesignerItems(IList<IItemData> data)
        {
            var root = ItemDatas.FirstOrDefault(x => x.ParentId == Guid.Empty);
            if (root == null) return null;
            var rootDesignerItem = CreateRootItem(root.Id, root);
            DataSource.Clear();
            DataSource.Add(rootDesignerItem);
            CreateChildDesignerItem(rootDesignerItem);
            return rootDesignerItem;
        }

        public void CreateChildDesignerItem(DesignerItem parentDesignerItem)
        {
            var child = ItemDatas.Where(x => x.ParentId == parentDesignerItem.ID && !x.Removed);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = CreateChildItem(parentDesignerItem.ID, userDataSource.Id, userDataSource);
                DataSource.Add(childDesignerItem);
                CreateChildDesignerItem(childDesignerItem);
            }
        }

        public void UpdateItemDatas()
        {
            ItemDatas.Clear();
            foreach (var item in DataSource)
            {
                ItemDatas.Add(item.Data as CustomItemData);
            }
        }


        #region Override

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var designer = (DesignerCanvas)GetTemplateChild("Designer");
            if (designer != null)
            {
                Designer = designer;
            }
            var diagramHeader = (GroupBox)GetTemplateChild("DiagramHeader");
            if (diagramHeader != null) diagramHeader.Header = DiagramHeader;
            BindData();

            var root = DataSource.FirstOrDefault(x => x.Data.ParentId == Guid.Empty);
            DiagramManager.HighlightSelected(root);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            if (!PreventNotify)
                handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}
