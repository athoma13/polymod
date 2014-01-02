using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public interface IInvoker
    {
        object GetValue(IModel target);
        void SetValue(IModel target, object value);
        Type ReturnType { get; }
        bool IsReadonly { get; }
        string Name { get; }
        IInvoker Clone();
    }

    public interface IModel
    {
        void Initialize(object value, IDictionary<string, object> state);
        object Get(string name);
        void Set(string name, object value);
        object GetValue();
        IDictionary<string, object> ModelState { get; }
        object Synclock { get; }
    }

    public interface IModel<T> : IModel
    {
        new T GetValue();
    }

}
