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
        public readonly static TypedKey<ProxyCache> ProxyCache = CreateKey<ProxyCache>("ProxyCache");

        
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
