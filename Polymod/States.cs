using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public static class States
    {
        public readonly static TypedKey<InterceptorRegistry> InterceptorRegistry = CreateKey<InterceptorRegistry>("InterceptorRegistry");
        public readonly static TypedKey<ProxyBuilder> ProxyBuilder = CreateKey<ProxyBuilder>("ProxyBuilder");
        public readonly static TypedKey<NotificationRegister> NotificationRegister = CreateKey<NotificationRegister>("NotificationRegister");
        


        public static TValue Get<TValue>(this IDictionary<string, object> stateBag, TypedKey<TValue> key)
        {
            object result;
            if (stateBag.TryGetValue(key.Key, out result)) return (TValue)result;
            throw new KeyNotFoundException(key + "");
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, object> stateBag, TKey key)
        {
            object result;
            if (stateBag.TryGetValue(key, out result)) return (TValue)result;
            return default(TValue);
        }

        public static TValue GetOrCreateDefault<TValue>(this IDictionary<string, object> stateBag, TypedKey<TValue> key)
            where TValue : new()
        {
            return GetOrCreateDefault(stateBag, key, () => new TValue());
        }

        public static TValue GetOrCreateDefault<TValue>(this IDictionary<string, object> stateBag, TypedKey<TValue> key, Func<TValue> creator)
        {
            object result;
            if (stateBag.TryGetValue(key.Key, out result)) return (TValue)result;
            var newValue = creator();
            stateBag[key.Key] = newValue;
            return newValue;
        }

        private static TypedKey<TValue> CreateKey<TValue>(string key)
        {
            return new TypedKey<TValue>(key);
        }

    }



    public class TypedKey<TValue>
    {
        public string Key { get; private set; }

        public TypedKey(string key)
        {
            Key = key;
        }

        public static implicit operator string(TypedKey<TValue> value)
        {
            return value.Key;
        }
    }
}
