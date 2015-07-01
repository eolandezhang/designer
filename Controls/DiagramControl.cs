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
        #region Fields & Properties
        public readonly ObservableCollection<DesignerItem> DesignerItems;/*节点元素*/
        private DesignerCanvas Designer { get; set; }
        private bool PreventNotify { get; set; }
        #endregion

        #region Dependency Property 用于数据绑定

        #region ZoomBoxControlProperty 缩放控件，以后需要修改

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
                            diagramControl.GenerateDesignerItems();
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
            "DiagramHeader", typeof(string), typeof(DiagramControl), new FrameworkPropertyMetadata(default(string)));

        public string DiagramHeader
        {
            get { return (string)GetValue(DiagramHeaderProperty); }
            set { SetValue(DiagramHeaderProperty, value); }
        }

        #endregion

        #region SelectedItem 选中项

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
           "SelectedItem", typeof(DesignerItem), typeof(DiagramControl), new PropertyMetadata(default(DesignerItem)));

        public DesignerItem SelectedItem
        {
            get { return (DesignerItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        #endregion

        #endregion

        #region Constructors
        public DiagramControl()
        {
            DesignerItems = new ObservableCollection<DesignerItem>();
            DesignerItems.CollectionChanged += (s, e) => { if (!PreventNotify)BindData(); };
        }
        #endregion

        #region Methods

        DesignerItem InitData/*如果画布无节点则自动添加一个节点*/()
        {
            var id = Guid.NewGuid();
            var newItem = new DesignerItem(this, id, new CustomItemData(id, Guid.Empty, GetText(), "", false, false));
            DesignerItems.Add(newItem);
            return newItem;
        }

        void GenerateDesignerItems/*利用数据源在画布上添加节点及连线*/()
        {
            PreventNotify = true;/*利用数据源创建节点时不执行CollectionChange事件*/
            var root = InitDesignerItems();/*创建DesignerItems*/
            if (root == null)
            {
                root = InitData(); //如果DataSource中无数据，则自动创建一个根节点
            }
            BindData();/*将DesignerItems放到画布上，并且创建连线*/
            SelectedItem = root;
            DiagramManager.HighlightSelected(root);
            PreventNotify = false;
        }

        public void BindData/*将DesignerItems放到画布上，并且创建连线*/()
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
                items.ForEach(x => x.Data.DiagramControl = this);
                items.ForEach(x =>
                {
                    x.MouseDoubleClick += (sender, e) =>
                    {
                        _itemTextEditor = ItemTextEditor;
                        DiagramManager.Edit(Designer, SelectedItem, _itemTextEditor);
                    };
                });
            }
            if (DesignerItems.Count == 1)
                SelectedItem = DesignerItems.FirstOrDefault();
            //if (SelectedItem != null)
            //    DiagramManager.HighlightSelected(SelectedItem);
        }

        #endregion

        #region 用命令，创建节点
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
            DiagramManager.UpdateYIndex(newitem);
            DiagramManager.HighlightSelected(SelectedItem);
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
            DiagramManager.HighlightSelected(SelectedItem);
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
            //删除子节点
            DiagramManager.GetAllSubItems(d, list);
            foreach (var designerItem in list)
            {
                designerItem.Data.Removed = true;
                designerItem.Visibility = Visibility.Collapsed;
            }
            item.Data.Removed = true;
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
                };
                return t;
            }
        }
        private string GetText()
        {
            return "Item-" + DesignerItems.Count();
        }
        #endregion

        #region 用数据源，构建节点元素

        private DesignerItem InitDesignerItems()
        {
            var root = ItemDatas.FirstOrDefault(x => x.ParentId == Guid.Empty);
            if (root == null) return null;
            var rootDesignerItem = CreateRootItem(root);
            DesignerItems.Clear();
            DesignerItems.Add(rootDesignerItem);
            CreateChildDesignerItem(rootDesignerItem);
            return rootDesignerItem;
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
        { return new DesignerItem(this, itemData.Id, itemData); }
        private DesignerItem CreateChildItem(Guid parentId, ItemDataBase itemData)
        { return new DesignerItem(this, itemData.Id, parentId, itemData); }

        #endregion

        #region Commands

        public ICommand AddSiblingCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem != null)
                    {
                        AddSibling(SelectedItem);
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
                    if (SelectedItem != null)
                    {
                        AddAfter(SelectedItem);
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
                    if (SelectedItem != null)
                    {
                        Delete(SelectedItem);
                    }
                });
            }
        }
        public ICommand CollapseCommand
        {
            get
            {
                return new RelayCommand(Collapse);
            }
        }
        public ICommand ExpandCommand
        {
            get
            {
                return new RelayCommand(Expand);
            }
        }
        public ICommand GetDataCommand
        {
            get
            {
                return new RelayCommand(() =>
                   {
                       var list = DesignerItems.Select(designerItem => designerItem.Data).ToList();
                       MessageBox.Show(string.Format("Changed:{0}\nAdded:{1}\nRemoved:{2}\nTotal:{3}", list.Count(x => x.Changed), list.Count(x => x.Added), list.Count(x => x.Removed), list.Count(x => !x.Removed)));
                   });
            }
        }

        public ICommand ReloadCommand
        {
            get
            {
                return new RelayCommand(GenerateDesignerItems);
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
