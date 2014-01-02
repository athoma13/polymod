using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Polymod.Fluent;

namespace Polymod.Tests
{
    public class Foo
    {
        public string A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }


    [TestClass]
    public class BasicGetterSetterTests
    {
        [TestMethod]
        public void ShouldGet()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            var foo = new Foo() { A = "original", B=10 };
            var proxy = pb.Build(foo);

            Assert.AreEqual(foo.A, Helper.GetProperty(proxy, "A"));
            Assert.AreEqual(foo.B, Helper.GetProperty(proxy, "B"));
        }

        [TestMethod]
        public void ShouldSet()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            var foo = new Foo() { A = "original", B = 10 };
            var proxy = pb.Build(foo);

            Helper.SetProperty(proxy, "A", "modified");
            Helper.SetProperty(proxy, "B", 15);

            Assert.AreEqual("modified", foo.A);
            Assert.AreEqual(15, foo.B);
        }


    }


}
