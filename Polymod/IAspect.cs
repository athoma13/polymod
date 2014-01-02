using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    //public interface IAspectInstantiator
    //{
    //    IAspect Create();
    //}

    //public class AspectInstantiator : IAspectInstantiator
    //{
    //    Func<IAspect> _func;

    //    public AspectInstantiator(Func<IAspect> func)
    //    {
    //        if (func == null) throw new ArgumentNullException("func");
    //        _func = func;
    //    }

    //    IAspect IAspectInstantiator.Create()
    //    {
    //        return _func();
    //    }

    //    public static IAspectInstantiator Create<TAspect>() where TAspect : IAspect, new()
    //    {
    //        return new AspectInstantiator(() => new TAspect());
    //    }

    //    public static IAspectInstantiator Create<TAspect>(Func<TAspect> creator) where TAspect : IAspect
    //    {
    //        return new AspectInstantiator(() => creator());
    //    }

    //}

    public interface IAspectBuilder
    {
        void Build(TypeBuilder typeBuilder, IDictionary<string, object> proxyState);
    }

    public interface IFluentAspectBuilder<TFluentNode>
    {
        IAspectBuilder Builder { get; }
        TFluentNode CreateFluentNode(ProxyBuilder proxyBuilder);
    }

    public interface IAspect
    {
        void Bind(IProxy proxy);
    }

    internal class ProxyTypeCachedEntry
    {
        public Type Type {get;set;}
        public IDictionary<string, object> ProxyState {get; set;}
    }


    

}
