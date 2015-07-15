using DiagramDesigner.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace DiagramDesigner.Data
{

    public class ItemDataBase : INotifyPropertyChanged, ICloneable
    {
        private bool Suppress /*阻止通知*/{ get; set; }
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
        //public Guid Id { get; set; }
        //private Guid _parentId;
        //public Guid ParentId
        //{
        //    get { return _parentId; }
        //    set
        //    {
        //        if (_parentId == value) return;

        //        _parentId = value;
        //        OnPropertyChanged("ParentId");

        //        if (!Added)
        //        {
        //            Changed = true;
        //        }
        //    }
        //}
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
                    Changed = true;
                    if (DiagramControl != null)
                    {
                        DiagramControl.DiagramManager.ArrangeWithRootItems();
                    }
                }
                OnPropertyChanged("Text");
            }
        }
        private bool _changed;
        public bool Changed
        {
            get { return _changed; }
            set
            {
                if (_changed == value) return;
                if (Added) value = false;
                _changed = value;
                OnPropertyChanged("Changed");
            }
        }
        public bool Added { get; set; }
        public bool Removed { get; set; }
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


        public ItemDataBase(string id)
        {
            ItemId = id;
        }
        public ItemDataBase() : this(Guid.NewGuid().ToString()) { }
        public ItemDataBase(
            string id,
            string parentId,
            string text,
            bool added,
            bool removed,
            double xIndex,
            double yIndex)
        {
            ItemId = id;
            ItemParentId = parentId;
            Text = text;
            XIndex = xIndex;
            YIndex = yIndex;
            Changed = false;
            Added = added;
            Removed = removed;
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
            var item = new ItemDataBase();
            item.Suppress = true;/*复制时阻止通知*/
            item.DiagramControl = DiagramControl;
            item.ItemId = Guid.NewGuid().ToString();
            item.ItemParentId = ItemParentId;
            item.Text = Text + "-" + "Copy";
            item.Changed = false;
            item.Added = false;
            item.Removed = false;
            item.XIndex = 0;
            item.YIndex = 0;
            item.Suppress = false;
            return item;
        }
    }
}
