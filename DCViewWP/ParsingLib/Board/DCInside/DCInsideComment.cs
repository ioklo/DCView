using System;
using System.Net;
using System.Windows;

namespace DCView.Board
{
    public class DCInsideComment : IComment
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public MemberStatus MemberStatus { get; set; }
        public string Text { get; set; }
    }
}
