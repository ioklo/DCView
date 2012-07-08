using System;
using System.Net;
using System.Collections.ObjectModel;
using MyApps.DCView;
using System.Threading.Tasks;
using MyApps.Common;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace MyApps.Models
{
    // 글 목록을 관리하는 클래스
    // - 글 목록 읽어들이기
    // - 글 전체 제거
    public class ArticleList
    {
        private List<Article> articles = new List<Article>();
        private int page;

        public IList<Article> Articles { get { return articles; } }

        public ArticleList()
        {
            this.page = 0; // 0페이지부터 시작
        }

        // 글 목록을 제거한다
        public void Clear()
        {
            articles.Clear();
            page = 0;
        }

        // 다음 목록을 얻어오는 코드         
        public Task<bool> GetArticlesAsync(string id, string site, string pcsite)
        {
            WebClientEx webClient = new WebClientEx();
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";            

            string url = string.Format("http://{0}/list.php?id={1}&page={2}&nocache={3}", site, id, page + 1, DateTime.Now.Ticks);

            return webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute))
            .ContinueWith<bool>((prevTask) =>
            {
                if (prevTask.IsCanceled || prevTask.Exception != null) 
                {
                    return false;
                }

                // 페이지 하나 증가시키고
                page++;

                // 결과스트링에서 게시물 목록을 뽑아낸다.                
                List<Article> newArticles = GetArticleListFromString(prevTask.Result);    

                // 게시물 목록중에서 지금 게시물과 중복되는 것을 제외하고 추가한다.
                AddArticles(newArticles);

                return true;
            });
        }
        
        // 문자열로부터 객체생성        
        private Regex getNumber = new Regex("no=(\\d+)[^>]*>");
        private Regex getArticleData = new Regex("<span class=\"list_right\"><span class=\"((list_pic_n)|(list_pic_y))\"></span>([^>]*)<span class=\"list_pic_re\">(\\[(\\d+)\\])?</span><br /><span class=\"list_pic_galler\">([^<]*)(<img[^>]*>)?<span>([^>]*)</span></span></span></a></li>");

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

        private void AddArticles(List<Article> newArticles)
        {
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
            }
        }
    }
}
