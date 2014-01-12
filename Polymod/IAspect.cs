using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public interface IAspectBuilder
    {
        void Build(TypeBuilder typeBuilder, StateBag proxyState);
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
        public StateBag ProxyState {get; set;}
    }


    

}
