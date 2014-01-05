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
    public class ProxyCache
    {
        private readonly List<WeakReference<IProxy>> _cache = new List<WeakReference<IProxy>>();

        public void Add(IProxy proxy)
        {
            //TODO: Locking and cleaning up of null weak references... (Bloating will occur on the list).

            _cache.Add(new WeakReference<IProxy>(proxy));
        }

        public bool TryGet(object target, out IProxy proxy)
        {
            foreach(var item in _cache)
            {
                IProxy cachedProxy;
                if (item.TryGetTarget(out cachedProxy) && ReferenceEquals(cachedProxy.Target, target))
                {
                    //found cached proxy
                    proxy = cachedProxy;
                    return true;
                }
            }

            proxy = null;
            return false;
        }
    }
}
