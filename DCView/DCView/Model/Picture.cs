using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView
{
    public class Picture
    {
        Uri uri;

        public Uri Uri{ get { return uri; } }

        public Picture(Uri uri, string referer)
        {
            this.uri = uri;
            Referer = referer;
        }

        public string Referer { get; set; }
    }
}
