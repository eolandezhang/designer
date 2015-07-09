using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Remoting.Channels;
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
                //if (_selectedItem != value)
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
                    _selectedItems = value;
                    OnPropertyChanged("SelectedItems");
                }
            }
        }

        public MainViewModel()
        {
            InitData();
            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case "SelectedItems":
                        if (SelectedItems != null)
                        {
                            if (SelectedItems.Count > 1 && SelectedItems.FirstOrDefault() != null)
                            {
                                SelectedItem = null;
                            }
                            else
                            {
                                SelectedItem = SelectedItems.FirstOrDefault();
                            }

                            SelectedItems.CollectionChanged += (d, args) =>
                            {
                                if (args.Action == NotifyCollectionChangedAction.Reset)
                                {
                                    SelectedItem = null;
                                }
                            };
                        }
                        break;
                }
            };
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
                }, EnableCommand);
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
                }, EnableCommand);
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
                                ItemDatas.Remove(item);
                            }
                        }
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
                        if (SelectedItems != null
                        && SelectedItems.Count == 1)
                        {
                            var selectedItem = SelectedItems.FirstOrDefault();
                            if (selectedItem != null)
                            {
                                selectedItem.DiagramControl.DiagramManager.SelectUp(selectedItem, true);
                            }

                        }
                    }, EnableCommand);
            }
        }
        public ICommand DownCommand
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
                            selectedItem.DiagramControl.DiagramManager.SelectUp(selectedItem, false);
                        }

                    }
                }, EnableCommand);
            }
        }

        public bool EnableCommand()
        {
            if (SelectedItems != null
                        && SelectedItems.Count == 1)
            {
                var selectedItem = SelectedItems.FirstOrDefault();
                if (selectedItem != null)
                {
                    return !selectedItem.DiagramControl.IsOnEditing;

                }
                return false;
            }
            return false;
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
