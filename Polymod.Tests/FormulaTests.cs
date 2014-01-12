using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polymod;
using System.Collections.Generic;
using Moq;
using System.Linq.Expressions;

namespace Polymod.Tests
{
    [TestClass]
    public class FormulaTests
    {
        public class Foo
        {
            public int A { get; set; }
            public int B { get; set; }
        }

        [TestMethod]
        public void ShouldAddNewProperty()
        {
            var proxy = GetFooProxyWithCProperty(new Foo(), false);
            Helper.SetProperty(proxy, "C", "Hello");
            Assert.AreEqual("Hello", Helper.GetProperty(proxy, "C"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Expected exception because should not be able to set a readonly property")]
        public void ShouldAddNewReadonlyProperty()
        {
            var proxy = GetFooProxyWithCProperty(new Foo(), true);
            Helper.SetProperty(proxy, "C", "Hello");
        }

        [TestMethod]
        public void ShouldAccessNewPropertyWithInterceptorAspect()
        {
            var proxyBuilder = new ProxyBuilder();
            var formulaAspectBuilder = new FormulaAspectBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());
            proxyBuilder.AddBuilder(formulaAspectBuilder);

            var node = formulaAspectBuilder.For<Foo>();
            var C = node.AddProperty<string>("C");
            var proxy = proxyBuilder.Build(new Foo());
            
            Helper.SetProperty(proxy, "C", "Hello");
            Assert.AreEqual("Hello", Helper.GetProperty(proxy, "C"));
            Assert.AreEqual("Hello", proxy.Get(C));

            proxy.Set(C, "Hello World!");
            Assert.AreEqual("Hello World!", proxy.Get(C));
        }

        [TestMethod]
        public void ShouldAddCalculatedFormula()
        {
            var proxyBuilder = new ProxyBuilder();
            var formulaAspectBuilder = new FormulaAspectBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());
            proxyBuilder.AddBuilder(formulaAspectBuilder);

            var node = formulaAspectBuilder.For<Foo>();
            var C = node.AddFormula("C", m => m.A * m.B);
            var proxy = proxyBuilder.Build(new Foo() { A = 3, B = 2 });

            Assert.AreEqual(6, Helper.GetProperty(proxy, "C"));
        }

        [TestMethod]
        public void FormulaVisitorShouldReplaceParametersWithProxyCalls()
        {
            var visitor = new FormulaVisitor();
            var foo = new Foo() { A = 8, B = 2 };
            Expression<Func<Foo, int>> fooExpression = m => m.A * m.B;
            var mock = new Mock<IProxy<Foo>>();
            mock.Setup(m => m.Target).Returns(foo);
            var visitedExpression = (Expression<Func<IProxy<Foo>, int>>)visitor.Visit(fooExpression);
            var compiled = visitedExpression.Compile();
            Assert.AreEqual(16, compiled(mock.Object));
        }



        private IProxy<Foo> GetFooProxyWithCProperty(Foo foo, bool isCReadonly)
        {
            var proxyBuilder = new ProxyBuilder();
            var formulaAspectBuilder = new FormulaAspectBuilder();
            proxyBuilder.AddBuilder(formulaAspectBuilder);

            var node = formulaAspectBuilder.For<Foo>();
            var C = node.AddProperty<string>("C", isCReadonly);
            var result = proxyBuilder.Build(foo);
            return result;
        }

        private IProxy<Foo> GetFooProxyWithCPropertyAndInterceptorAspect(Foo foo, bool isCReadonly)
        {
            var proxyBuilder = new ProxyBuilder();
            var formulaAspectBuilder = new FormulaAspectBuilder();
            proxyBuilder.AddBuilder(new InterceptorAspectBuilder());
            proxyBuilder.AddBuilder(formulaAspectBuilder);

            var node = formulaAspectBuilder.For<Foo>();
            var C = node.AddProperty<string>("C", isCReadonly);
            var result = proxyBuilder.Build(foo);
            return result;
        }

    }
}
