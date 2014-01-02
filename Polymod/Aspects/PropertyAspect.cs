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
            var interceptor = GetInterceptor(getter);
            return (TValue)interceptor.Get(_proxy);
        }

        public void Set<TValue>(Expression<Func<TTarget, TValue>> setter, TValue value)
        {
            var interceptor = GetInterceptor(setter);
            interceptor.Set(_proxy, value);
        }

        public IList<IProxy<TValue>> GetCollection<TValue>(Expression<Func<TTarget, IEnumerable<TValue>>> getter)
        {
            return PrivateGetCollection<TValue>(getter);
        }
        private IList<IProxy<TValue>> PrivateGetCollection<TValue>(LambdaExpression expression)
        {
            var interceptor = GetInterceptor(expression);
            var collection = interceptor.Get(_proxy) as IList<object>;
            if (collection == null) return null;
            return new CollectionWrapper<IProxy<TValue>>(collection);
        }


        private IPropertyInterceptor GetInterceptor(LambdaExpression expression)
        {
            var interceptors = _proxy.State.Get(States.InterceptorRegistry);
            var result = interceptors[ExpressionHelper.GetPropertyName(expression)];
            return result;
        }

        void IAspect.Bind(IProxy proxy)
        {
            _proxy = proxy;
        }
    }


    
}
