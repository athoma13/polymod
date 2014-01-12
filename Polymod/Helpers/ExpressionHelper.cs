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
    static internal class ExpressionHelper
    {
        private class PropertyNameVisitor : ExpressionVisitor
        {
            private string _fieldOrPropertyName;

            /// <summary>
            /// Gets FieldOrPropertyName
            /// </summary>
            public string FieldOrPropertyName
            {
                get { return _fieldOrPropertyName; }
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Member.MemberType == MemberTypes.Property || node.Member.MemberType == MemberTypes.Field)
                {
                    _fieldOrPropertyName = node.Member.Name;
                    //Do not recursively visit (found what we were looking for already), just return the node.
                    return node;
                }
                return base.VisitMember(node);
            }
        }

        public static MethodInfo GetMethod(LambdaExpression expression)
        {
            return null;
        }


        public static string GetPropertyName(LambdaExpression expression)
        {
            var propertyNameVisitor = new PropertyNameVisitor();
            propertyNameVisitor.Visit(expression);
            return propertyNameVisitor.FieldOrPropertyName;
        }

        public static string GetMethodName(LambdaExpression expression)
        {
            var methodCall = expression.Body as MethodCallExpression;
            if (methodCall == null) throw new ArgumentException("expression");
            return methodCall.Method.Name;
        }

        public static IPropertyInterceptor CreateInterceptor(PropertyInfo property)
        {
            return new DummyInterceptor(property);
        }

        public static Delegate CreateSetter(PropertyInfo propertyInfo)
        {
            var parameter = Expression.Parameter(propertyInfo.DeclaringType);
            var value = Expression.Parameter(propertyInfo.PropertyType);

            var property = Expression.Property(parameter, propertyInfo);
            var assign = Expression.Assign(property, value);
            var setter = Expression.Lambda(typeof(Action<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType), assign, parameter, value);
            return setter.Compile();
        }

        public static Delegate CreateGetter(PropertyInfo propertyInfo)
        {
            var parameter = Expression.Parameter(propertyInfo.DeclaringType);
            var property = Expression.Property(parameter, propertyInfo);
            var getter = Expression.Lambda(typeof(Func<,>).MakeGenericType(propertyInfo.DeclaringType, propertyInfo.PropertyType), property, parameter);
            return getter.Compile();
        }


        private class DummyInterceptor : IPropertyInterceptor
        {
            Delegate _setter;
            Delegate _getter;

            public DummyInterceptor(PropertyInfo propertyInfo)
            {
                if (propertyInfo.CanRead) _getter = CreateGetter(propertyInfo);
                if (propertyInfo.CanWrite) _setter = CreateSetter(propertyInfo);
            }

            public void Set(IProxy proxy, object propertyValue)
            {
                if (_setter == null) throw new InvalidOperationException("Property cannot be set");
                _setter.DynamicInvoke(proxy.Target, propertyValue);
            }

            public object Get(IProxy proxy)
            {
                if (_getter == null) throw new InvalidOperationException("Cannot get property");
                return _getter.DynamicInvoke(proxy.Target);
            }
        }
    }
}
