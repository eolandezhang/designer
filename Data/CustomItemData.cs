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
using System.Linq;

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

                if (_desc != "")
                {
                    Changed = true;
                }
            }
        }

        #region Constructors
        public CustomItemData(string id)
            : base(id)
        {
            ItemId = id;
        }
        
        public CustomItemData(
            string id,
            string parentId,
            string text,
            string desc,
            double xIndex,
            double yIndex)
            : base(
            id,
            parentId,
            text,
            xIndex,
            yIndex)
        {
            Desc = desc;
        }

        #endregion
    }
}
