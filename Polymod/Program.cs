using Polymod.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    class Program
    {
        static void Main(string[] args)
        {
            var proxyBuilder = new ProxyBuilder();
            var proxy = proxyBuilder.Build(new Foo());


            var modelBuilder = new FluentModelBuilder<Foo>();

            modelBuilder.AddNotificationAspect()
                .AddChange(f => f.Name, f => f.LastName)
                .AddChange(f => f.Name, f => f.Id);



            
        }
    }

    public class Foo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string LastName { get; set; }
    }

}
