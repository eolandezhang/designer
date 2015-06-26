using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DiagramDesigner
{
    public class DiagramControl : ContentControl, INotifyPropertyChanged
    {
        #region Property

        public DesignerCanvas Designer { get; set; }
        public bool PreventNotify { get; set; }

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

        public static readonly DependencyProperty DataSourceProperty = DependencyProperty.Register(
            "Items", typeof(ObservableCollection<DesignerItem>), typeof(DiagramControl), new FrameworkPropertyMetadata(new ObservableCollection<DesignerItem>(), OnItemsChanged, OnCoerecItemsValue));
        public ObservableCollection<DesignerItem> DataSource
        {
            get { return (ObservableCollection<DesignerItem>)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        private static object OnCoerecItemsValue(DependencyObject d, object baseValue)
        {
            return baseValue ?? new ObservableCollection<DesignerItem>();
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var diagramControl = d as DiagramControl;
            if (diagramControl != null) diagramControl.BindData();
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

        #endregion

        #region Constructors

        public DiagramControl()
        {
            LoadDataSource();//模拟数据
            InitData();
            RegistPropertyChanged();
            DataSource.CollectionChanged += (s, e) =>
            {
                if (!PreventNotify)
                    BindData();
            };
        }

        #endregion

        #region Method

        void InitData()
        {
            if (!DataSource.Any())
            {
                PreventNotify = true;
                var newItem = new DesignerItem(Guid.NewGuid(), new CustomItemData(this, GetText(), ""));
                DataSource.Add(newItem);
                PreventNotify = false;
            }
        }

        void LoadDataSource()
        {
            PreventNotify = true;
            var root = UserDataSourceRepository.InitDesignerItems(this);
            if (root != null)
            {
                var list = UserDataSourceRepository.DesignerItems.OrderBy(x => x.Data.YIndex);
                foreach (var designerItem in list)
                {
                    DataSource.Add(designerItem);
                }
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
        }

        #endregion

        #region 节点操作

        public void AddSibling(DesignerItem designerItem)
        {
            if (designerItem == null) return;
            if (designerItem.Data == null) return;
            if (designerItem.ParentItemId.Equals(Guid.Empty))
            {
                AddAfter(designerItem);
                return;
            }
            var n5 = Guid.NewGuid();
            if (DataSource.Any(x => x.ID.Equals(n5)))
            {
                return;
            }
            var parent = DataSource.FirstOrDefault(x => x.ID == designerItem.ParentItemId);
            if (parent == null) return;
            var newitem = new DesignerItem(n5, parent.ID, new CustomItemData(this, GetText(), ""));
            DataSource.Add(newitem);
            SelectedItem = newitem;
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
            var newitem = new DesignerItem(n5, parentDesignerItem.ID, new CustomItemData(this, GetText(), ""));
            DataSource.Add(newitem);
            SelectedItem = newitem;
            DiagramManager.UpdateYIndex(newitem);
        }
        private string GetText()
        {
            return "Item-" + DataSource.Count();
        }

        public void Delete(DesignerItem d)
        {
            if (d == null) return;
            if (d.ParentItemId == Guid.Empty) return;
            if (d.Data == null) return;
            var item = this.DataSource.FirstOrDefault(x => x.ID == d.ID);
            if (item == null) return;
            PreventNotify = true;
            var list = new List<DesignerItem>();
            DiagramManager.GetAllSubItems(d, list);

            foreach (var designerItem in list)
            {
                var item1 = designerItem;
                DataSource.Remove(item1);
            }
            DataSource.Remove(item);
            PreventNotify = false;
            var connections = DiagramManager.GetItemConnections(d);
            var sink = connections.Where(x => x.Sink.ParentDesignerItem.Equals(d));
            foreach (var connection in sink)
            {
                connection.Sink = null;
            }
            if (!PreventNotify)
                BindData();
            var parent = DataSource.FirstOrDefault(x => x.ID == d.ParentItemId);
            SelectedItem = parent;
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

                };
                return t;
            }
        }

        #endregion

        #region 数据源初始化节点
        public DesignerItem CreateRootItem(string id, IItemData itemData)
        {
            return new DesignerItem(new Guid(id), itemData);
        }

        public DesignerItem CreateChildItem(string parentId, string childId, IItemData itemData)
        {
            return new DesignerItem(new Guid(childId), new Guid(parentId), itemData);
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

            var root = DataSource.FirstOrDefault(x => x.ParentItemId == Guid.Empty);
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
