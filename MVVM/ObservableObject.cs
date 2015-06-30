/* ============================================================================== 
* 类名称：ObservableObject 
* 类描述： 
* 创建人：eolandecheung 
* 创建时间：2015/6/30 15:22:46 
* 修改人： 
* 修改时间： 
* 修改备注： 
* @version 1.0 
* ==============================================================================*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DiagramDesigner.MVVM
{

    [Serializable]
    public class ObservableObject : INotifyPropertyChanged
    {
        //[NonSerialized]
        Hashtable valueTable = new Hashtable();

        [NonSerialized]
        PropertyChangedEventHandler propertyChanged;

        [NonSerialized]
        bool suppressPropertyChanged = false;

        protected internal virtual Hashtable ValueTable
        {
            get { return valueTable; }
        }

        public virtual event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                propertyChanged = (PropertyChangedEventHandler)
                    System.Delegate.Combine(propertyChanged, value);
            }
            remove
            {
                propertyChanged = (PropertyChangedEventHandler)
                    System.Delegate.Remove(propertyChanged, value);
            }
        }


        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual internal void OnPropertyChanged(string propertyName)
        {
            if (!suppressPropertyChanged && propertyChanged != null)
                propertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        protected internal virtual T Get<T>(string property, T defaultValue = default(T))
        {
            if (valueTable.ContainsKey(property))
                return (T)valueTable[property];
            valueTable[property] = defaultValue;
            return defaultValue;
        }

        protected internal virtual bool Set<T>(string property, T newValue)
        {
            T oldValue = Get<T>(property);

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;



            valueTable[property] = newValue;

            OnPropertyChanged(property);
            return true;
        }

        protected internal virtual T Get<T>(Expression<Func<T>> propertyExpression, T defaultValue = default(T))
        {
            var property = (propertyExpression.Body as MemberExpression).Member.Name;
            return Get(property, defaultValue);
        }

        protected internal virtual bool Set<T>(Expression<Func<T>> propertyExpression, T newValue)
        {
            var property = (propertyExpression.Body as MemberExpression).Member.Name;

            return Set(property, newValue);
        }



        public virtual void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        public virtual void ResumePropertyChanged()
        {
            suppressPropertyChanged = false;
        }

        public virtual void SuppressPropertyChanged()
        {
            suppressPropertyChanged = true;
        }
    }

}
