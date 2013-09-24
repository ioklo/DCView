using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using DCView.Lib;
using DCView.Util;

namespace DCView.Adapter
{
    class WPAdapterFactory : AdapterFactory
    {
        public override IWebClient CreateWebClient(bool bMobile)
        {
            return new DCViewWebClient(bMobile);
        }
        
        public override ISettings Settings
        {
            get { return WPSetting.Instance; }
        }

        public override CookieContainer CookieContainer
        {
            get { return WebClientEx.CookieContainer; }
        }

        public override void ResetCookie()
        {
            DCViewWebClient.ResetCookie();
        }

        public override Stream OpenReadStorageFile(string fileName)
        {
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            return storage.OpenFile(fileName, FileMode.Open);
        }

        public override Stream OpenWriteStorageFile(string fileName)
        {
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            return storage.OpenFile(fileName, FileMode.Create);
        }

        public override Stream OpenReadResourceFile(string fileName)
        {
            var sri = Application.GetResourceStream(new Uri(fileName, UriKind.Relative));
            Debug.Assert(sri != null);
            return sri.Stream;
        }

        public static void Init()
        {
            AdapterFactory.Instance = new WPAdapterFactory();
        }

        public override string UrlEncode(string str)
        {
            return HttpUtility.UrlEncode(str);
        }

        public override string UrlDecode(string str)
        {
            return HttpUtility.UrlDecode(str);
        }

        public override string HtmlDecode(string str)
        {
            return HttpUtility.HtmlDecode(str);
        }

        public override bool CopyResourceToStorage(string appPath, string storagePath, bool bForce = false)
        {
            return StorageUtil.CopyResourceToStorage(appPath, storagePath, bForce);
        }
    }
}
