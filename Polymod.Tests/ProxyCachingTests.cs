using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polymod;
using System.Collections.Generic;

namespace Polymod.Tests
{
    [TestClass]
    public class ProxyCachingTests
    {
        public class MockProxy : IProxy
        {
            public object Target { get; private set; }
            public StateBag State { get { throw new NotImplementedException(); } }

            public MockProxy(object target)
            {
                Target = target;
            }
        }

        public class Foo
        {
            public Foo FooReference { get; set; }
            public Foo[] FooReferences { get; set; }
        }

        [TestMethod]
        public void ShouldReturnSameProxyReference()
        {
            var proxyBuilder = new ProxyBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());

            var foo = new Foo();
            var proxy1 = proxyBuilder.Build(foo);
            var proxy2 = proxyBuilder.Build(foo);

            Assert.AreSame(proxy1, proxy2);
        }

        [TestMethod]
        public void ShouldHandleCircularReference()
        {
            var proxyBuilder = new ProxyBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());

            var foo = new Foo();
            foo.FooReference = foo;

            var proxy1 = proxyBuilder.Build(foo);
            var proxy2 = Helper.GetProperty(proxy1, m => m.FooReference);

            Assert.AreSame(proxy1, proxy2);
        }

        [TestMethod]
        public void ShouldHandleCircularReferenceWithinSpiderCollections()
        {
            var proxyBuilder = new ProxyBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());

            var foo = new Foo();
            var foo1 = new Foo();
            var foo2 = new Foo();
            var foo3 = new Foo();

            foo.FooReferences = new[] { foo1, foo2, foo3, foo };

            var proxy1 = proxyBuilder.Build(foo);
            var proxy2 = ((IList<object>)Helper.GetProperty(proxy1, m => m.FooReferences))[3];

            Assert.AreSame(proxy1, proxy2);
        }

        [TestMethod]
        public void ShouldHandleIdenticalReferencesWitinCollections()
        {
            var proxyBuilder = new ProxyBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());

            var foo = new Foo();
            var foo1 = new Foo();

            foo.FooReferences = new[] { new Foo(), new Foo(), foo1, foo1 };

            var proxy1 = proxyBuilder.Build(foo);
            var proxy2 = ((IList<object>)Helper.GetProperty(proxy1, m => m.FooReferences))[2];
            var proxy3 = ((IList<object>)Helper.GetProperty(proxy1, m => m.FooReferences))[3];

            //FooReferences items 2 and 3 point to the same foo reference
            Assert.AreSame(proxy2, proxy3);
        }

        [TestMethod]
        public void CacheShouldNotHoldOnToGarbageCollectedInstances()
        {
            //This test prooves that when proxies go out of scope, the cache will not cause them to not be finalized and garbage collected.
            var cache = new ProxyCache();
            
            var foo = new Foo();
            var fooReference = new WeakReference<Foo>(foo);
            cache.Add(new MockProxy(foo));

            //Release foo and Garbage collect.
            foo = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            Assert.IsFalse(fooReference.TryGetTarget(out foo));
        }



    }
}
