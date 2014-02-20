using System.Collections.Generic;
using System.Text;

namespace VARSpike
{
    public class Vector<T> : List<T>
    {
        public Vector()
        {
        }

        public Vector(int capacity) : base(capacity)
        {
        }

        public Vector(IEnumerable<T> collection) : base(collection)
        {
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("(");
            var first = true;
            foreach (var item in this)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(item);
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}