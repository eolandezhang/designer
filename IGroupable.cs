using System;

namespace DiagramDesigner
{
    public interface IGroupable
    {
        //Guid ID { get; }
        string ParentID { get; set; }
        string ItemId { get; set; }
        bool IsGroup { get; set; }
    }
}
