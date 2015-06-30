using DiagramDesigner.Controls;
using System;
using System.ComponentModel;

namespace DiagramDesigner.Data
{

    public class ItemDataBase : INotifyPropertyChanged
    {
        public DiagramControl DiagramControl { get; set; }
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
                if (DiagramControl != null)
                {
                    DiagramControl.BindData();
                }
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
