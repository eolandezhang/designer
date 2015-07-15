using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DiagramDesigner
{
    public sealed class MainViewModel : ObservableObject
    {
        private DesignerItem _selectedItem;
        public DesignerItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                OnPropertyChanged("SelectedItem");
            }
        }
        private List<ItemData> _itemDatas;
        public List<ItemData> ItemDatas
        {
            get { return _itemDatas; }
            set
            {
                if (_itemDatas != value)
                {
                    _itemDatas = value;
                    OnPropertyChanged("ItemDatas");
                }
            }
        }
        private ObservableCollection<DesignerItem> _selectedItems;
        public ObservableCollection<DesignerItem> SelectedItems
        {
            get
            {
                if (_selectedItems == null) _selectedItems = new ObservableCollection<DesignerItem>();
                return _selectedItems;
            }
            set
            {
                if (_selectedItems != value)
                {
                    _selectedItems = value;
                    OnPropertyChanged("SelectedItems");
                }
            }
        }
        public DiagramControl DiagramControl { get; set; }
        private ObservableCollection<DesignerItem> _designerItems;

        public ObservableCollection<DesignerItem> DesignerItems
        {
            get { return _designerItems; }
            set { _designerItems = value; OnPropertyChanged("DesignerItems"); }
        }
        public MainViewModel()
        {
            //InitData();
            PropertyChanged += (s, e) =>
            {
                //switch (e.PropertyName)
                //{
                //    case "SelectedItems":
                //        if (SelectedItems != null)
                //        {
                //            if (SelectedItems.Count > 1 && SelectedItems.FirstOrDefault() != null)
                //            {
                //                SelectedItem = null;
                //            }
                //            else
                //            {
                //                SelectedItem = SelectedItems.FirstOrDefault();
                //            }

                //            SelectedItems.CollectionChanged += (d, args) =>
                //            {
                //                if (args.Action == NotifyCollectionChangedAction.Add)
                //                {
                //                    var n = args.NewItems.Cast<DesignerItem>().ToList();
                //                    SelectedItem = SelectedItems.Count() > 1 ? null : n.FirstOrDefault();
                //                }
                //                if (args.Action == NotifyCollectionChangedAction.Reset)
                //                {
                //                    SelectedItem = null;
                //                }
                //            };
                //        }
                //        break;
                //}
            };


        }
        //可用框架中的消息实现
        public void InitData()
        {
            //ItemDatas = new ObservableCollection<ItemDataBase>()
            //{
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",5d,5d),
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",0,2),
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",0,1),
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f933","d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "Item-3", "3",0,3),
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f934","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-4", "4",0,4),
            //    new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f935","d342e6d4-9e76-4a21-b4f8-41f8fab0f933", "Item-5\r\nasdf", "5",0,5)
            //};
            ItemDatas = new List<ItemData>()
            {
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",5d,5d),
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",0,2),
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",0,1),
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f933","d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "Item-3", "3",0,3),
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f934","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-4", "4",0,4),
                new ItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f935","d342e6d4-9e76-4a21-b4f8-41f8fab0f933", "Item-5\r\nasdf", "5",0,5)
            };

            DesignerItems = DesignerItemOperator.Default.InitDesignerItems(ItemDatas);

        }

        #region Command


        public bool EnableCommand()
        {
            if (SelectedItem != null)
            {
                return !SelectedItem.DiagramControl.IsOnEditing;
            }
            return false;
        }
        public ICommand AddSiblingCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    var parentId = SelectedItem.ItemParentId;

                    var id = parentId.Equals(string.Empty) ? SelectedItem.ItemId : parentId;
                    ItemDatas.Add(new ItemData(Guid.NewGuid().ToString(), id, GetText(), "", 0, double.MaxValue));
                }, EnableCommand);
            }
        }
        public ICommand AddAfterCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    ItemDatas.Add(new ItemData(Guid.NewGuid().ToString(), SelectedItem.ItemId, GetText(), "", 0, double.MaxValue));

                }, EnableCommand);
            }
        }
        public ICommand DeleteCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    var item = SelectedItem.Data;
                    if (item != null)//&& item.ItemParentId != string.Empty)
                    {
                        ItemDatas.Remove(item as ItemData);
                    }
                }, EnableCommand);
            }
        }
        public ICommand UpCommand
        {
            get
            {
                return new RelayCommand(() =>
                    {
                        if (SelectedItem == null) return;
                        SelectedItem.DiagramControl.DiagramManager.SelectUpDown(SelectedItem, true);
                    }, EnableCommand);
            }
        }
        public ICommand DownCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    SelectedItem.DiagramControl.DiagramManager.SelectUpDown(SelectedItem, false);

                }, EnableCommand);
            }
        }

        public ICommand RightCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    SelectedItem.DiagramControl.DiagramManager.SelectRightLeft(SelectedItem, true);
                }, EnableCommand);
            }
        }

        public ICommand LeftCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItem == null) return;
                    SelectedItem.DiagramControl.DiagramManager.SelectRightLeft(SelectedItem, false);
                }, EnableCommand);
            }
        }

        #endregion

        private string GetText() { return "Item-" + ((ItemDatas == null) ? "0" : (ItemDatas.Count().ToString())); }
        public ICommand CollapseCommand { get { return new RelayCommand(() => { if (DiagramControl != null) DiagramControl.DiagramManager.CollapseAll(); }); } }
        public ICommand ExpandCommand { get { return new RelayCommand(() => { if (DiagramControl != null) DiagramControl.DiagramManager.ExpandAll(); }); } }

        public ICommand CutCommand { get { return new RelayCommand(() => { }, () => SelectedItems.Any()); } }
        public ICommand CopyCommand { get { return new RelayCommand(() => { }, () => SelectedItems.Any()); } }
        public ICommand PasteCommand { get { return new RelayCommand(() => { }, () => SelectedItems.Any()); } }


        public ICommand LoadData
        {
            get
            {
                return new RelayCommand(InitData);
            }
        }

        //public ICommand SaveCommand { get { return new RelayCommand(_diagramManager.Save); } }
    }
}
