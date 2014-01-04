using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    /// <summary>
    /// Responsible for Building Proxies
    /// </summary>
    public class ProxyBuilder
    {
        private readonly List<IAspectBuilder> _aspectBuilders = new List<IAspectBuilder>();
        private readonly Dictionary<Type, ProxyTypeCachedEntry> _proxyTypeCache = new Dictionary<Type, ProxyTypeCachedEntry>();
        private readonly object synclock = new object();
        private volatile bool _isBuilt = false;

        public ProxyBuilder()
        {
            _aspectBuilders.Add(new BaseAspectBuilder());
        }

        public void AddBuilder(IAspectBuilder builder)
        {
            if (_isBuilt) throw new InvalidOperationException("ProxyBuilder has already been used to build types. Cannot add Builders.");
            _aspectBuilders.Add(builder);
        }

        public IProxy Build(object value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return Build(value, value.GetType());
        }

        public IProxy Build(object value, Type type)
        {
            //TODO: How to Guard against passing in non-proxy candidates in here?

            _isBuilt = true;
            ProxyTypeCachedEntry proxyTypeEntry;
            var targetType = type;

            //Ensure the ProxyType is created
            if (!_proxyTypeCache.TryGetValue(targetType, out proxyTypeEntry))
            {
                lock (synclock)
                {
                    if (!_proxyTypeCache.TryGetValue(targetType, out proxyTypeEntry))
                    {
                        if (IsProxyCandidate(targetType))
                        {
                            //Cache the Proxy Type.
                            Dictionary<string, object> proxyState;
                            var proxyType = MakeProxyType(targetType, out proxyState);
                            proxyTypeEntry = new ProxyTypeCachedEntry() { ProxyState = proxyState, Type = proxyType };
                        }
                        else
                        {
                            proxyTypeEntry = new ProxyTypeCachedEntry() { ProxyState = null, Type = typeof(NonCandidateProxy<>).MakeGenericType(targetType) };
                        }

                        _proxyTypeCache[targetType] = proxyTypeEntry;
                    }
                }
            }

            //TODO: Need to be able to recursively call Build (for Graphs...)

            //TODO: Think about Aspect dependencies. Should one aspect builder be allowed to build another Aspect because the current Aspect depends on it?

            //NOTE/TODO: proxyTypeEntry - proxyState is shared across all proxy instances (see below)... would do pass a copy of the Dictionary AND make sure that all initial state is Readonly objects... e.g. Create a IInjectableState, and call a Injet() 
            //so that each proxy has a copy of it's state rather than share a global one.


            //Create an instance of the ProxyType
            var instance = (IProxy)Activator.CreateInstance(proxyTypeEntry.Type, value, proxyTypeEntry.ProxyState);
            return instance;
        }

        public IProxy<T> Build<T>(T value)
        {
            return (IProxy<T>)Build(value, typeof(T));
        }

        private Type MakeProxyType(Type targetType, out Dictionary<string, object> proxyState)
        {
            var tb = new TypeBuilder(targetType);

            //Build the Aspect, and create the AspectState which will be injected into the proxy at initialization.
            proxyState = new Dictionary<string, object>();
            proxyState[States.ProxyBuilder] = this;

            foreach (var aspectBuilder in _aspectBuilders)
            {
                aspectBuilder.Build(tb, proxyState);
            }

            return tb.CreateProxyType();
        }

        public virtual bool IsProxyCandidate(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (!type.IsClass) return false;
            if (type == typeof(string)) return false;
            return true;
        }
    }

    internal class NonCandidateProxy<T> : IProxy<T>
    {
        public T Target
        {
            get;
            private set;
        }
        object IProxy.Target
        {
            get { return Target; }
        }

        public Dictionary<string, object> State
        {
            get;
            private set;
        }

        public NonCandidateProxy(T target, Dictionary<string, object> state)
        {
            Target = target;
            State = state;
        }
    }

}
