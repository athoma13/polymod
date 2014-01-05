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

    public class FluentNotificationAspectBuilder : IFluentAspectBuilder<FluentNotificationNode>
    {
        private NotificationAspectBuilder _builder;
        public IAspectBuilder Builder { get { return _builder; } }

        public FluentNotificationAspectBuilder()
        {
            _builder = new NotificationAspectBuilder();
        }

        public FluentNotificationNode CreateFluentNode(ProxyBuilder proxyBuilder)
        {
            return new FluentNotificationNode(_builder);
        }
    }

    public class FluentNotificationNode
    {
        private readonly NotificationAspectBuilder _builder;

        internal FluentNotificationNode(NotificationAspectBuilder builder)
        {
            _builder = builder;
        }

        public FluentNotificationSourceNode<TSource> For<TSource>()
        {
            return new FluentNotificationSourceNode<TSource>(_builder);
        }

    }

    public class FluentNotificationSourceNode<TSource>
    {
        private readonly NotificationAspectBuilder _builder;

        internal FluentNotificationSourceNode(NotificationAspectBuilder builder)
        {
            _builder = builder;
        }

        public FluentNotificationSourceNode<TSource2> For<TSource2>()
        {
            return new FluentNotificationSourceNode<TSource2>(_builder);
        }

        public FluentNotificationSourceNode<TSource> AddNotification<TValue>(Expression<Func<TSource, TValue>> source, Expression<Func<TSource, TValue>> affects)
        {
            _builder.AddNotification(typeof(TSource), ExpressionHelper.GetPropertyName(source), ExpressionHelper.GetPropertyName(affects));
            return this;
        }
    }

    public class NotificationAspectBuilder : IAspectBuilder
    {
        private readonly NotificationRegister _notificationRegister = new NotificationRegister();

        public void AddNotification(Type type, string sourcePropertyName, string affectedPropertyName)
        {
            _notificationRegister.AddRegistration(type, sourcePropertyName, affectedPropertyName);
        }


        public void Build(TypeBuilder typeBuilder, StateBag aspectState)
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
            aspectState.Add(States.NotificationRegister, _notificationRegister);
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

            if (proxy.Target == null) return;
            var targetType = proxy.Target.GetType();
            var proxyAsRaiseProperty = proxy as IRaisePropertyChanged;
            if (proxyAsRaiseProperty == null) return;

            var notificationRegister = proxy.State.Get(States.NotificationRegister);
            using (var notifyScope = NotifyScope.Create())
            {
                foreach (var affectedProperty in notificationRegister.GetAffectedProperties(targetType, _name, true))
                {
                    notifyScope.NotifyChanged(proxyAsRaiseProperty, affectedProperty);
                }
            }
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
