using System;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCView.Lib;
using DCView.Lib.HtmlParser;
using DCView.Util;


namespace DCView
{
    public class DCInsideBoardSearchLister : ILister<IArticle>
    {
        int lastArticleID = int.MaxValue;

        DCInsideBoard board;        
        int page;
        int serpos;
        string searchText;
        SearchType searchType;

        private Regex getNumber = new Regex("no=(\\d+)[^>]*>");
        private Regex getArticleData = new Regex("<span class=\"list_right\"><span class=\"((list_pic_n)|(list_pic_y))\"></span>(.*?)<span class=\"list_pic_re\">(\\[(\\d+)\\])?</span><br /><span class=\"list_pic_galler\">(.*?)(<img[^>]*>)?<span>([^>]*)</span></span></span></a></li>");
        


        public DCInsideBoardSearchLister(DCInsideBoard board, string text, SearchType searchType )
        {
            this.board = board;
            this.page = 0;
            this.serpos = 0;
            this.searchText = text;
            this.searchType = searchType;
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

        public bool GetArticleList(CancellationToken ct, out IEnumerable<DCInsideArticle> articles)
        {
            // 현재 페이지에 대해서 
            DCViewWebClient webClient = new DCViewWebClient();

            string serposStr = serpos != 0 ? serpos.ToString() : string.Empty;

            // 접속할 사이트
            string url = string.Format("http://m.dcinside.com/list.php?id={0}&page={1}&serVal={2}&s_type={3}&ser_pos={4}&nocache={5}",
                board.ID,
                page + 1,
                searchText,
                GetSearchTypeText(searchType),
                serposStr,
                DateTime.Now.Ticks);

            // 페이지를 받고
            string result = webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute), ct).GetResult();

            // 결과스트링에서 게시물 목록을 뽑아낸다.                
            articles = GetArticleListFromString(result);

            // 마지막 리스트였다면
            int nextSearchPos;
            if (IsLastSearch(result, out nextSearchPos))
            {
                page = 0;
                serpos = nextSearchPos;
            }
            else
            {
                // 페이지 하나 증가시키고
                page++;
            }

            return true;
        }



        // 여기 리턴값의 의미는
        // 글들이 있고, 성공 (return true, result 있음)
        // 끝 (return false)
        // 실패 (exception)
        public bool Next(CancellationToken ct, out IEnumerable<IArticle> result)
        {
            result = null;

            IEnumerable<DCInsideArticle> articles;
            if (!GetArticleList(ct, out articles))
                throw new Exception(); // 글 가져오기 실패

            result = from article in articles
                     where int.Parse(article.ID) < lastArticleID
                     select (IArticle)article;
            
            return true;
        }

        private List<DCInsideArticle> GetArticleListFromString(string input)
        {
            List<DCInsideArticle> result = new List<DCInsideArticle>();
            var sr = new StringReader(input);

            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (!line.Contains("\"list_picture_a\""))
                    continue;

                string line2 = sr.ReadLine();

                DCInsideArticle article = new DCInsideArticle(board);

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
                nextSearchPos = -int.Parse(match.Groups[3].Value);
                return true;
            }

            return false;
        }       


    }

    public class DCInsideBoardSearch
    {
        
        // 내부 변수        
        string searchText;
        SearchType searchType;
        
        DCInsideBoard board;

        public DCInsideBoardSearch(DCInsideBoard board, string searchText, SearchType searchType)
        {
            this.board = board;            
            this.searchText = searchText;
            this.searchType = searchType;
        }

        public Uri Uri 
        {
            get { return board.Uri; }
        }

        
        //         
        
        
        
        
        
        public bool WriteArticle(string title, string text, List<AttachmentStream> attachments, CancellationToken ct)
        {
            return board.WriteArticle(title, text, attachments, ct);
        }
    }
}
