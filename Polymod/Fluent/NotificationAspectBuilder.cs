using Polymod.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Fluent
{
    /// <summary>
    /// Raises the PropertyChanged Event
    /// </summary>
    public interface IRaisePropertyChanged
    {
        void RaisePropertyChanged(string propertyName);
    }

    public class NotificationAspectBuilder<TSource> : IAspectBuilder
    {
        private readonly Dictionary<string, List<string>> _affectedChangeRegister = new Dictionary<string, List<string>>();

        public void AddNotification(string sourcePropertyName, string affectedPropertyName)
        {
            List<string> tmp;
            if (!_affectedChangeRegister.TryGetValue(sourcePropertyName, out tmp))
            {
                tmp = new List<string>();
                _affectedChangeRegister[sourcePropertyName] = tmp;
            }

            if (!tmp.Contains(affectedPropertyName)) tmp.Add(affectedPropertyName);
        }


        public void Build(TypeBuilder typeBuilder, IDictionary<string, object> aspectState)
        {
            typeBuilder.Implement<INotifyPropertyChanged>();
            typeBuilder.Implement<IRaisePropertyChanged>();

            var eventField = typeBuilder.ExplicitlyImplement(Ex.Event<INotifyPropertyChanged>());
            
            //Create the RaisePropertyChanged Method.
            var mb = typeBuilder.ExplicitlyImplement(Ex.Method<IRaisePropertyChanged>(m => m.RaisePropertyChanged("")));
            
            var il = mb.GetILGenerator();

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, eventField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, Ex.Method(() => ILHelper.RaisePropertyChanged(null, null, "")));
            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);


            //TODO: To get a InterceptorRegistry means a dependency on the InterceptorAspectBuilder... would be nice to be able to build a default one?
            //Or at the very least, throw a MissingDependency sort of exception..
            aspectState.Get(States.InterceptorRegistry).Wrap((name, i) => new NotifyInterceptor(name, i));
        }
    }

    public class NotifyInterceptor : IPropertyInterceptor
    {
        private IPropertyInterceptor _inner;
        private string _name;

        public NotifyInterceptor(string name, IPropertyInterceptor inner)
        {
            _inner = inner;
            _name = name;
        }

        public void Set(IProxy proxy, object propertyValue)
        {
            if (EqualityComparer.Default.Equals(_inner.Get(proxy), propertyValue)) return;
            _inner.Set(proxy, propertyValue);
            ((IRaisePropertyChanged)proxy).RaisePropertyChanged(_name);
        }

        public object Get(IProxy proxy)
        {
            return _inner.Get(proxy);
        }
    }


    public class NotificationAspect : IAspect
    {
        public void Bind(IProxy proxy)
        {

        }
    }

}
