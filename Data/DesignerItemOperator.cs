using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using DiagramDesigner.Controls;

namespace DiagramDesigner.Data
{
    public class DesignerItemOperator
    {
        public static DesignerItemOperator Default;

        static DesignerItemOperator()
        {
            Default = new DesignerItemOperator();
        }
        #region Create items from datasource
        public ObservableCollection<DesignerItem> InitDesignerItems(List<ItemData> itemDatas)
        {
            ObservableCollection<DesignerItem> DesignerItems = new ObservableCollection<DesignerItem>();
            if (itemDatas == null) return null;
            var roots = itemDatas.Where(x => String.IsNullOrEmpty(x.ItemParentId)).ToList();
            if (!roots.Any()) return null;
            List<DesignerItem> rootDesignerItems = new List<DesignerItem>();
            foreach (var root in roots)
            {
                var rootDesignerItem = CreateRootItem(root.ItemId, root.Text, root);
                rootDesignerItems.Add(rootDesignerItem);
                DesignerItems.Add(rootDesignerItem);
                var list = CreateChildDesignerItem(rootDesignerItem, itemDatas);
                foreach (var designerItem in list)
                {
                    DesignerItems.Add(designerItem);
                }
            }
            return DesignerItems;
        }
        private List<DesignerItem> CreateChildDesignerItem(DesignerItem parentDesignerItem, List<ItemData> itemDatas)
        {
            List<DesignerItem> DesignerItems = new List<DesignerItem>();
            var child = itemDatas.Where(x => x.ItemParentId == parentDesignerItem.ItemId);
            foreach (var userDataSource in child)
            {
                var childDesignerItem = CreateChildItem(userDataSource.ItemId, parentDesignerItem.ItemId, userDataSource.Text, userDataSource);
                DesignerItems.Add(childDesignerItem);
                DesignerItems.AddRange(CreateChildDesignerItem(childDesignerItem, itemDatas));
            }
            return DesignerItems;
        }
        private DesignerItem CreateRootItem(string id, string text, object itemData, DiagramControl diagramControl = null)
        { return new DesignerItem(id, text, itemData, diagramControl); }
        private DesignerItem CreateChildItem(string id, string parentId, string text, object itemData, DiagramControl diagramControl = null)
        { return new DesignerItem(id, parentId, text, itemData, diagramControl); }
        #endregion

    }
}
