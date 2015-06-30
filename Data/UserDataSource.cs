/* ============================================================================== 
* 类名称：UserDataSource 
* 类描述： 测试用数据源
* 创建人：eolandecheung 
* 创建时间：2015/6/26 14:20:03 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/

using System;
using System.Collections.Generic;
using System.Linq;


namespace DiagramDesigner
{
    public class UserDataSourceRepository : IDataSourceRepository
    {
        public List<IItemData> DataSources { get; set; }
        public List<DesignerItem> DesignerItems { get; set; }

        public DesignerItem InitDesignerItems(DiagramControl diagramControl)
        {
            DesignerItems = new List<DesignerItem>();
            DataSources = new List<IItemData>()
            {
                new CustomItemData(diagramControl,"d342e6d4-9e76-4a21-b4f8-41f8fab0f93c","","Root","Root　Item",false,false),
                new CustomItemData(diagramControl,"d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",false,false,2),
                new CustomItemData(diagramControl,"d342e6d4-9e76-4a21-b4f8-41f8fab0f932","d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",false,false,1)
            };
            var root = DataSources.FirstOrDefault(x => x.ParentId == Guid.Empty);
            if (root == null) return null;
            var rootDesignerItem = diagramControl.CreateRootItem(root.Id, root);
            DesignerItems.Clear();
            DesignerItems.Add(rootDesignerItem);
            CreateChildDesignerItem(diagramControl, rootDesignerItem);
            return rootDesignerItem;
        }

        public void CreateChildDesignerItem(DiagramControl diagramControl, DesignerItem parentDesignerItem)
        {
            var child = DataSources.Where(x => x.ParentId == parentDesignerItem.ID && !x.Removed);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = diagramControl.CreateChildItem(parentDesignerItem.ID, userDataSource.Id, userDataSource);
                DesignerItems.Add(childDesignerItem);
                CreateChildDesignerItem(diagramControl, childDesignerItem);
            }
        }

        public void UpdateDataSources()
        {
            DataSources.Clear();
            foreach (var item in DesignerItems)
            {
                DataSources.Add(item.Data as CustomItemData);
            }
        }



    }
}
