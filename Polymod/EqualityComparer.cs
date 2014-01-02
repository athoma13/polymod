using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Polymod
{
    public class EqualityComparer : IEqualityComparer
    {
        private static readonly EqualityComparer _default = new EqualityComparer();
        public static EqualityComparer Default { get { return _default; } }

        public bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, null) && ReferenceEquals(y, null)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            throw new NotSupportedException();
        }
    }
}
