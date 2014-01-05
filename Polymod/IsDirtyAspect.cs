using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public class IsDirtyAspect : IAspect
    {
        public readonly static TypedKey<IsDirtyManager> IsDirtyManager = new TypedKey<IsDirtyManager>("IsDirtyManager");
        private IProxy _proxy;

        public void Bind(IProxy proxy)
        {
            _proxy = proxy;
        }

        public bool IsDirty
        {
            get
            {
                return _proxy.State.Get(IsDirtyManager).IsDirty;
            }
        }
    }

    public class DirtyEntry
    {
        public object Value { get; set; }
        public IEqualityComparer EqualityComparer { get; private set; }
        public bool IsDirty { get; set; }
        public bool IsInitialized { get; set; }

        public DirtyEntry(IEqualityComparer equalityComparer)
        {
            EqualityComparer = equalityComparer;
        }
    }

    public class IsDirtyManager
    {
        public Dictionary<string, DirtyEntry> DirtyEntries { get; private set; }
        public bool IsDirty 
        {
            get { return DirtyEntries.Values.Any(d => d.IsDirty); } 
        }
        public IsDirtyManager()
        {
            DirtyEntries = new Dictionary<string, DirtyEntry>();
        }
    }


    public class IsDirtyAspectBuilder : IAspectBuilder
    {
        public void Build(TypeBuilder typeBuilder, StateBag proxyState)
        {
            var isDirtyManager = proxyState.GetOrCreateDefault(IsDirtyAspect.IsDirtyManager);
            var interceptorRegistry = proxyState.Get(States.InterceptorRegistry);

            interceptorRegistry.Wrap((name, interceptor) => new IsDirtyInterceptor(name, interceptor));
            
            foreach (var kp in interceptorRegistry.Data)
            {
                isDirtyManager.DirtyEntries[kp.Key] = new DirtyEntry(EqualityComparer.Default);
            }

        }
    }

    public class IsDirtyInterceptor : IPropertyInterceptor
    {
        private IPropertyInterceptor _interceptor;
        private string _name;

        public IsDirtyInterceptor(string name, IPropertyInterceptor interceptor)
        {
            _interceptor = interceptor;
            _name = name;
        }

        public void Set(IProxy proxy, object propertyValue)
        {
            var isDirtyManager = proxy.State.Get(IsDirtyAspect.IsDirtyManager);

            var entry = isDirtyManager.DirtyEntries[_name];
            if (!entry.IsInitialized)
            {
                entry.Value = _interceptor.Get(proxy);
                entry.IsInitialized = true;
            }

            entry.IsDirty = !entry.EqualityComparer.Equals(propertyValue, entry.Value);
            _interceptor.Set(proxy, propertyValue);
        }

        public object Get(IProxy proxy)
        {
            return _interceptor.Get(proxy);
        }
    }



}
