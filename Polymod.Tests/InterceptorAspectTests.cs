using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polymod.Aspects;
using Polymod.Fluent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Tests
{
    [TestClass]
    public class InterceptorAspectTests
    {
        public class Root
        {
            public Foo Foo1 { get; set; }
            public Foo[] FooArray { get; set; }
            public IEnumerable<Foo> FooGenericEnumerable { get; set; }
            public IEnumerable FooEnumerable { get; set; }
            public List<Foo> FooList { get; set; }
            public int[] IntArray { get; set; }
            public List<int> IntList { get; set; }
            public List<string> StringList { get; set; }
        }

        public class Foo
        {
            public Foo FooProperty { get; set; }
            public string A { get; set; }
            public int B { get; set; }
        }

        [TestMethod]
        public void ShouldInterceptList()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { FooList = new List<Foo>() { new Foo() { A = "123" } } };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.FooList);
            Assert.AreEqual(1, collection.Count);
            Assert.AreSame(root.FooList[0], collection.First().Target);
        }

        [TestMethod]
        public void ShouldInterceptArray()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { FooArray = new Foo[] { new Foo() { A = "123" } } };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.FooArray);
            Assert.AreEqual(1, collection.Count);
            Assert.AreSame(root.FooArray[0], collection.First().Target);
        }

        [TestMethod]
        public void ShouldSetArrayElement()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { FooArray = new Foo[] { new Foo() { A = "123" } } };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.FooArray);
            var newElement = new Foo() { A = "Hello"};
            collection[0] = pb.Build(newElement);

            Assert.AreSame(root.FooArray[0], newElement);
        }

        [TestMethod]
        public void ShouldInterceptNonProxyableArray()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { IntArray = new[] { 1, 2, 3 } };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.IntArray);
            Assert.AreEqual(3, collection.Count);
            Assert.AreEqual(root.IntArray[0], collection.First().Target);
        }

        [TestMethod]
        public void ShouldSetNonProxyableArrayElement()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { IntArray = new [] { 1,2,3} };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.IntArray);
            var newElement = 5;
            collection[0] = pb.Build(newElement);

            Assert.AreEqual(root.IntArray[0], newElement);
        }


        [TestMethod]
        public void ShouldInterceptGenericEnumerable()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());

            var root = new Root() { FooGenericEnumerable = new Foo[] { new Foo() { A = "123" } }.AsEnumerable() };
            var rootProxy = pb.Build(root);

            var aspect = rootProxy.GetPropertyAspect();
            var collection = aspect.GetCollection(f => f.FooGenericEnumerable);
            Assert.AreEqual(1, collection.Count);
            Assert.AreSame(root.FooGenericEnumerable.First(), collection.First().Target);
        }




        //TODO: Finish all ShouldIntercept on all kinds of Collections (includint IEnumerables - that are not arrays or collections) (Test non proxy candidates, and Collections that contain both candidates and non-proxy candidates)
        //TODO: Assert I can access items in the collection, add new items, remove existing ones etc...
        //TODO: Need to have a 'Proxies' hash set that's connected to the root of the built graph. This will be used to prevent circular building, and also return existing proxies that have already been generated. E.g. For a Person p that has a Manager where the Person self manages, the same proxy instance should be returned for p and p's manager. Prevents circular dependencies.


    }

}
