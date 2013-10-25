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

        public Picture(string uri)
        {
            Uri = uri;
            BrowserUri = uri;
        }

        public Picture(string uri, string browserUri)
        {
            Uri = uri;
            BrowserUri = browserUri;
        }

    }
}
