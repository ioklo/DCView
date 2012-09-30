using System;
using System.Net;
using System.Windows;
using System.Security;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Specialized;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MyApps.Common
{
    // 쿠키를 지원하는 WebClient
    public class WebClientEx : WebClient
    {
        static private CookieContainer cookieContainer = new CookieContainer();
        static public CookieContainer CookieContainer { get { return cookieContainer; } }

        [SecuritySafeCritical]
        public WebClientEx()
        {
            this.Encoding = System.Text.Encoding.UTF8;
        }

        public Task<string> DownloadStringAsyncTask(Uri uri, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<string>();            

            var registration = ct.Register(() =>
                {
                    this.CancelAsync();
                });

            this.DownloadStringCompleted += (o, e) =>
            {
                // 취소되었거나
                if (e.Cancelled)
                {
                    tcs.SetCanceled();
                    return;
                }

                // 에러가 났거나
                if (e.Error != null)
                {
                    tcs.SetException(e.Error);
                    return;
                }
                        
                tcs.SetResult(e.Result);
                registration.Dispose();
            };

            this.DownloadStringAsync(uri);

            return tcs.Task;
        }

        public Task<string> UploadStringAsyncTask(Uri uri, string method, string data)
        {
            var tcs = new TaskCompletionSource<string>();

            this.UploadStringCompleted += (o, e) =>
            {
                // 취소되었거나
                if (e.Cancelled)
                {
                    tcs.SetCanceled();
                    return;
                }

                // 에러가 났거나
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                    return;
                }

                tcs.SetResult(e.Result);
            };

            this.UploadStringAsync(uri, method, data);

            return tcs.Task;
        }

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
