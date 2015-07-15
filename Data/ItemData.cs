using DiagramDesigner.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DiagramDesigner.Data
{

    public class ItemData : INotifyPropertyChanged, ICloneable
    {
        public bool Suppress /*阻止通知*/{ get; set; }
        public DiagramControl DiagramControl { get; set; }
        private string _itemId;
        public string ItemId
        {
            get
            {
                return _itemId;
            }
            set
            {
                _itemId = value;
                OnPropertyChanged("ItemId");
            }
        }
        private string _itemParentId;
        public string ItemParentId
        {
            get
            {
                return _itemParentId;
            }
            set
            {
                _itemParentId = value;
                OnPropertyChanged("ItemParentId");
            }
        }
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                _text = value;
                if (_text != "")//初始化时，不标记为更改
                {
                    if (DiagramControl != null)
                    {
                        DiagramControl.DiagramManager.ArrangeWithRootItems();
                    }
                }
                OnPropertyChanged("Text");
            }
        }
        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                if (_desc == value) return;
                _desc = value;
                OnPropertyChanged("Desc");
            }
        }
        private double _xIndex;
        public double XIndex
        {
            get { return _xIndex; }
            set
            {
                if (_xIndex.Equals(value)) return;
                _xIndex = value;
                OnPropertyChanged("XIndex");
            }
        }
        private double _yIndex;
        public double YIndex
        {
            get { return _yIndex; }
            set
            {
                if (_yIndex.Equals(value)) return;
                _yIndex = value;
                OnPropertyChanged("YIndex");
            }
        }


        public ItemData(string id)
        {
            ItemId = id;
        }
        public ItemData() : this(Guid.NewGuid().ToString()) { }
        public ItemData(
            string id,
            string parentId,
            string text,
            string desc,
            double xIndex,
            double yIndex)
        {
            ItemId = id;
            ItemParentId = parentId;
            Text = text;
            Desc = desc;
            XIndex = xIndex;
            YIndex = yIndex;

        }



        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            if (Suppress) return;
            var handler = PropertyChanged;
            if (handler == null) return;
            handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion



        public object Clone()
        {
            var item = new ItemData();
            item.Suppress = true;/*复制时阻止通知*/
            item.DiagramControl = DiagramControl;
            item.ItemId = Guid.NewGuid().ToString();
            item.ItemParentId = ItemParentId;
            item.Text = Text + "-" + "Copy";
            item.XIndex = 0;
            item.YIndex = 0;
            item.Suppress = false;
            return item;
        }
    }
}
