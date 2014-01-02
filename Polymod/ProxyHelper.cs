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
    }
}
