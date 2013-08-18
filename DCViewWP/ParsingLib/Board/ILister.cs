using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DCView.Board
{
    public interface ILister<T>
    {
        bool Next(out IEnumerable<T> elems);
    }
}
