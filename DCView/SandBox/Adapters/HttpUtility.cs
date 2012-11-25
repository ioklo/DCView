using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    class HttpUtility  
    {
        static public string UrlEncode(string str)
        {
            return System.Web.HttpUtility.UrlEncode(str);
        }

        static public string UrlDecode(string str)
        {
            return System.Web.HttpUtility.UrlDecode(str);
        }

        static public string HtmlDecode(string str)
        {
            return System.Web.HttpUtility.HtmlDecode(str);
        }
    }
}
