using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Polymod.Fluent;

namespace Polymod.Tests
{
    public static class Helper
    {
        public static object GetProperty<T, TValue>(IProxy<T> proxy, Expression<Func<T, TValue>> propertyExpression)
        {
            var propertyInfo = GetPropertyInfo(propertyExpression);
            var proxyPropertyInfo = proxy.GetType().GetProperty(propertyInfo.Name);
            return proxyPropertyInfo.GetValue(proxy);
        }

        public static object GetProperty(object target, string properyName)
        {
            return target.GetType().GetProperty(properyName).GetValue(target);
        }
        public static void SetProperty(object target, string properyName, object value)
        {
            target.GetType().GetProperty(properyName).SetValue(target, value);
        }
        
        private static PropertyInfo GetPropertyInfo(LambdaExpression expression)
        {
            return (PropertyInfo)((MemberExpression)expression.Body).Member;
        }
    }
}
