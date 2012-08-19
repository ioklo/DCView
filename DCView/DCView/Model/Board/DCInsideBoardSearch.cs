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
using System.Threading.Tasks;
using System.Threading;
using DCView.Util;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using MyApps.Common;
using MyApps.Common.HtmlParser;

namespace DCView
{
    public class DCInsideBoardSearch : IBoard    
    {
        public enum SearchType
        {
            Subject,
            Content,
            Name,
        }

        // 내부 변수
        string id;
        string searchText;
        SearchType searchType;
        int page;
        int serpos = 0; // 현재 search pos
        ObservableCollection<Article> articleList;
        ReadOnlyObservableCollection<Article> readonlyArticleList;

        public ReadOnlyObservableCollection<Article> Articles
        {
            get { return readonlyArticleList; }
        }

        public DCInsideBoardSearch(string id, string searchText, SearchType searchType)
        {            
            this.id = id;
            this.searchText = searchText;
            this.searchType = searchType;
            this.articleList = new ObservableCollection<Article>();
            this.readonlyArticleList = new ReadOnlyObservableCollection<Article>(this.articleList);
        }

        public Uri GetBoardUri()
        {
            return new Uri(string.Format("http://gall.dcinside.com/list.php?id={0}", id), UriKind.Absolute);
        }

        public Uri GetArticleUri(Article article)
        {
            return new Uri(string.Format("http://gall.dcinside.com/list.php?id={0}&no={1}", id, article.ID), UriKind.Absolute);
        }

        // 
        public void ResetArticleList(int page)
        {
            this.page = page;
            articleList.Clear();
        }

        private string GetSearchTypeText(SearchType searchType)
        {
            switch (searchType)
            {
                case SearchType.Content: return "memo";
                case SearchType.Subject: return "subject";
                case SearchType.Name: return "name";
            }

            return string.Empty;
        }

        public Task GetNextArticleList(CancellationToken cts)
        {
            // 현재 페이지에 대해서 
            DCViewWebClient webClient = new DCViewWebClient();

            // 접속할 사이트
            string url = string.Format("http://m.dcinside.com/list.php?id={0}&page={1}&serVal={2}&s_type={3}&ser_pos={4}&nocache={5}", 
                id, 
                page + 1, 
                searchText,
                GetSearchTypeText(searchType),
                serpos != 0 ? serpos.ToString() : string.Empty,
                DateTime.Now.Ticks);

            // 페이지를 받고
            Task<string> result = webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute), cts);

