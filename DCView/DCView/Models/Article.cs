using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace MyApps.DCView
{
    public class Article
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public bool HasImage { get; set; }
        public string Text { get; set; }
        public string CommentUserID { get; set; } // 댓글 요청 보낼때 쓸 user_no
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public List<Comment> Comments { get; private set; }
        public List<Uri> Images { get; private set; }

        public Article()
        {
            Comments = new List<Comment>();
            Images = new List<Uri>();
        }

        public string Status
        {
            get
            {
                string dateString = string.Empty;
                TimeSpan elapsed = DateTime.Now.Subtract(Date);

                if (elapsed < new TimeSpan(1, 0, 0))
                    dateString = string.Format("{0}분 전", elapsed.Minutes);
                else if (elapsed < new TimeSpan(1, 0, 0, 0))
                    dateString = string.Format("{0}시간 전", elapsed.Hours);
                else
                    dateString = Date.ToString("MM-dd");

                return string.Format("{0} | {1} | 댓글 {2}", Name, dateString, CommentCount);

            }
        }


    }
}
