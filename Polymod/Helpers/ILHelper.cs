using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Helpers
{
    /// <summary>
    /// IL helper, IL-Generated methods will call methods on the IL Helper to minimize the amount of IL code generation.
    /// </summary>
    public static class ILHelper
    {
        public static void RaisePropertyChanged(PropertyChangedEventHandler handler, object sender, string propertyName)
        {
            var localHandler = handler;
            if (localHandler != null) localHandler(sender, new PropertyChangedEventArgs(propertyName));
        }

        public static object GetPropertyValue(this IProxy proxy, string invokerName)
        {
            var interceptors = proxy.State.Get(States.InterceptorRegistry);
            var proxyBuilder = proxy.State.Get(States.ProxyBuilder);

            var invoker = interceptors[invokerName];
            var result = invoker.Get(proxy);
            return result;
        }

        public static void SetPropertyValue(this IProxy proxy, string invokerName, object value)
        {
            var interceptors = proxy.State.Get(States.InterceptorRegistry);
            var invoker = interceptors[invokerName];
            invoker.Set(proxy, value);
        }

        public static T Convert<T>(object value)
        {
            return (T)value;
        }
    }
}
