using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DiagramDesigner
{
    public interface IItemData : INotifyPropertyChanged
    {
        string Text { get; set; }
        double YIndex { get; set; }
        bool Changed { get; set; }
    }
}
