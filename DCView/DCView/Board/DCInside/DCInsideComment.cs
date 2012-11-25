using System;
using System.Net;
using System.Windows;

namespace DCView
{
    public class DCInsideComment : IComment
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
    }
}
