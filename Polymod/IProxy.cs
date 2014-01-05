using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Polymod
{
    public interface IProxy
    {
        object Target { get; }
        StateBag State { get; }
    }

    public interface IProxy<out T> : IProxy
    {
        new T Target { get; }
    }
}
