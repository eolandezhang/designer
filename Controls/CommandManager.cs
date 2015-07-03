/* ============================================================================== 
* 类名称：CommandManager 
* 类描述： 
* 创建人：eolandecheung 
* 创建时间：2015/7/3 9:05:54 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using DiagramDesigner.MVVM;

namespace DiagramDesigner.Controls
{
    public class CommandManager
    {
        private DiagramManager _diagramManager;
        public CommandManager(DiagramManager diagramManager)
        {
            _diagramManager = diagramManager;
        }
        public ICommand AddSiblingCommand { get { return new RelayCommand(_diagramManager.AddSibling); } }
        public ICommand AddAfterCommand { get { return new RelayCommand(_diagramManager.AddAfter); } }
        public ICommand RemoveCommand { get { return new RelayCommand(_diagramManager.Remove); } }
        public ICommand CollapseCommand { get { return new RelayCommand(_diagramManager.CollapseAll); } }
        public ICommand ExpandCommand { get { return new RelayCommand(_diagramManager.ExpandAll); } }
        public ICommand ReloadCommand { get { return new RelayCommand(_diagramManager.GenerateDesignerItems); } }
        public ICommand CopyCommand { get { return new RelayCommand(_diagramManager.Copy); } }
        public ICommand PasteCommand { get { return new RelayCommand(_diagramManager.Paste); } }
        public ICommand SaveCommand { get { return new RelayCommand(_diagramManager.Save); } }
    }
}
