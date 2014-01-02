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
        public static object GetProperty(object target, string properyName)
        {
            return target.GetType().GetProperty(properyName).GetValue(target);
        }
        public static void SetProperty(object target, string properyName, object value)
        {
            target.GetType().GetProperty(properyName).SetValue(target, value);
        }
        
        public static string PropertyName<T>(Expression<Func<T, object>> expression)
        {
            return ((MemberExpression)expression.Body).Member.Name;
        }
    }
}