            return result.ContinueWith( prevTask =>
            {
                if (prevTask.IsCanceled)
                {
                    if (cts.IsCancellationRequested)
                        cts.ThrowIfCancellationRequested();                    
                }

                if (prevTask.Exception != null)
                {
                    throw prevTask.Exception;
                }
                
                // 결과스트링에서 게시물 목록을 뽑아낸다.                
                List<Article> newArticles = GetArticleListFromString(prevTask.Result);
                AddArticles(newArticles);

                // 마지막 리스트였다면
                int nextSearchPos;
                if (IsLastSearch(prevTask.Result, out nextSearchPos))
                {
                    page = 0;
                    serpos = nextSearchPos;
                }
                else
                {
                    // 페이지 하나 증가시키고
                    page++;
                }
            });
        }

        private bool IsLastSearch(string input, out int nextSearchPos)
        {
            nextSearchPos = 0;

            Match match = Regex.Match(input, "<em(.*)class=\"pg_num_on21\">(\\d+)</em>/(\\d+)");
            if (!match.Success)
                return false;

            if (match.Success && match.Groups[2].Value != match.Groups[3].Value)
            {
                return false;
            }

            // 없다면 
            match = Regex.Match(input, "<button type='button' class='pg_btn21' ([^>]*)onclick=\"location.href='([^']*)ser_pos=-(\\d+)';\"\\s*>다음</button>");
            if (match.Success)
            {
                nextSearchPos = - int.Parse(match.Groups[3].Value);
                return true;
            }

            return false;
        }       

        public Task GetArticleText(Article article, CancellationToken cts)
        {
            article.Text = string.Empty;
            article.Comments.Clear();

            DCViewWebClient webClient = new DCViewWebClient();            
            string url = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}&nocache={2}", id, article.ID, DateTime.Now.Ticks);

            return webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute), cts)
            .ContinueWith( prevTask =>
            {
                if (prevTask.IsCanceled)
                {
                    throw new OperationCanceledException(cts);
                }

                if (prevTask.Exception != null) 
                {
                    throw prevTask.Exception;
                }

                if (!UpdateArticleTextAndComments(article, prevTask.Result))
                {
                    throw new Exception("파싱 에러");
                }
            });
        }

        public Task GetNextCommentList(Article article, CancellationToken cts)
        {
            throw new NotImplementedException();
        }

        public Task WriteArticle(string title, string text, List<Picture> pics, CancellationToken cts)
        {
            throw new NotImplementedException();
        }

        public Task WriteComment(Article article, string text, CancellationToken cts)
        {
            DCViewWebClient webClient = new DCViewWebClient();
        
            webClient.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            webClient.Headers["Referer"] = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}", id, article.ID);
            string data = string.Format(
                "id={0}&no={1}&comment_memo={2}&mode=comment&user_no={3}",
                HttpUtility.UrlEncode(id),
                HttpUtility.UrlEncode(article.ID),
                HttpUtility.UrlEncode(text),
                article.CommentUserID
                );

            return webClient.UploadStringAsyncTask(new Uri("http://m.dcinside.com/_option_write.php", UriKind.Absolute), "POST", data);
        }       


        private Regex getNumber = new Regex("no=(\\d+)[^>]*>");
        private Regex getArticleData = new Regex("<span class=\"list_right\"><span class=\"((list_pic_n)|(list_pic_y))\"></span>(.*?)<span class=\"list_pic_re\">(\\[(\\d+)\\])?</span><br /><span class=\"list_pic_galler\">(.*?)(<img[^>]*>)?<span>([^>]*)</span></span></span></a></li>");
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
                article.Title = HttpUtility.HtmlDecode(HtmlParser.StripTags(matchArticleData.Groups[4].Value)).Trim();
                article.CommentCount = matchArticleData.Groups[5].Length == 0 ? 0 : int.Parse(matchArticleData.Groups[6].Value);
                article.Name = HttpUtility.HtmlDecode(HtmlParser.StripTags(matchArticleData.Groups[7].Value)).Trim();
                article.Date = DateTime.Parse(matchArticleData.Groups[9].Value);

                result.Add(article);
            }

            return result;
        }

        private void AddArticles(List<Article> newArticles)
        {
            // 저번 아티클에서 가장 작았던 항목의 인덱스를 알아낸다.
            int lastItemIndex = int.MaxValue;
            if (articleList.Count > 0)
            {
                Article article = articleList[articleList.Count - 1];

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

                articleList.Add(newArticle);
            }
        }

        // 글의 텍스트와 코멘트를 읽어서 채워넣는다
        private bool UpdateArticleTextAndComments(Article article, string input)
        {
            // 1. 이미지들을 찾는다
            // <img id='dc_image_elm_*.. src="()"/> 이미지
            // <img  id=dc_image_elm0 src='http://dcimg1.dcinside.com/viewimage.php?id=windowsphone&no=29bcc427b78a77a16fb3dab004c86b6fc3a0be4a5f9fd1a8cc77865e83c2029da6f5d553d560d273a5d0802458ed844942b60ffcef4cc95a9e820f3d0eb76388a4ded971bc29b6cc1fd6a780e7e52f627fdf1b9b6a40491c7fa25f4acaa4663f080794f8abd4e01cc6&f_no=7bee837eb38760f73f8081e240847d6ecaa51e16c7795ecc2584471ef43a7f730867c7d42ef66cf9f0827af5263d'width=550  /></a><br/> <br/>        </p>

            StringEngine se = new StringEngine(input);

            article.Pictures.Clear();

            Regex imageRegex = new Regex("<img\\s+id=dc_image_elm[^>]*src='(http://dcimg[^']*)'", RegexOptions.IgnoreCase);
            Match match;

            while (se.Next(imageRegex, out match))
            {
                article.Pictures.Add(
                    new Picture(
                        new Uri(match.Groups[1].Value, UriKind.Absolute)));
            }

            // div를 개수를 세서 안에 있는 div 
            var textRegex = new Regex("<div id=\"memo_img\"[^>]*>");

            if (!se.Next(textRegex, out match))
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
                article.Text = input.Substring(start, match.Index - start).Trim();
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
