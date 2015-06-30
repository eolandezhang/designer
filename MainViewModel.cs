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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Input;
using DiagramDesigner.Controls;
using DiagramDesigner.Data;
using DiagramDesigner.MVVM;
using System.Windows;

namespace DiagramDesigner
{
    public class MainViewModel : ObservableObject
    {
        //private DesignerItem _selectedItem;
        //public DesignerItem SelectedItem
        //{
        //    get { return _selectedItem; }
        //    set
        //    {
        //        if (_selectedItem != value)
        //        {
        //            _selectedItem = value;

        //            EditorDataSource = value == null ? null : value.Data;
        //            OnPropertyChanged("SelectedItem");
        //        }
        //    }
        //}
        private ItemDataBase _editorDataSource;
        public ItemDataBase EditorDataSource
        {
            get { return _editorDataSource; }
            set
            {
                if (_editorDataSource != value)
                {
                    _editorDataSource = value;

                    OnPropertyChanged("EditorDataSource");
                }
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
        public MainViewModel()
        {
            ItemDatas = new ObservableCollection<ItemDataBase>();
            PropertyChanged += MainViewModel_PropertyChanged;
        }

        void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "SelectedItem":
                    {
                        //MessageBox.Show(SelectedItem.Data.Text);

                    }
                    break;
            }
        }
        //可用框架中的消息实现
        public void InitData(DiagramControl diagramControl)
        {
            ItemDatas = new ObservableCollection<ItemDataBase>()
            {
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,2),
                new CustomItemData("d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,1)
            };
        }
    }
}
