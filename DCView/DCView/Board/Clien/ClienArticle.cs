using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using DCView.Lib;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace DCView
{
    public class ClienArticle : IArticle
    {
        ClienBoard board;
        string id;
        List<Picture> pictures = new List<Picture>();

        public ClienArticle(ClienBoard board, string id)
        {
            this.board = board;
            this.id = id;
            HasImage = false;
        }

        public string ID
        {
            get { return id; }
        }

        public string Name{ get; set;}        
        public string Title {get; set;}        
        public DateTime Date {get; set;}
        public int CommentCount { get; set; }

        public bool CanWriteComment { get { return false; } }
      

        public Uri Uri
        {
            get { return new Uri(string.Format("http://clien.career.co.kr/cs2/bbs/board.php?bo_table={0}&wr_id={1}", ((IBoard)board).ID, id), UriKind.Absolute); }
        }

        public bool HasImage
        {
            get;
            set;
        }        

        public int ViewCount
        {
            get { return 0;  }
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

        public System.Collections.Generic.List<Picture> Pictures
        {
            get { return pictures; }
        }

        public ILister<IComment> GetCommentLister()
        {
            return new CommentLister(comments);
        }

        List<IComment> comments = new List<IComment>();

        public bool GetText(System.Threading.CancellationToken ct, out string text)
        {
            text = string.Empty;
            WebClientEx client = new WebClientEx();
            
            var result = client.DownloadStringAsyncTask(
                new Uri(string.Format("http://clien.career.co.kr/cs2/bbs/board.php?bo_table={0}&wr_id={1}", board.ID, id), UriKind.Absolute),
                ct).GetResult();

            StringEngine se = new StringEngine(result);

            Match match;

            pictures.Clear();

            if (se.Next(new Regex("<div class=\"attachedImage\"><img.*?src='(.*?)'"), out match))
            {
                var uri = new Uri(string.Format("http://clien.career.co.kr/cs2/bbs/" + match.Groups[1].Value), UriKind.Absolute);
                Picture pic = new Picture(uri, Uri.ToString());
                
                pictures.Add(pic);
                HasImage = true;
            }


            var textRegex = new Regex("<span id=\"writeContents\"(.*?)>");

            
            if (!se.Next(textRegex, out match)) return false;

            int start = se.Cursor;
            int count = 1;
            var divRegex = new Regex("(<\\s*span[^>]*>)|(<\\s*/\\s*span\\s*>)", RegexOptions.IgnoreCase); // div 또는 /div

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
                text = result.Substring(start).Trim();
                return true;
            }
            else
            {
                text = HttpUtility.HtmlDecode(result.Substring(start, match.Index - start).Trim());
            }

            comments.Clear();

            // 댓글 파트
            while (se.Next(new Regex("<ul class=\"reply_info\">"), out match))
            {
                string line;
                if (!se.GetNextLine(out line)) continue;

                ClienComment comment = new ClienComment();

                comment.Level = 0;

                if (line.Contains("<img src=\"../skin/board/cheditor/img/blet_re2.gif\">"))
                    comment.Level = 1;

                match = Regex.Match(line, "<img src='/cs2/data/member/.*?/(.*?).gif");
                if (match.Success)
                {
                    comment.Name = match.Groups[1].Value;
                }
                else
                {
                    match = Regex.Match(line, @"<span class='member'>(.*?)</span>");
                    if (match.Success)
                        comment.Name = match.Groups[1].Value;
                    else
                        continue;
                }

                if (!se.Next(new Regex("<div class=\"reply_content\">"), out match))
                    continue;                

                StringBuilder sb = new StringBuilder();

                while (se.GetNextLine(out line))
                {
                    match = Regex.Match(line, "(.*?)<span id='edit");
                    if (match.Success)
                    {
                        sb.Append(match.Groups[1].Value);
                        break;
                    }

                    sb.Append(line);
                }

                comment.Text = HttpUtility.HtmlDecode(sb.ToString().Trim());
                comments.Add(comment);
            }

            return true;
        }

        public bool WriteComment(string text, System.Threading.CancellationToken ct)
        {
            return false;
        }

        class CommentLister : ILister<IComment>
        {
            List<IComment> comments;
            public CommentLister(List<IComment> comments)
            {
                this.comments = comments;
            }

            public bool Next(System.Threading.CancellationToken ct, out IEnumerable<IComment> elems)
            {
                elems = comments;
                return false;
            }
        }
    }
}
