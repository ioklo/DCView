using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Threading.Tasks;

using DCView.Util;

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


        public Task Login(string id, string passwd)
        {
            return Task.Factory.StartNew(() =>
            {
                DCViewWebClient client = new DCViewWebClient();

                client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                client.Headers["Referer"] = "http://m.dcinside.com/login.php?r_url=%2F";

                string data = string.Format(
                    "user_id={0}&user_pw={1}&r_url=%2F",
                    HttpUtility.UrlEncode(id),
                    HttpUtility.UrlEncode(passwd));
                
                Task<string> task = client.UploadStringAsyncTask(new Uri("http://dcid.dcinside.com/join/mobile_login_ok.php", UriKind.Absolute), "POST", data);
                task.Wait();

                var cookies = DCViewWebClient.CookieContainer.GetCookies(new Uri("http://gall.dcinside.com"));

                // 로긴 성공
                if (cookies["dc_m_login"] != null)
                {
                    LoggedIn = true;
                }
                else
                {
                    LoggedIn = false;
                }
                
            });
        }
    }
}
