using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace DiagramDesigner
{
    public class MainViewModel : ObservableObject
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

        private ObservableCollection<ItemDataBase> _itemDatas;
        public ObservableCollection<ItemDataBase> ItemDatas
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
            get { return _selectedItems; }
            set
            {
                if (_selectedItems != value)
                {
                    var x = value.FirstOrDefault();
                    if (x != null)
                    {
                        SelectedItem = x;
                    }
                    _selectedItems = value;

                    OnPropertyChanged("SelectedItems");
                }
            }
        }

        public MainViewModel()
        {
            InitData();
        }

        //可用框架中的消息实现
        public void InitData()
        {
            ItemDatas = new ObservableCollection<ItemDataBase>()
            {
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false,5d,5d),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,0,2),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,0,1)
            };
        }

        public ICommand AddSiblingCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItems != null
                        && SelectedItems.Count == 1)
                    {
                        var selectedItem = SelectedItems.FirstOrDefault();
                        if (selectedItem != null)
                        {
                            var parentId = selectedItem.Data.ParentId;
                            var id = parentId.Equals(Guid.Empty) ? selectedItem.ID : parentId;
                            ItemDatas.Add(new CustomItemData(Guid.NewGuid(), id, GetText(), "", true, false, 0, double.MaxValue));
                        }

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
                    if (SelectedItems != null
                        && SelectedItems.Count == 1)
                    {
                        var selectedItem = SelectedItems.FirstOrDefault();
                        if (selectedItem != null)
                        {
                            ItemDatas.Add(new CustomItemData(Guid.NewGuid(), selectedItem.ID, GetText(), "", true, false, 0, double.MaxValue));
                        }

                    }
                });
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (SelectedItems != null
                        && SelectedItems.Count == 1)
                    {
                        var selectedItem = SelectedItems.FirstOrDefault();
                        if (selectedItem != null)
                        {
                            var item = selectedItem.Data;
                            if (item != null && item.ParentId != Guid.Empty)
                            {
                                //移除子节点

                                //移除节点
                                ItemDatas.Remove(item);
                                

                            }
                        }

                    }
                });
            }
        }

        private string GetText() { return "Item-" + ((ItemDatas == null) ? "0" : (ItemDatas.Count().ToString())); }




        //public ICommand AddAfterCommand { get { return new RelayCommand(_diagramManager.AddAfter); } }
        //public ICommand RemoveCommand { get { return new RelayCommand(_diagramManager.Remove); } }
        //public ICommand CollapseCommand { get { return new RelayCommand(_diagramManager.CollapseAll); } }
        //public ICommand ExpandCommand { get { return new RelayCommand(_diagramManager.ExpandAll); } }
        //public ICommand ReloadCommand { get { return new RelayCommand(_diagramManager.GenerateDesignerItems); } }
        //public ICommand CopyCommand { get { return new RelayCommand(_diagramManager.Copy); } }
        //public ICommand PasteCommand { get { return new RelayCommand(_diagramManager.Paste); } }
        //public ICommand SaveCommand { get { return new RelayCommand(_diagramManager.Save); } }
    }
}
