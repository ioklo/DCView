using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using DCView.Adapter;
using DCView.Misc;

namespace DCView.Board
{
    class ClienSearchArticleLister : ILister<IArticle>
    {
        ClienBoard board;
        string id, searchText;

        public ClienSearchArticleLister(ClienBoard board, string id, string searchText, SearchType searchType)            
        {
            this.board = board;
            this.id = id;
            this.searchText = searchText;
        }

        public bool Next(out IEnumerable<IArticle> elems)
        {
            string url = string.Format("http://www.clien.net/cs2/bbs/board.php?bo_table={0}&sfl=wr_subject&stx={1}&{2}",
                AdapterFactory.Instance.UrlEncode(id),
                AdapterFactory.Instance.UrlEncode(searchText), DateTime.Now.Ticks);

            HttpClient client = new HttpClient();

            // client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17");
            client.DefaultRequestHeaders.Referrer = new Uri(url, UriKind.Absolute);

            var content = client.GetAsync(url).Result.Content;
            var result = content.ReadAsStringAsync().Result;

            /*string result = AdapterFactory.Instance.CreateWebClient(false).DownloadStringAsyncTask(
                new Uri(url, UriKind.Absolute))
                .Result;*/

            StringEngine se = new StringEngine(result);

            List<IArticle> articles = new List<IArticle>();
            bool curBool = true;

            while (true)
            {
                Match match;
                if (!se.Next(new Regex("<tr class=\"mytr\">"), out match)) break;

                string line;
                if (!se.GetNextLine(out line)) continue;

                match = Regex.Match(line, @"<td>(\d+)</td>");
                if (!match.Success) continue;

                string articleID = match.Groups[1].Value;
                int curArticleID = int.Parse(articleID);

                /*if (recentArticle <= curArticleID)
                    continue;

                recentArticle = curArticleID;*/

                ClienArticle article = new ClienArticle(board, articleID);

                article.HasImage = curBool;
                curBool = !curBool;

                // 글 제목과 댓글 개수
                if (!se.Next(new Regex(@"<td\s+class=""post_subject"">.*?<a[^>]*?>(.*?)</a>\s*(<span>\[(\d+)\]</span>)?"), out match)) continue;
                if (!match.Success) continue;

                article.Title = AdapterFactory.Instance.HtmlDecode(HtmlParser.StripTags(match.Groups[1].Value));
                if (match.Groups[3].Success)
                    article.CommentCount = int.Parse(match.Groups[3].Value);
                else
                    article.CommentCount = 0;

                // 이름
                if (!se.GetNextLine(out line)) continue;
                match = Regex.Match(line, @"<span class='member'>(.*?)</span>");
                if (match.Success)
                {
                    article.Name = match.Groups[1].Value;
                }
                else
                {
                    match = Regex.Match(line, @"<img src='/cs2/data/member/.*?/(.*?).gif");
                    if (!match.Success) continue;

                    article.Name = match.Groups[1].Value;
                }

                // 시간
                if (!se.GetNextLine(out line)) continue;
                match = Regex.Match(line, "<span title=\"([^\"]*?)\">");
                if (!match.Success) continue;

                article.Date = DateTime.Parse(match.Groups[1].Value);



                articles.Add(article);
            }

            elems = articles;            
            return true;
        }
    }
}
