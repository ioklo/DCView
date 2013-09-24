using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using DCView.Adapter;

namespace SandBox
{
    class SandboxWebClient : WebClient, IWebClient
    {
        public SandboxWebClient(bool bMobile)
        {
            if (bMobile)
                this.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";

            this.Encoding = System.Text.Encoding.UTF8;            
        }

        Dictionary<Action<object, long, long, int>, DownloadProgressChangedEventHandler> eventHandlers = new Dictionary<Action<object, long, long, int>, DownloadProgressChangedEventHandler>();
        event Action<object, long, long, int> IWebClient.DownloadProgressChanged
        {
            add
            {
                DownloadProgressChangedEventHandler handler = (obj, args) => { value(obj, args.BytesReceived, args.TotalBytesToReceive, args.ProgressPercentage); };
                eventHandlers.Add(value, handler);
                DownloadProgressChanged += handler;
            }

            remove
            {
                DownloadProgressChangedEventHandler handler;
                if (eventHandlers.TryGetValue(value, out handler))
                {
                    DownloadProgressChanged -= handler;
                    eventHandlers.Remove(value);
                }
            }
        }

        public void SetHeader(string name, string data)
        {
            Headers[name] = data;
        }

        public System.Threading.Tasks.Task<string> DownloadStringAsyncTask(Uri uri)
        {
            return this.DownloadStringTaskAsync(uri);
        }

        public System.Threading.Tasks.Task<string> UploadStringAsyncTask(Uri uri, string method, string data)
        {
            return this.UploadStringTaskAsync(uri, method, data);
        }

        static private CookieContainer cookieContainer = new CookieContainer();
        static public CookieContainer CookieContainer { get { return cookieContainer; } }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            HttpWebResponse httpResponse = response as HttpWebResponse;

            if (httpResponse != null)
            {
                cookieContainer.Add(response.ResponseUri, httpResponse.Cookies);
            }

            return response;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.CookieContainer = cookieContainer;
            }

            return request;
        }

        static public void ResetCookie()
        {
            cookieContainer = new CookieContainer();
        }
    }
}
