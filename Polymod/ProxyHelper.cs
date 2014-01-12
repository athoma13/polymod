using Polymod.Aspects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Polymod
{
    public static class ProxyHelper
    {

        public static TAspect GetAspect<TAspect>(this IProxy proxy) where TAspect : IAspect, new()
        {
            var aspect = new TAspect();
            aspect.Bind(proxy);
            return aspect;
        }

        public static TAspect GetAspect<TAspect, TTarget>(this IProxy<TTarget> proxy) where TAspect : IAspect, new()
        {
            var aspect = new TAspect();
            aspect.Bind(proxy);
            return aspect;
        }

        public static PropertyAspect<TTarget> GetPropertyAspect<TTarget>(this IProxy<TTarget> proxy)
        {
            var result = new PropertyAspect<TTarget>();
            ((IAspect)result).Bind(proxy);
            return result;
        }

        public static TValue Get<TTarget, TValue>(this IProxy<TTarget> proxy, Expression<Func<TTarget, TValue>> getter)
        {
            return GetPropertyAspect(proxy).Get(getter);
        }

        public static void Set<TTarget, TValue>(this IProxy<TTarget> proxy, Expression<Func<TTarget, TValue>> setter, TValue value)
        {
            GetPropertyAspect(proxy).Set(setter, value);
        }

        public static IList<IProxy<TValue>> GetCollection<TTarget, TValue>(this IProxy<TTarget> proxy, Expression<Func<TTarget, IEnumerable<TValue>>> getter)
        {
            return GetPropertyAspect(proxy).GetCollection(getter);
        }

        public static TValue Get<TTarget, TValue>(this IProxy<TTarget> proxy, IFormula<TTarget, TValue> formula)
        {
            return (TValue)GetPropertyAspect(proxy).Get(formula.Name);
        }

        public static void Set<TTarget, TValue>(this IProxy<TTarget> proxy, IFormula<TTarget, TValue> formula, TValue value)
        {
            GetPropertyAspect(proxy).Set(formula.Name, value);
        }


    }
}
