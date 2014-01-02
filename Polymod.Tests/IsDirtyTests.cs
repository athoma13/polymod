using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq.Expressions;
using Polymod.Fluent;

namespace Polymod.Tests
{
    [TestClass]
    public class IsDirtyTests
    {
        [TestMethod]
        public void ShouldSetIsDirty()
        {
            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new IsDirtyAspectBuilder());

            var foo = new Foo() { A = "original", B = 10 };
            var proxy = pb.Build(foo);

            Assert.IsFalse(proxy.GetAspect<IsDirtyAspect>().IsDirty);
            Helper.SetProperty(proxy, "A", "modified");
            Assert.IsTrue(proxy.GetAspect<IsDirtyAspect>().IsDirty);

            Helper.SetProperty(proxy, "A", "original");
            Assert.IsFalse(proxy.GetAspect<IsDirtyAspect>().IsDirty);

            Helper.SetProperty(proxy, "B", 15);
            Assert.IsTrue(proxy.GetAspect<IsDirtyAspect>().IsDirty);

        }
    }
}
