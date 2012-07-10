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
using System.Collections.ObjectModel;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Specialized;
using MyApps.Common;

namespace MyApps.DCView
{
    public class ArticleManager : INotifyPropertyChanged
    {
        ObservableCollection<Article> articles;
        int curPage;
        bool isLoadingArticleList;
        bool isLoadingArticleText;
        bool isReplying;

        public delegate void ArticleAddedDelegate(Article article);

        public event PropertyChangedEventHandler PropertyChanged;
        public event ArticleAddedDelegate ArticleAdded;
        public event Action ArticleCleared;

        public string ID {get; private set;}
        public string Site { get; private set; }
        public string PCSite { get; private set; }

        public bool IsLoadingArticleList 
        {
            get { return isLoadingArticleList; }
            private set
            {
                isLoadingArticleList = value;
                Notify("IsLoadingArticleList");
            }
        }

        public bool IsLoadingArticleText
        {
            get { return isLoadingArticleText; }
            private set
            {
                isLoadingArticleText = value;
                Notify("IsLoadingArticleText");
            }
        }

        public bool IsReplying
        {
            get { return isReplying; }
            private set
            {
                isReplying = value;
                Notify("IsReplying");
            }
        }

        public ArticleManager(string id, string site, string pcsite)
        {
            ID = id;
            Site = site;
            PCSite = pcsite;
            isLoadingArticleList = false;
            isLoadingArticleText = false;
            isReplying = false;
            articles = new ObservableCollection<Article>();
            curPage = 0;
        }

        private void Notify(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Refresh()
        {
            articles.Clear();
            ArticleCleared();

            curPage = 0;
            GetNextArticles();
        }
        
        
        
        public void Reply(Article article, string text, Action ReplyCompleted, Action ReplyCancelled)
        {
            WebClientEx webClient = new WebClientEx();
            webClient.UploadStringCompleted += (s, e) =>
            {
                if (e.Cancelled || e.Error != null)
                    ReplyCancelled();
                else
                    ReplyCompleted();
            };

            webClient.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            webClient.Headers["Referer"] = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}", ID, article.ID);
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
            string data = string.Format(
                "id={0}&no={1}&comment_memo={2}&mode=comment&user_no={3}",
                HttpUtility.UrlEncode(ID),
                HttpUtility.UrlEncode(article.ID),
                HttpUtility.UrlEncode(text),
                article.CommentUserID
                );

            webClient.UploadStringAsync(new Uri(string.Format("http://{0}/_option_write.php", Site), UriKind.Absolute), "POST", data);
        }
        

        

        
    }

}
