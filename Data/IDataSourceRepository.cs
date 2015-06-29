using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiagramDesigner.Data
{
    public interface IDataSourceRepository
    {
        List<UserDataSource> DataSources { get; set; }
        List<DesignerItem> DesignerItems { get; set; }
        DesignerItem InitDesignerItems(DiagramControl diagramControl);
        void UpdateDataSources();
    }
}
