using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
/* ============================================================================== 
* 类名称：MainViewModel 
* 类描述： 
* 创建人：eolandecheung 
* 创建时间：2015/6/30 15:05:31 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/
using System.Collections.ObjectModel;
using System.Linq;

namespace DiagramDesigner
{
    public class MainViewModel : ObservableObject
    {
        private DesignerItem _selectedItem;
        public DesignerItem SelectedItem
        {
            get
            {
                if (SelectedItems != null && SelectedItems.Count() == 1)
                {
                    _selectedItem = SelectedItems.FirstOrDefault();
                }
                else
                {
                    _selectedItem = null;
                }
                return _selectedItem;
            }
            set { _selectedItem = value; OnPropertyChanged("SelectedItem"); }
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
            ItemDatas = new ObservableCollection<ItemDataBase>();
            SelectedItems = new ObservableCollection<DesignerItem>();
            InitData();
        }

        //可用框架中的消息实现
        public void InitData()
        {
            ItemDatas = new ObservableCollection<ItemDataBase>()
            {
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false,5d,5d),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,0,2),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,0,1)
            };
        }
    }
}
