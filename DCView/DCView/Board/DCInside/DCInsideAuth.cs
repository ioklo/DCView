using System;
using System.Net;
using System.Windows;
using System.Threading.Tasks;

using DCView.Util;
using DCView.Lib;
using System.Threading;

namespace DCView
{
    public class DCInsideAuth
    {
        private bool loggedIn = false;

        public bool LoggedIn
        {
            get
            {
                lock(this)
                {
                    return loggedIn;
                }
            }
       
            set
            {
                lock (this)
                {
                    loggedIn = value;
                }
            }
        }


        public bool Login(string id, string passwd, CancellationToken ct)
        {
            DCViewWebClient client = new DCViewWebClient();

            client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            client.Headers["Referer"] = "http://m.dcinside.com/login.php?r_url=%2F";

            string data = string.Format(
                "user_id={0}&user_pw={1}&r_url=%2F",
                HttpUtility.UrlEncode(id),
                HttpUtility.UrlEncode(passwd));

            try
            {
                string result = client.UploadStringAsyncTask(new Uri("http://dcid.dcinside.com/join/mobile_login_ok.php", UriKind.Absolute), "POST", data, ct).GetResult();
                var cookies = DCViewWebClient.CookieContainer.GetCookies(new Uri("http://gall.dcinside.com"));

                if (result.IndexOf("parent.location.href='http://m.dcinside.com/'") == -1)
                    LoggedIn = false;
                else LoggedIn = true;

                return LoggedIn;
            }
            catch
            {
                LoggedIn = false;
                return false;                
            }
        }
    }
}
