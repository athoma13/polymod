using Polymod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Aspects
{
    public class PropertyAspect<TTarget> : IAspect
    {
        IProxy _proxy;

        public TValue Get<TValue>(Expression<Func<TTarget, TValue>> getter)
        {
            return (TValue)Get(ExpressionHelper.GetPropertyName(getter));
        }

        public TValue Get<TValue>(string name)
        {
            return (TValue)Get(name);
        }

        public object Get(string name)
        {
            var interceptor = GetInterceptor(name);
            var result = interceptor.Get(_proxy);
            var resultAsProxy = result as IProxy;
            if (resultAsProxy != null) return resultAsProxy.Target;
            return result;
        }

        public void Set<TValue>(Expression<Func<TTarget, TValue>> setter, TValue value)
        {
            var interceptor = GetInterceptor(ExpressionHelper.GetPropertyName(setter));
            interceptor.Set(_proxy, value);
        }

        public void Set(string name, object value)
        {
            var interceptor = GetInterceptor(name);
            interceptor.Set(_proxy, value);
        }


        public IList<IProxy<TValue>> GetCollection<TValue>(Expression<Func<TTarget, IEnumerable<TValue>>> getter)
        {
            return PrivateGetCollection<TValue>(getter);
        }
        private IList<IProxy<TValue>> PrivateGetCollection<TValue>(LambdaExpression expression)
        {
            var interceptor = GetInterceptor(ExpressionHelper.GetPropertyName(expression));
            var collection = interceptor.Get(_proxy) as IList<object>;
            if (collection == null) return null;
            return new CollectionWrapper<IProxy<TValue>>(collection);
        }

        private IPropertyInterceptor GetInterceptor(string name)
        {
            var interceptors = _proxy.State.Get(States.InterceptorRegistry);
            var result = interceptors[name];
            return result;
        }


        void IAspect.Bind(IProxy proxy)
        {
            _proxy = proxy;
        }
    }


    
}
