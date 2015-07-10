/* ============================================================================== 
* 类名称：InputBindingManager 
* 类描述： 
* 创建人：eolandecheung 
* 创建时间：2015/7/10 16:07:42 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/
using System.Windows;
using System.Windows.Input;

namespace DiagramDesigner
{
    public class GlobalInputBindingManager
    {
        public static GlobalInputBindingManager Default = new GlobalInputBindingManager();
        static InputBindingCollection temp = new InputBindingCollection();
        public void Clear()
        {
            foreach (InputBinding inputBinding in Application.Current.MainWindow.InputBindings)
            {
                temp.Add(inputBinding);
            }
            Application.Current.MainWindow.InputBindings.Clear();
        }

        public void Recover()
        {
            if (temp.Count != 0)
            {
                foreach (InputBinding inputBinding in temp)
                {
                    Application.Current.MainWindow.InputBindings.Add(inputBinding);
                }
            }

        }

    }
}
