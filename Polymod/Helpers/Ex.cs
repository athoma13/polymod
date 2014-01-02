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
    public static class Ex<T>
    {
        public static PropertyInfo Property<TValue>(Expression<Func<T, TValue>> expression) { return (PropertyInfo)((MemberExpression)expression.Body).Member; }
    }

    public static class Ex
    {
        public static MethodInfo Method(Expression<Action> expression) { return ((MethodCallExpression)expression.Body).Method; }
        public static ConstructorInfo Constructor(Expression<Func<object>> expression) { return ((NewExpression)expression.Body).Constructor; }
        public static MethodInfo Method<T>(Expression<Action<T>> expression) { return ((MethodCallExpression)expression.Body).Method; }
        

        /// <summary>
        /// Gets the Single Event defined on this Type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static EventInfo Event<T>()
        {
            return FindEvent(typeof(T), ev => true);
        }

        /// <summary>
        /// Gets the single Event defined with the delegate Type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        public static EventInfo Event<T>(Type delegateType)
        {
            if (delegateType == null) throw new ArgumentNullException("delegateType");
            return FindEvent(typeof(T), ev => ev.EventHandlerType == delegateType);
        }

        /// <summary>
        /// Gets the single Event defined with the event name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static EventInfo Event<T>(string eventName)
        {
            if (eventName == null) throw new ArgumentNullException("eventName");
            return FindEvent(typeof(T), ev => ev.Name == eventName);
        }

        /// <summary>
        /// Gets the single Event defined with the event name and delegate type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="delegateType"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static EventInfo Event<T>(Type delegateType, string eventName)
        {
            if (eventName == null) throw new ArgumentNullException("eventName");
            if (delegateType == null) throw new ArgumentNullException("delegateType");
            return FindEvent(typeof(T), ev => ev.Name == eventName && ev.EventHandlerType == delegateType);
        }
        private static EventInfo FindEvent(Type type, Func<EventInfo, bool> predicate)
        {
            var results = type.GetEvents().Where(predicate).ToArray();
            if (results.Length == 0) throw new InvalidOperationException("No results Found.");
            if (results.Length > 1) throw new InvalidOperationException("Multiple results Found.");
            return results[0];
        }
    }


    public static class Arg
    {
        public static T OfType<T>() { return default(T); }
        public static string String { get { return null; } }
        public static object Object { get { return null; } }
        public static int Int { get { return 0; } }
        public static ArgArray Array { get { return new ArgArray(); } }
    }

    public class ArgArray
    {
        public static T[] OfType<T>() { return new T[0]; }
        public static string[] String { get { return null; } }
        public static object[] Object { get { return null; } }
        public static int[] Int { get { return null; } }
    }


}
