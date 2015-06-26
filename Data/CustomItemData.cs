using System;
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
                _yIndex = value;
                OnPropertyChanged("YIndex");
            }
        }
        private string _text;
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged("Text");
                DiagramControl.BindData();
            }
        }
        #endregion

        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                _desc = value;
                OnPropertyChanged("Desc");
                DiagramControl.BindData();
            }
        }

        public DiagramControl DiagramControl { get; set; }

        #region Constructors

        public CustomItemData(DiagramControl diagramControl, string text, double yIndex = double.MaxValue)
        {
            DiagramControl = diagramControl;
            Text = text;
            YIndex = yIndex;
        }
        public CustomItemData(DiagramControl diagramControl, string text, string desc, double yIndex = double.MaxValue)
        {
            DiagramControl = diagramControl;
            Text = text;
            Desc = desc;
            YIndex = yIndex;
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




    }
}
