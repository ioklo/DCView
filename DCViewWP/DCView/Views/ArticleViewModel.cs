using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Media;
using DCView.Board;

namespace DCView
{
    public class ArticleViewModel
    {
        public IArticle Article { get; private set; }

        public ArticleViewModel(IArticle article)
        {
            Article = article;
        }

        public Brush HasImageBrush
        {
            get
            {
                if (Article.HasImage)
                    return (Brush)Application.Current.Resources["DCViewAccentBrush"];
                else
                    return (Brush)Application.Current.Resources["PhoneSubtleBrush"];
            }
        }

        public string Title { get { return HttpUtility.HtmlDecode(Article.Title); } }

        public string Name { get { return HttpUtility.HtmlDecode(Article.Name); } }
        public DateTime Date { get { return Article.Date; } }

        public Visibility HasImageVisibility { get { return Article.HasImage ? Visibility.Visible : Visibility.Collapsed; } }

        public Brush MemberStatusBrush
        {
            get
            {
                switch (Article.MemberStatus)
                {
                    case MemberStatus.Anonymous:
                        return new SolidColorBrush(Colors.Transparent);

                    case MemberStatus.Default:
                        return new SolidColorBrush(Colors.LightGray);

                    case MemberStatus.Fix:
                        return new SolidColorBrush(Colors.Yellow);
                }

                return null;
            }
        }
        
        public Visibility MemberStatusVisibility 
        {
            get
            {
                if (Article.MemberStatus == MemberStatus.Anonymous)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public string DateString
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

                return dateString;
            }
        }

        public int CommentCount
        {
            get { return Article.CommentCount; }
        }
    }
}
