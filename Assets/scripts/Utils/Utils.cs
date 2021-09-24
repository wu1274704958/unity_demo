using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Utils
{
    public class Wrap<T>
        where T : struct
    {
        public T val;

        public Wrap(T val)
        {
            this.val = val;
        }
    }
    public class Pair<T, T2>
    {
        public T first;
        public T2 second;

        public Pair(T first, T2 second)
        {
            this.first = first;
            this.second = second;
        }
    }

    public class VarCache<T> where T : class
    {
        private T t = null;
        private Func<T> construct;

        public VarCache(Func<T> construct)
        {
            this.construct = construct;
        }

        public T val
        {
            get
            {
                if (t == null && construct != null)
                    t = construct.Invoke();
                return t;
            }
        }

        public void clear()
        {
            t = null;
        }
    }
}
