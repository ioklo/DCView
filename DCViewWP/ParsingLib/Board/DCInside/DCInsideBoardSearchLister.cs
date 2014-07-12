using System;
using System.Net;
using System.Collections.ObjectModel;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCView.Adapter;
using DCView.Misc;
using System.Text;

namespace DCView.Board
{
    public class DCInsideBoardSearchLister : ILister<IArticle>
    {
        int lastArticleID = int.MaxValue;

        DCInsideBoard board;        
        int page;
        int serpos;
        string searchText;
        SearchType searchType;        

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

        public bool GetArticleList(out IEnumerable<DCInsideArticle> articles)
        {
            // 현재 페이지에 대해서 
            var webClient = AdapterFactory.Instance.CreateWebClient();

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
            string result = webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute)).Result;

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
        public bool Next(out IEnumerable<IArticle> result)
        {
            result = null;

            IEnumerable<DCInsideArticle> articles;
            if (!GetArticleList(out articles))
                throw new Exception(); // 글 가져오기 실패

            result = from article in articles
                     where int.Parse(article.ID) < lastArticleID
                     select (IArticle)article;
            
            return true;
        }

        private List<DCInsideArticle> GetArticleListFromString(string input)
        {
            List<DCInsideArticle> result = new List<DCInsideArticle>();

            foreach (Match match in DCRegexManager.SearchListArticles.Matches(input))
            {
                DCInsideArticle article = new DCInsideArticle(board);

                article.ID = match.Groups["ID"].Value;
                article.HasImage = (match.Groups["HasImage"].Value == "ico_p_y");
                article.Title = AdapterFactory.Instance.HtmlDecode(HtmlParser.StripTags(match.Groups["Title"].Value.Trim()));
                article.CommentCount = match.Groups["ReplyCount"].Length == 0 ? 0 : int.Parse(match.Groups["ReplyCount"].Value);
                article.Name = AdapterFactory.Instance.HtmlDecode(HtmlParser.StripTags(match.Groups["Name"].Value.Trim()));
                article.Date = DateTime.Parse(match.Groups["Date"].Value);

                var statusValue = match.Groups["MemberStatus"].Value;
                if (statusValue == "fixed")
                    article.MemberStatus = MemberStatus.Default;
                else if (statusValue == "flow")
                    article.MemberStatus = MemberStatus.Fix;
                else
                    article.MemberStatus = MemberStatus.Anonymous;

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
}
