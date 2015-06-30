using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DiagramDesigner
{
    public interface IItemData : INotifyPropertyChanged
    {
        Guid Id { get; set; }
        Guid ParentId { get; set; }
        string Text { get; set; }
        double YIndex { get; set; }
        bool Changed { get; set; }
        bool Added { get; set; }
        bool Removed { get; set; }
    }
}
