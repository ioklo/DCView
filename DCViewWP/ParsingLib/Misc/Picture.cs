using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView
{
    public class Picture
    {
        public string Uri { get; private set; }
        public string BrowserUri { get; private set; }
        public string Referer { get; private set; }

        public Picture(string uri, string referer)
        {
            Uri = uri;
            BrowserUri = uri;
            Referer = referer;
        }

        public Picture(string uri, string browserUri,string referer)
        {
            Uri = uri;
            BrowserUri = browserUri;
            Referer = referer;
        }

    }
}
