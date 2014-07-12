using System;
using System.Net;
using System.Windows;

namespace DCView.Board
{
    public class DCInsideComment : IComment
    {
        public int Level { get; set; }
        public string Name {
            get
            {
                if (MemberStatus == MemberStatus.Anonymous) return string.Format("{0} ({1})", name, IP);
                return name;
            }
            set
            {
                name = value;
            }
        }
        public MemberStatus MemberStatus { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public string IP { get; set; }

        private string name;
    }
}
