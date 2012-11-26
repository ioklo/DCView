using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView
{
    public class Picture
    {
        string uri;

        public string Uri{ get { return uri; } }

        public Picture(string uri, string referer)
        {
            this.uri = uri;
            Referer = referer;
        }

        public string Referer { get; set; }
    }
}
