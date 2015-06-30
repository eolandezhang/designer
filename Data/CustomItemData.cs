using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace DiagramDesigner
{
    public class CustomItemData : IItemData
    {
        #region IItemData
        public Guid Id { get; set; }
        private Guid _parentId;
        public Guid ParentId
        {
            get { return _parentId; }
            set
            {
                if (_parentId == value) return;
                _parentId = value;
                OnPropertyChanged("ParentId");
                if (!Added)
                {
                    Changed = true;
                }
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
                OnPropertyChanged("Text");
                DiagramControl.BindData();
                if (_text != "")//初始化时，不标记为更改
                {
                    Changed = true;
                }
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
        #endregion

        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                if (_desc == value) return;
                _desc = value;
                OnPropertyChanged("Desc");
                DiagramControl.BindData();
                if (_desc != "")
                {
                    Changed = true;
                }
            }
        }

        public DiagramControl DiagramControl { get; set; }

        #region Constructors
        public CustomItemData(Guid id)
        {
            Id = id;
        }
        public CustomItemData(DiagramControl diagramControl, Guid id, Guid parentId, string text, string desc, bool added, bool removed, double yIndex = double.MaxValue)
        {
            DiagramControl = diagramControl;
            Id = id;
            ParentId = parentId;
            Text = text;
            Desc = desc;
            YIndex = yIndex;
            Changed = false;
            Added = added;
            Removed = removed;
        }
        public CustomItemData(DiagramControl diagramControl, Guid id, Guid parentId, string text, bool added, bool removed, double yIndex = double.MaxValue) : this(diagramControl, id, parentId, text, "", added, removed, yIndex) { }


        public CustomItemData(DiagramControl diagramControl, string id, string parentId, string text, string desc, bool added, bool removed, double yIndex = double.MaxValue) : this(diagramControl, new Guid(id), parentId == "" ? Guid.Empty : new Guid(parentId), text, desc, added, removed, yIndex) { }


        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            handler(this, new PropertyChangedEventArgs(name));
        }

        #endregion






    }
}
