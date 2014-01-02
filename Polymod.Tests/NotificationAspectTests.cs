using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polymod.Aspects;
using Polymod.Fluent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod.Tests
{
    [TestClass]
    public class NotificationAspectTests
    {
        [TestMethod]
        public void ShouldImplementINotifyPropertyChanged()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());
            bool flag = false;

            var proxy = pb.Build(foo);
            var myProperyName = "random";
            ((INotifyPropertyChanged)proxy).PropertyChanged +=
                (sender, e) =>
                {
                    Assert.AreSame(proxy, sender);
                    Assert.AreEqual(myProperyName, e.PropertyName);
                    flag = true;
                };

            var raise = (IRaisePropertyChanged)proxy;
            raise.RaisePropertyChanged(myProperyName);
            Assert.IsTrue(flag, "PropertyChanged wasn't raised.");
        }

        [TestMethod]
        public void ShouldRaisePropertyChangedForChangesOnProxy()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());
            

            var proxy = pb.Build(foo);
            var receivedEvents = new List<string>();
            ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, e) => receivedEvents.Add(e.PropertyName);

            Helper.SetProperty(proxy, "A", "modified");
            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual("A", receivedEvents[0]);
        }



        [TestMethod]
        public void RaisingWhenNoOneIsListeningShouldNotFail()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());

            var proxy = pb.Build(foo);
            var myProperyName = "random";
            var raise = (IRaisePropertyChanged)proxy;

            //These call should not fail (even though there is no registered Event Listeners)
            raise.RaisePropertyChanged(myProperyName);
            raise.RaisePropertyChanged(myProperyName);
        }

        [TestMethod]
        public void ShouldBeAbleToHaveMultipleEventListeners()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());
            

            var proxy = pb.Build(foo);
            var receivedEvents = new List<string>();

            ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, e) => receivedEvents.Add(e.PropertyName);
            ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, e) => receivedEvents.Add(e.PropertyName);
            ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, e) => receivedEvents.Add(e.PropertyName);


            var raise = (IRaisePropertyChanged)proxy;
            raise.RaisePropertyChanged("Hello");
            raise.RaisePropertyChanged("World");

            Assert.AreEqual(6, receivedEvents.Count);
            Assert.AreEqual(3, receivedEvents.Count(e => e == "Hello"));
            Assert.AreEqual(3, receivedEvents.Count(e => e == "World"));
        }

        [TestMethod]
        public void ShouldBeAbleToRemoveEventListeners()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());

            var proxy = pb.Build(foo);
            var receivedEvents = new List<string>();

            var listener1 = new PropertyChangedEventHandler((sender, e) => receivedEvents.Add(e.PropertyName + "1"));
            var listener2 = new PropertyChangedEventHandler((sender, e) => receivedEvents.Add(e.PropertyName + "2"));
            var listener3 = new PropertyChangedEventHandler((sender, e) => receivedEvents.Add(e.PropertyName + "3"));
            ((INotifyPropertyChanged)proxy).PropertyChanged += listener1;
            ((INotifyPropertyChanged)proxy).PropertyChanged += listener2;
            ((INotifyPropertyChanged)proxy).PropertyChanged += listener3;

            var raise = (IRaisePropertyChanged)proxy;
            raise.RaisePropertyChanged("Hello");
            Assert.AreEqual(3, receivedEvents.Count);
            Assert.IsTrue(receivedEvents[0] == "Hello1");
            Assert.IsTrue(receivedEvents[1] == "Hello2");
            Assert.IsTrue(receivedEvents[2] == "Hello3");

            //Clear and remove handlers
            receivedEvents.Clear();
            ((INotifyPropertyChanged)proxy).PropertyChanged -= listener3;
            ((INotifyPropertyChanged)proxy).PropertyChanged -= listener1;
            raise.RaisePropertyChanged("World");
            Assert.AreEqual(1, receivedEvents.Count);
            Assert.IsTrue(receivedEvents[0] == "World2");

            //Remove last handler
            receivedEvents.Clear();
            ((INotifyPropertyChanged)proxy).PropertyChanged -= listener2;
            raise.RaisePropertyChanged("Should Not See This");
            Assert.AreEqual(0, receivedEvents.Count);
        }

        [TestMethod]
        public void ShouldRaiseEventsForAllAffectedProperties()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            var notificationAspectBuilder = new NotificationAspectBuilder();
            notificationAspectBuilder.AddNotification(typeof(Foo), "A", "B");
            notificationAspectBuilder.AddNotification(typeof(Foo), "B", "C");
            pb.AddBuilder(notificationAspectBuilder);

            var proxy = pb.Build(foo);
            var receivedEvents = ListenToEvents(proxy);
            proxy.GetPropertyAspect().Set(m => m.A, "new");
            
            //A has changed...
            Assert.IsTrue(receivedEvents.Contains("A"));
            //Since A affects B...
            Assert.IsTrue(receivedEvents.Contains("B"));
            //Since B affects C...
            Assert.IsTrue(receivedEvents.Contains("C"));
        }

        [TestMethod]
        public void ShouldRaiseEventsInBatches()
        {
            var foo = new Foo() { A = "original", B = 10 };

            var pb = new ProxyBuilder();
            pb.AddBuilder(new InterceptorAspectBuilder());
            pb.AddBuilder(new NotificationAspectBuilder());

            var proxy = pb.Build(foo);
            var receivedEvents = ListenToEvents(proxy);

            using (var scope = NotifyScope.Create())
            {
                proxy.GetPropertyAspect().Set(m => m.A, "new");
                proxy.GetPropertyAspect().Set(m => m.B, 11);
                Assert.AreEqual(0, receivedEvents.Count);
            }
            
            //Events will only be published when the topmost scope is released.
            Assert.AreEqual(2, receivedEvents.Count);
            Assert.IsTrue(receivedEvents.Contains("A"));
            Assert.IsTrue(receivedEvents.Contains("B"));
        }


        private List<string> ListenToEvents(IProxy proxy)
        {
            var list = new List<string>();
            ((INotifyPropertyChanged)proxy).PropertyChanged += (sender, e) => list.Add(e.PropertyName);
            return list;
        }
    }
}
