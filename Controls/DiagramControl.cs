using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DiagramDesigner.Controls
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        private ObservableCollection<DesignerItem> DesignerItems;/*节点元素*/

        #region Private Property
        private DesignerCanvas Designer { get; set; }
        private bool PreventNotify { get; set; }
        #endregion

        #region Public Property

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

        #endregion

        #region Dependency Property

        #region DiagramDataEditorControl 节点编辑器

        public static readonly DependencyProperty DiagramDataEditorControlProperty = DependencyProperty.Register(
            "DiagramDataEditorControl", typeof(DiagramDataEditorControl), typeof(DiagramControl), new FrameworkPropertyMetadata(null,
                (d, e) =>
                {
                    var c = d as DiagramControl;
                    if (c.SelectedItem != null)
                        c.DiagramDataEditorControl.ItemData = c.SelectedItem.Data;
                    else
                    {
                        c.DiagramDataEditorControl.ItemData = new ItemDataBase();
                    }
                }));

        public DiagramDataEditorControl DiagramDataEditorControl
        {
            get { return (DiagramDataEditorControl)GetValue(DiagramDataEditorControlProperty); }
            set { SetValue(DiagramDataEditorControlProperty, value); }
        }

        #endregion

        #region ZoomBoxControlProperty

        public static readonly DependencyProperty ZoomBoxControlProperty = DependencyProperty.Register(
            "ZoomBoxControl", typeof(ZoomBoxControl), typeof(DiagramControl), new FrameworkPropertyMetadata(default(ZoomBoxControl),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;
                    if (diagramControl != null)
                    {
                        var scrollViewer = diagramControl.Template.FindName("DesignerScrollViewer", diagramControl) as ScrollViewer;
                        diagramControl.ZoomBoxControl.ScrollViewer = scrollViewer;
                    }
                    diagramControl.ZoomBoxControl.OnApplyTemplate();
                }));

        public ZoomBoxControl ZoomBoxControl
        {
            get { return (ZoomBoxControl)GetValue(ZoomBoxControlProperty); }
            set { SetValue(ZoomBoxControlProperty, value); }
        }
        #endregion

        #region ItemDatasProperty 数据源

        public static readonly DependencyProperty ItemDatasProperty = DependencyProperty.Register(
            "ItemDatas", typeof(ObservableCollection<ItemDataBase>), typeof(DiagramControl), new FrameworkPropertyMetadata(new ObservableCollection<ItemDataBase>(),
                (d, e) =>
                {
                    var diagramControl = d as DiagramControl;
                    if (diagramControl != null)
                    {
                        if (diagramControl.ItemDatas != null)
                        {
                            diagramControl.GenerateDesignerItems(diagramControl.ItemDatas);
                        }

                    }
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

        #endregion

        #region Constructors

        public DiagramControl()
        {
            DesignerItems = new ObservableCollection<DesignerItem>();
            //ItemDatas = new ObservableCollection<ItemDataBase>();
            RegistPropertyChanged();
            RegistCollectionChanged();
            ItemDatas = new ObservableCollection<ItemDataBase>()
            {
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,2),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,1)
            };
            GenerateDesignerItems(ItemDatas);
        }

        #endregion

        #region Method

        void InitData()
        {
            if (!DesignerItems.Any())
            {
                PreventNotify = true;
                var id = Guid.NewGuid();
                var newItem = new DesignerItem(this, id, new CustomItemData(id, Guid.Empty, GetText(), "", false, false));
                DesignerItems.Add(newItem);
                SelectedItem = newItem;
                PreventNotify = false;
            }
        }
        /// <summary>
        /// 用数据初始化
        /// </summary>
        void GenerateDesignerItems(IList<ItemDataBase> data)
        {
            PreventNotify = true;
            var root = InitDesignerItems(data);
            if (root == null)
            {
                //InitData(); //如果DataSource中无数据，则自动创建一个根节点
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
                        if (DiagramDataEditorControl != null)
                        {
                            DiagramDataEditorControl.DiagramControl = this;
                            if (SelectedItem != null)
                            {
                                DiagramManager.HighlightSelected(SelectedItem);
                                if (DiagramDataEditorControl != null)
                                {
                                    DiagramDataEditorControl.ItemData = SelectedItem.Data;
                                }
                            }
                            else
                            {

                                DiagramDataEditorControl.ItemData = null;
                            }
                        }
                        break;
                }
            };
        }

        void RegistCollectionChanged()
        {
            DesignerItems.CollectionChanged += (s, e) =>
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
            //if (!DesignerItems.Any()) return;
            var designerItems = DiagramManager.GenerateItems(designer, DesignerItems);
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
            if (DesignerItems.Count == 1)
                SelectedItem = DesignerItems.FirstOrDefault();
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
            if (DesignerItems.Any(x => x.ID.Equals(n5)))
            {
                return;
            }
            var parent = DesignerItems.FirstOrDefault(x => x.ID == designerItem.Data.ParentId);
            if (parent == null) return;
            var newitem = new DesignerItem(this, n5, parent.ID, new CustomItemData(n5, parent.ID, GetText(), "", true, false));
            DesignerItems.Add(newitem);
            SelectedItem = newitem;
            UpdateItemDatas();
        }
        public void AddAfter(DesignerItem parentDesignerItem)
        {
            if (parentDesignerItem == null) return;
            if (parentDesignerItem.Data == null) return;
            var n5 = Guid.NewGuid();
            if (DesignerItems.Any(x => x.ID.Equals(n5)))
            {
                return;
            }
            var newitem = new DesignerItem(this, n5, parentDesignerItem.ID,
                new CustomItemData(n5, parentDesignerItem.ID, GetText(), "", true, false));
            DesignerItems.Add(newitem);
            SelectedItem = newitem;
            DiagramManager.UpdateYIndex(newitem);
            UpdateItemDatas();
        }
        private string GetText()
        {
            return "Item-" + DesignerItems.Count();
        }

        public void Delete(DesignerItem d)
        {
            if (d == null) return;
            if (d.Data.ParentId == Guid.Empty) return;
            if (d.Data == null) return;
            var item = this.DesignerItems.FirstOrDefault(x => x.ID == d.ID);
            if (item == null) return;
            PreventNotify = true;
            var list = new List<DesignerItem>();
            DiagramManager.GetAllSubItems(d, list);

            foreach (var designerItem in list)
            {
                var item1 = designerItem;
                if (!item1.Data.Added) item1.Data.Removed = true;
                item1.Visibility = Visibility.Collapsed;
            }
            if (!item.Data.Added) item.Data.Removed = true;
            item.Visibility = Visibility.Collapsed;
            PreventNotify = false;
            #region 移除连线

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
            var parent = DesignerItems.FirstOrDefault(x => x.ID == d.Data.ParentId);
            SelectedItem = parent;

            #endregion

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
                    var data = DesignerItems.FirstOrDefault(x => x.ID == SelectedItem.ID);
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
        public DesignerItem CreateRootItem(Guid id, ItemDataBase itemData)
        {
            return new DesignerItem(this, id, itemData);
        }

        public DesignerItem CreateChildItem(Guid parentId, Guid childId, ItemDataBase itemData)
        {
            return new DesignerItem(this, childId, parentId, itemData);
        }
        #endregion

        #region 用数据源，构建节点元素

        DesignerItem InitDesignerItems(IList<ItemDataBase> data)
        {
            var root = ItemDatas.FirstOrDefault(x => x.ParentId == Guid.Empty);
            if (root == null) return null;
            var rootDesignerItem = CreateRootItem(root.Id, root);
            DesignerItems.Clear();
            DesignerItems.Add(rootDesignerItem);
            CreateChildDesignerItem(rootDesignerItem);
            return rootDesignerItem;
        }

        void CreateChildDesignerItem(DesignerItem parentDesignerItem)
        {
            var child = ItemDatas.Where(x => x.ParentId == parentDesignerItem.ID && !x.Removed);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = CreateChildItem(parentDesignerItem.ID, userDataSource.Id, userDataSource);
                DesignerItems.Add(childDesignerItem);
                CreateChildDesignerItem(childDesignerItem);
            }
        }

        #endregion

        #region 控件发生变化，同时更新数据源

        void UpdateItemDatas()
        {
            ItemDatas.Clear();
            foreach (var item in DesignerItems)
            {
                ItemDatas.Add(item.Data as CustomItemData);
            }
        }

        #endregion

        #region Commands

        public ICommand AddSiblingCommand
        {
            get
            {
                return new RelayCommand<DesignerItem>((x) =>
                {
                    if (SelectedItem != null)
                    {
                        AddSibling(x);
                    }
                });
            }
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
            }
            var diagramHeader = (GroupBox)GetTemplateChild("DiagramHeader");
            if (diagramHeader != null) diagramHeader.Header = DiagramHeader;
            BindData();

            var root = DesignerItems.FirstOrDefault(x => x.Data.ParentId == Guid.Empty);
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
