/* ============================================================================== 
* 类名称：CustomItemData 
* 类描述： 
* 创建人：eolandecheung 
* 创建时间：2015/6 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using DiagramDesigner.Controls;

namespace DiagramDesigner.Data
{
    public class CustomItemData : ItemDataBase
    {
        private string _desc;
        public string Desc
        {
            get { return _desc; }
            set
            {
                if (_desc == value) return;
                _desc = value;
                OnPropertyChanged("Desc");
                if (DiagramControl != null)
                {
                    DiagramControl.DiagramManager.BindData();
                }
                if (_desc != "")
                {
                    Changed = true;
                }
            }
        }

        #region Constructors
        public CustomItemData(Guid id)
            : base(id)
        {
            Id = id;
        }
        public CustomItemData(
            Guid id,
            Guid parentId,
            string text,
            string desc,
            bool added,
            bool removed,
            double xIndex,
            double yIndex)
            : base(id, parentId, text, added, removed, xIndex, yIndex)
        {
            Desc = desc;
        }

        public CustomItemData(
            string id,
            string parentId,
            string text,
            string desc,
            bool added,
            bool removed,
            double xIndex,
            double yIndex)
            : this(
            new Guid(id),
            parentId == "" ? Guid.Empty : new Guid(parentId),
            text,
            desc,
            added,
            removed,
            xIndex,
            yIndex) { }

        #endregion
    }
}
