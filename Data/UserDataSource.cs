﻿/* ============================================================================== 
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DiagramDesigner
{
    public class UserDataSource
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Text { get; set; }
        public string Desc { get; set; }
        public double YIndex { get; set; }

        public UserDataSource(string id, string parentId, string text, string desc, double yIndex = 0)
        {
            Id = id;
            ParentId = parentId;
            Text = text;
            Desc = desc;
            YIndex = yIndex;
        }
    }

    public class UserDataSourceRepository
    {
        public static List<UserDataSource> DataSources;
        public static List<DesignerItem> DesignerItems;

        public static DesignerItem InitDesignerItems(DiagramControl diagramControl)
        {
            DesignerItems = new List<DesignerItem>();
            DataSources = new List<UserDataSource>()
            {
                new UserDataSource("d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "", "Root", "root item",0),
                new UserDataSource("d342e6d4-9e76-4a21-b4f8-41f8fab0f931", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-1", "1",2),
                new UserDataSource("d342e6d4-9e76-4a21-b4f8-41f8fab0f932", "d342e6d4-9e76-4a21-b4f8-41f8fab0f93c", "Item-2", "2",1)
            };
            var root = DataSources.FirstOrDefault(x => x.ParentId == "");
            if (root == null) return null;
            var rootDesignerItem = diagramControl.CreateRootItem(root.Id, new CustomItemData(diagramControl, root.Text, root.Desc, root.YIndex));
            DesignerItems.Clear();
            DesignerItems.Add(rootDesignerItem);
            CreateChildDesignerItem(diagramControl, rootDesignerItem);
            return rootDesignerItem;
        }

        public static void CreateChildDesignerItem(DiagramControl diagramControl, DesignerItem parentDesignerItem)
        {
            var child = DataSources.Where(x => x.ParentId == parentDesignerItem.ID.ToString());
            foreach (var userDataSource in child)
            {
                var childDesignerItem = diagramControl.CreateChildItem(parentDesignerItem.ID.ToString(), userDataSource.Id,
                    new CustomItemData(diagramControl, userDataSource.Text, userDataSource.Desc, userDataSource.YIndex));
                DesignerItems.Add(childDesignerItem);
                CreateChildDesignerItem(diagramControl, childDesignerItem);
            }
        }
    }
}
