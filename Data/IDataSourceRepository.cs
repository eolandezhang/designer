using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiagramDesigner
{
    public interface IDataSourceRepository
    {
        List<IItemData> DataSources { get; set; }
        List<DesignerItem> DesignerItems { get; set; }
        DesignerItem InitDesignerItems(DiagramControl diagramControl);
        void UpdateDataSources();
    }
}
