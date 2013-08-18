using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace DCView.Adapter
{
    public abstract class AdapterFactory
    {
        public abstract IWebClient CreateWebClient();
        public abstract ISettings Settings { get; }

        public abstract string UrlEncode(string str);
        public abstract string UrlDecode(string str);
        public abstract string HtmlDecode(string str);

        public abstract CookieContainer CookieContainer { get; }

        public static AdapterFactory Instance { get; set; }

        public abstract void ResetCookie();

        public abstract Stream OpenReadStorageFile(string fileName);
        public abstract Stream OpenWriteStorageFile(string fileName);
        public abstract Stream OpenReadResourceFile(string fileName);

        public abstract bool CopyResourceToStorage(string appPath, string storagePath, bool bForce = false);
    }
}
