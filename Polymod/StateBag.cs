using Polymod.Helpers;
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
    public class StateBag : Dictionary<string, object>
    {
        public StateBag()
        {
        }

        public StateBag(StateBag stateBag)
            : base(stateBag)
        {
        }

        public TValue Get<TValue>(TypedKey<TValue> key)
        {
            object result;
            if (TryGetValue(key.Key, out result)) return (TValue)result;
            throw new KeyNotFoundException(key + "");
        }

        public TValue GetOrDefault<TValue>(TypedKey<TValue> key)
        {
            object result;
            if (TryGetValue(key.Key, out result)) return (TValue)result;
            return default(TValue);
        }

        public TValue GetOrCreateDefault<TValue>(TypedKey<TValue> key)
            where TValue : new()
        {
            return GetOrCreateDefault(key, () => new TValue());
        }

        public bool TryGetValue<TValue>(TypedKey<TValue> key, out TValue result)
        {
            object tmpResult;
            if (TryGetValue(key.Key, out tmpResult))
            {
                result = (TValue)tmpResult;
                return true;
            }
            result = default(TValue);
            return false;
        }

        public TValue GetOrCreateDefault<TValue>(TypedKey<TValue> key, Func<TValue> creator)
        {
            object result;
            if (TryGetValue(key.Key, out result)) return (TValue)result;
            var newValue = creator();
            this[key.Key] = newValue;
            return newValue;
        }

        public StateBag Clone()
        {
            return new StateBag(this);
        }

    }
}
