using System;
using System.Net;
using System.Windows;
using DCView.Lib;

namespace DCView.Util
{
    public class DCViewWebClient : WebClientEx
    {
        public DCViewWebClient()
        {
            this.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
        }
    }
}
