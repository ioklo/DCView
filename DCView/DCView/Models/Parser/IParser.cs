using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyApps.Models
{
    interface IParser
    {
        // 파서는 하나만, 혹은 여러개의 데이터를 되돌려 줄 수 있다
        IEnumerable<IDictionary<string, string>> Parse(string input); 
    }
}
