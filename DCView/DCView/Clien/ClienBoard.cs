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
using DCView.Util;
using MyApps.Common;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCView
{
    public class ClienBoard : IBoard
    {
        ISite site;
        string id;
        string name;

        public ClienBoard(Clien clien, string id, string name)
        {
            this.site = clien;
            this.id = id;
            this.name = name;
        }

        public string DisplayTitle
        {
            get { return "클리앙 - " + name; }
        }
        
        public ISite Site
        {
            get { return site; }
        }

        public string ID
        {
            get { return id; }
        }

        public string Name
        {
            get { return name; }
        }

        public bool CanWriteArticle { get { return false; } }

        Uri IBoard.Uri
        {
            get { throw new NotImplementedException(); }
        }

        public bool CanSearch { get { return false; } }

        ILister<IArticle> IBoard.GetArticleLister(int page)
        {
            return new ArticleLister(this, id, page);
        }

        ILister<IArticle> IBoard.GetSearchLister(string text, SearchType searchType)
        {
            // 지원하지 않는 기능
            throw new NotSupportedException();
        }

        bool IBoard.WriteArticle(string title, string text, System.Collections.Generic.List<AttachmentStream> attachments, System.Threading.CancellationToken ct)
        {
            return false;
        }

        class ArticleLister : ILister<IArticle>
        {
            ClienBoard board;
            string id;
            int page;
            int recentArticle = int.MaxValue;

            public ArticleLister(ClienBoard board, string id, int page)
            {
                this.board = board;
                this.id = id;
                this.page = page;
            }

            public bool Next(System.Threading.CancellationToken ct, out System.Collections.Generic.IEnumerable<IArticle> elems)
            {
                WebClientEx client = new WebClientEx();

                string result = client.DownloadStringAsyncTask(
                    new Uri(string.Format("http://clien.career.co.kr/cs2/bbs/board.php?bo_table={0}&page={1}&{2}", id, page + 1, DateTime.Now.Ticks), UriKind.Absolute),
                    ct).GetResult();

                StringEngine se = new StringEngine(result);

                List<IArticle> articles = new List<IArticle>();
                bool curBool = true;

                while(true)
                {
                    Match match;
                    if (!se.Next(new Regex("<tr class=\"mytr\">"), out match)) break;

                    string line;
                    if (!se.GetNextLine(out line)) continue;

                    match = Regex.Match(line, @"<td>(\d+)</td>");
                    if (!match.Success) continue;
                    
                    string articleID = match.Groups[1].Value;
                    int curArticleID = int.Parse(articleID);

                    if (recentArticle <= curArticleID)
                        continue;

                    recentArticle = curArticleID;

                    ClienArticle article = new ClienArticle(board, articleID);

                    article.HasImage = curBool;
                    curBool = !curBool;

                    // 글 제목과 댓글 개수
                    if (!se.GetNextLine(out line)) continue;
                    match = Regex.Match(line, @"<a[^>]*?>(.*?)</a>\s*(<span>\[(\d+)\]</span>)?");
                    if (!match.Success) continue;

                    article.Title = HttpUtility.HtmlDecode(match.Groups[1].Value);
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
                page++;
                return true;
            }
        }
    }
}
