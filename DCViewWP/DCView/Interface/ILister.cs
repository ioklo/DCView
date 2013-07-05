using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DCView
{
    public interface ILister<T>
    {
        bool Next(CancellationToken ct, out IEnumerable<T> elems);
    }
}
