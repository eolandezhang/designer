﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DiagramDesigner
{
    public class CustomItemData : IItemData
    {
        #region IItemData

        private double _yIndex;
        public double YIndex
        {
            get { return _yIndex; }
            set
            {
                if (_yIndex != value)
                {
                    _yIndex = value;
                    OnPropertyChanged("YIndex");
                }
            }
        }
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                    DiagramControl.BindData();
                    if (_text != "")//初始化时，不标记为更改
                    {
                        Changed = true;
                    }
                }


            }
        }
        public bool Changed { get; set; }
        #endregion

        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                if (_desc != value)
                {
                    _desc = value;
                    OnPropertyChanged("Desc");
                    DiagramControl.BindData();
                    if (_desc != "")
                    {
                        Changed = true;
                    }
                }
            }
        }

        public DiagramControl DiagramControl { get; set; }

        #region Constructors

        public CustomItemData(DiagramControl diagramControl, string text, string desc, bool added, bool removed, double yIndex = double.MaxValue)
        {
            DiagramControl = diagramControl;
            Text = text;
            Desc = desc;
            YIndex = yIndex;
            Changed = false;
            Added = added;
            Removed = removed;
        }
        public CustomItemData(DiagramControl diagramControl, string text, bool added, bool removed, double yIndex = double.MaxValue)
            : this(diagramControl, text, "", added, removed, yIndex)
        {

        }

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








        private bool _added;
        public bool Added
        {
            get { return _added; }
            set { _added = value; }
        }
        private bool _removed;
        public bool Removed
        {
            get { return _removed; }
            set { _removed = value; }
        }
    }
}
