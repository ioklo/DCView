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
        
        public void GetNextArticles()
        {
            WebClientEx webClient = new WebClientEx();

            webClient.Encoding = Encoding.UTF8;
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
            webClient.DownloadStringCompleted += GetNextArticlesCompleted;

            string url = string.Format("http://{0}/list.php?id={1}&page={2}&nocache={3}", Site, ID, curPage + 1, DateTime.Now.Ticks);
            webClient.DownloadStringAsync(new Uri(url, UriKind.Absolute));

            IsLoadingArticleList = true;
        }

        private void GetNextArticlesCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            IsLoadingArticleList = false;

            // 취소되거나 에러인 경우
            if (e.Cancelled || e.Error != null)
            {
                MessageBox.Show("다음 페이지를 읽어들이는데 실패했습니다. 다시 시도해보세요");                
                return;
            }           

            List<Article> newArticles = GetArticleListFromString(e.Result);
            curPage++;

            // 저번 아티클에서 가장 작았던 항목의 인덱스를 알아낸다.
            int lastItemIndex = int.MaxValue;
            if (articles.Count > 0)
            {
                Article article = articles[articles.Count - 1];

                int articleIndex;
                if (int.TryParse(article.ID, out articleIndex))
                    lastItemIndex = articleIndex;
            }

            // 항목을 추가한다. 저번보다 큰 번호는 추가하지 않는다
            foreach (var newArticle in newArticles)
            {
                int curItemIndex;
                if (!int.TryParse(newArticle.ID, out curItemIndex))
                    continue;

                if (lastItemIndex <= curItemIndex)
                    continue;

                articles.Add(newArticle);
                ArticleAdded(newArticle);
            }
        }
        
        public void GetArticleText(Article article, Action CompletedAction, Action CancelAction)
        {
            article.Text = string.Empty;
            article.Comments.Clear();

            WebClientEx webClient = new WebClientEx();
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
            webClient.Encoding = Encoding.UTF8;
            string url = string.Format("http://{0}/view.php?id={1}&no={2}&nocache={3}", Site, ID, article.ID, DateTime.Now.Ticks);

            // 프로그레스 바 작동            
            webClient.DownloadStringCompleted += (s, e) => 
            {
                IsLoadingArticleText = false;
                if (e.Cancelled || e.Error != null)
                {
                    CancelAction();
                    return;
                }

                if (!UpdateArticleTextAndComments(article, e.Result))
                {
                    CancelAction();
                    return;
                }

                CompletedAction();
            };
            webClient.DownloadStringAsync(new Uri(url, UriKind.Absolute));

            IsLoadingArticleText = true;
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
        

        // 문자열로부터 객체생성        
        Regex getNumber = new Regex("no=(\\d+)[^>]*>");
        Regex getCommentCount = new Regex("<a([^>]*?)view_comment=1[^>]*>\\[(\\d+)\\]</a>");
        Regex getName = new Regex("<a([^>]*?)title=\"([^\"]*)\"[^>]*>");
        Regex getTime = new Regex("<span title='([^']*)'");
        Regex getViewCount = new Regex("<td[^>]*><font[^>]*>(\\d+)</td>");

        Regex getArticleData = new Regex("<span class=\"list_right\"><span class=\"((list_pic_n)|(list_pic_y))\"></span>([^>]*)<span class=\"list_pic_re\">(\\[(\\d+)\\])?</span><br /><span class=\"list_pic_galler\">([^<]*)(<img[^>]*>)?<span>([^>]*)</span></span></span></a></li>");

        private List<Article> GetArticleListFromString(string input)
        {
            List<Article> result = new List<Article>();
            var sr = new StringReader(input);

            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (!line.Contains("\"list_picture_a\""))
                    continue;
                
                string line2 = sr.ReadLine();
                
                Article article = new Article();
                
                // Number
                Match matchGetNumber = getNumber.Match(line);
                if (!matchGetNumber.Success) break;
                article.ID = matchGetNumber.Groups[1].Value;

                Match matchArticleData = getArticleData.Match(line2);
                if (!matchArticleData.Success) continue;

                // HasImage
                article.HasImage = matchArticleData.Groups[3].Length != 0;
                article.Title = HttpUtility.HtmlDecode(matchArticleData.Groups[4].Value);
                article.CommentCount = matchArticleData.Groups[5].Length == 0 ? 0 : int.Parse(matchArticleData.Groups[6].Value);
                article.Name = HttpUtility.HtmlDecode(matchArticleData.Groups[7].Value).Trim();
                article.Date = DateTime.Parse(matchArticleData.Groups[9].Value);

                result.Add(article);
            }

            return result;
        }        

        // 글의 텍스트와 코멘트를 읽어서 채워넣는다
        bool UpdateArticleTextAndComments(Article article, string input)
        {
            // 1. 이미지들을 찾는다
            // <img id='dc_image_elm_*.. src="()"/> 이미지
            // <img  id=dc_image_elm0 src='http://dcimg1.dcinside.com/viewimage.php?id=windowsphone&no=29bcc427b78a77a16fb3dab004c86b6fc3a0be4a5f9fd1a8cc77865e83c2029da6f5d553d560d273a5d0802458ed844942b60ffcef4cc95a9e820f3d0eb76388a4ded971bc29b6cc1fd6a780e7e52f627fdf1b9b6a40491c7fa25f4acaa4663f080794f8abd4e01cc6&f_no=7bee837eb38760f73f8081e240847d6ecaa51e16c7795ecc2584471ef43a7f730867c7d42ef66cf9f0827af5263d'width=550  /></a><br/> <br/>        </p>

            StringEngine se = new StringEngine(input);

            article.Images.Clear();

            Regex imageRegex = new Regex("<img\\s+id=dc_image_elm[^>]*src='(http://dcimg[^']*)'", RegexOptions.IgnoreCase);
            Match match;

            while(se.Next(imageRegex, out match) )
            {
                article.Images.Add(new Uri(match.Groups[1].Value, UriKind.Absolute));                
            }

            // div를 개수를 세서 안에 있는 div 
            var textRegex = new Regex("<div id=\"memo_img\"[^>]*>");

            if(!se.Next(textRegex, out match))
                return false;

            int start = se.Cursor;
            int count = 1;
            var divRegex = new Regex("(<\\s*div[^>]*>)|(<\\s*/\\s*div\\s*>)", RegexOptions.IgnoreCase); // div 또는 /div

            while (count > 0)
            {
                if (!se.Next(divRegex, out match))
                    break;

                if (match.Groups[1].Value.Length != 0)
                {
                    count++;
                }
                else
                {
                    count--;
                    if (count == 0)
                        break;
                }                
            }

            if (count != 0)
            {
                article.Text = input.Substring(start).Trim();
                return true;
            }
            else
            {
                article.Text = input.Substring(start, match.Index - start ).Trim();
            }

            Regex commentStart = new Regex("<div class=\"m_reply_list m_list\">");
            Regex getCommentName = new Regex("<p>(<a[^>]*>)?\\[([^<]*)(<img[^>]*>)?\\](</a>)?</p>");
            Regex getCommentText = new Regex("<div class=\"m_list_text\">([^<]*)</div>");

            article.Comments.Clear();

            // 댓글 가져오기
            while (se.Next(commentStart, out match))
            {
                string line;                

                if (!se.GetNextLine(out line)) continue;
                match = getCommentName.Match(line);
                if (!match.Success) continue;

                var cmt = new Comment();
                cmt.Name = HttpUtility.HtmlDecode(match.Groups[2].Value.Trim());

                // 내용
                if (!se.Next(getCommentText, out match)) continue;
                cmt.Text = HttpUtility.HtmlDecode(match.Groups[1].Value.Trim());

                article.Comments.Add(cmt);
            }

            // CommentUserID 얻기
            Regex userRegex = new Regex("<input[^>]*id=\"user_no\"[^>]*value=\"(\\d+)\"/>");
            if (se.Next(userRegex, out match))
            {
                article.CommentUserID = match.Groups[1].Value;
            }

            return true;
        }        
    }

}
