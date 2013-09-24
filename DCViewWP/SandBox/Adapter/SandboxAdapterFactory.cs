using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DCView.Adapter;

namespace SandBox
{
    class SandboxAdapterFactory : AdapterFactory
    {
        static public void Init()
        {
            Instance = new SandboxAdapterFactory();
        }

        public override IWebClient CreateWebClient(bool bMobile)
        {
            return new SandboxWebClient(bMobile);
        }

        public override ISettings Settings
        {
            get { return SandboxSettings.Instance; }
        }

        public override string UrlEncode(string str)
        {
            return System.Web.HttpUtility.UrlEncode(str);
        }

        public override string UrlDecode(string str)
        {
            return System.Web.HttpUtility.UrlDecode(str);
        }

        public override string HtmlDecode(string str)
        {
            return System.Web.HttpUtility.HtmlDecode(str);
        }

        public override CookieContainer CookieContainer
        {
            get { return SandboxWebClient.CookieContainer; }
        }

        public override void ResetCookie()
        {
            SandboxWebClient.ResetCookie();
        }

        public override Stream OpenReadStorageFile(string fileName)
        {
            string path = "storage" + fileName;
            return File.OpenRead(path);
        }

        public override Stream OpenWriteStorageFile(string fileName)
        {
            string path = "storage" + fileName;
            return File.OpenWrite(path);            
        }

        public override Stream OpenReadResourceFile(string fileName)
        {
            return File.OpenRead(fileName);            
        }

        public override bool CopyResourceToStorage(string appPath, string storagePath, bool bForce = false)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName("storage" + storagePath));
                File.Copy(appPath, "storage" + storagePath, bForce);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
