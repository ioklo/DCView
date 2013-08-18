using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView.Board
{
    public interface ICredential
    {
        string StatusText { get; }
        event EventHandler OnStatusChanged;
    }
}
