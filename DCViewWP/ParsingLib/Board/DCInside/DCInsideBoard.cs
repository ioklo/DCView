using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DCView.Misc;
using DCView.Adapter;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace DCView.Board
{
    public class DCInsideBoard : IBoard
    {
        // 내부 변수
        DCInsideSite site;
        string id;
        string name;
        
        public DCInsideBoard(DCInsideSite site, string id)
        {
            this.site = site;
            this.id = id;
        }

        public ISite Site { get { return site; } }
        public string Name { get { return name; } set { name = value; } }
        public string ID { get { return id; } }
        public string DisplayTitle { get { return name + " 갤러리"; } }
        public bool ViewRecommend { get; set; }

        // 개념글
        public class ViewRecommendOption : IBoardOption
        {
            DCInsideBoard board;

            public ViewRecommendOption(DCInsideBoard board)
            {
                this.board = board;
            }

            public string Display { get { return "개념글 보기"; } }

            public bool Toggle
            {
                get
                {
                    return board.ViewRecommend;
                }
                set
                {
                    board.ViewRecommend = value;
                }
            }
        }

        public IEnumerable<IBoardOption> BoardOptions
        {
            get
            {
                yield return new ViewRecommendOption(this);
            }
        }

        // 인터페이스 
        public Uri Uri
        {
            get
            {
                return new Uri(string.Format("http://gall.dcinside.com/list.php?id={0}", id), UriKind.Absolute);
            }
        }

        public ILister<IArticle> GetArticleLister(int page)
        {
            return new DCInsideLister(this, page, ViewRecommend);
        }

        public ILister<IArticle> GetSearchLister(string text, SearchType type)
        {
            return new DCInsideBoardSearchLister(this, text, type);
        }

        // 동기 함수, 비동기 처리는 밖에서 해주는 것
        public bool WriteArticle(string title, string text, List<AttachmentStream> attachmentStreams)
        {
            string code, mobileKey;
            if (!GetCodeAndMobileKey(out code, out mobileKey))
                return false;

            string flData = null, oflData = null;
            if (attachmentStreams != null && attachmentStreams.Count > 0)
                if (!UploadPictures(attachmentStreams, out flData, out oflData))
                    return false; // 업로드 실패

            if (!UploadArticle(title, text, code, mobileKey, flData, oflData))
                return false;

            return true;
        }

        public bool DeleteArticle(string articleID)
        {
            // 성공했다는것은 어찌 알 수 있을까..            
            // 내 UserID를 알아야 한다
            string url = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}&nocache={2}", id, articleID, DateTime.Now.Ticks);
            string result = AdapterFactory.Instance.CreateWebClient().DownloadStringAsyncTask(new Uri(url, UriKind.Absolute)).Result;

            var userIDRegex = new Regex(@"<input type=""hidden"" name=""user_no"" id=""user_no"" value=""(\d+)"">");

            var match = userIDRegex.Match(result);
            if (!match.Success)
                return false;

            var userNo = match.Groups[1].Value;

            var client = AdapterFactory.Instance.CreateWebClient();

            client.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            client.SetHeader("Referer", string.Format("http://m.dcinside.com/view.php?id={0}&no={1}", id, articleID));

            string data = string.Format(
                "id={0}&no={1}&mode=board_del&user_no={2}&page=1",
                AdapterFactory.Instance.UrlEncode(id),
                AdapterFactory.Instance.UrlEncode(articleID),
                AdapterFactory.Instance.UrlEncode(userNo)
                );

            result = client.UploadStringAsyncTask(new Uri("http://m.dcinside.com/_option_write.php", UriKind.Absolute), "POST", data).Result;
            return result == "1";
        }

        // 인터페이스 끝
        public Uri GetArticleUri(DCInsideArticle article)
        {
            return new Uri(string.Format("http://gall.dcinside.com/list.php?id={0}&no={1}", id, article.ID), UriKind.Absolute);
        }

        public bool GetArticleList(int page, bool bRecommend, out IEnumerable<DCInsideArticle> articles)
        {
            // 접속할 사이트
            string url;

            if (bRecommend)
            {
                url = string.Format("http://m.dcinside.com/list.php?id={0}&recommend=1&page={1}&nocache={2}",
                id,
                page + 1,
                DateTime.Now.Ticks);
            }
            else
            {
                url = string.Format("http://m.dcinside.com/list.php?id={0}&page={1}&nocache={2}",
                id,
                page + 1,
                DateTime.Now.Ticks);
            }

            // 페이지를 받고
            string result = AdapterFactory.Instance.CreateWebClient().DownloadStringAsyncTask(new Uri(url, UriKind.Absolute)).Result;
                
            // 결과스트링에서 게시물 목록을 뽑아낸다.                
            articles = GetArticleListFromString(result);

            return true;
        }       

        public bool GetArticleText(DCInsideArticle article, out string text)
        {            
            string url = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}&nocache={2}", id, article.ID, DateTime.Now.Ticks);

            string result = AdapterFactory.Instance.CreateWebClient().DownloadStringAsyncTask(new Uri(url, UriKind.Absolute)).Result;

            List<Picture> pictures;
            List<DCInsideComment> comments;
            string commentUserID;
            if (!UpdateArticleTextAndComments(result, out pictures, out comments, out commentUserID, out text))
                return false; // 파싱 에러

            article.Pictures = pictures;
            article.CachedComments = comments;
            article.CommentUserID = commentUserID;            
            
            return true;
        }

        private bool GetCodeAndMobileKey(out string code, out string mobileKey)
        {
            code = null;
            mobileKey = null;

            // 일단 code와 mobile_key를 얻는다            
            var client = AdapterFactory.Instance.CreateWebClient();
            client.SetHeader("Referer", string.Format("http://m.dcinside.com/list.php?id={0}", id));

            string response = client.DownloadStringAsyncTask(
                new Uri(
                    string.Format("http://m.dcinside.com/write.php?id={0}&mode=write", id), UriKind.Absolute)).Result;
            
            StringEngine se = new StringEngine(response);
            Match match;

            if (!se.Next(DCRegexManager.WriteCode, out match))
                return false;

            code = match.Groups[1].Value;

            if (!se.Next(DCRegexManager.WriteMobileKey, out match))
                return false;


            mobileKey = match.Groups[1].Value;
            return true;
        }

        public bool WriteCommentNonmember(DCInsideArticle article, string text)
        {
            var client = AdapterFactory.Instance.CreateWebClient();

            client.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            client.SetHeader("Referer", string.Format("http://m.dcinside.com/view.php?id={0}&no={1}", id, article.ID));
            
            string data = string.Format(
                "id={0}&no={1}&comment_nick={2}&comment_pw={3}&comment_memo={4}&mode=comment_nonmember",
                AdapterFactory.Instance.UrlEncode(id),
                AdapterFactory.Instance.UrlEncode(article.ID),
                AdapterFactory.Instance.UrlEncode("testid"),
                AdapterFactory.Instance.UrlEncode("testpw"),
                AdapterFactory.Instance.UrlEncode(text)
                );

            client.UploadStringAsyncTask(new Uri("http://m.dcinside.com/_option_write.php", UriKind.Absolute), "POST", data).Wait();
            return true;
        }

        public bool WriteComment(DCInsideArticle article, string text)
        {
            if (article.CommentUserID.Length == 0)
            {
                // 한번만 더 시도하고, (이전에 로그인을 안했을 수도 있으므로)
                string dummyText;
                if (!GetArticleText(article, out dummyText))
                    return false;

                // 그래도 commentUserID를 얻지 못했다면
                if (article.CommentUserID.Length == 0)
                    return false;
            }

            var client = AdapterFactory.Instance.CreateWebClient();

            client.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            client.SetHeader("Referer", string.Format("http://m.dcinside.com/view.php?id={0}&no={1}", id, article.ID));
            string data = string.Format(
                "id={0}&no={1}&comment_memo={2}&mode=comment&user_no={3}",
                AdapterFactory.Instance.UrlEncode(id),
                AdapterFactory.Instance.UrlEncode(article.ID),
                AdapterFactory.Instance.UrlEncode(text),
                article.CommentUserID
                );

            var result = client.UploadStringAsyncTask(new Uri("http://m.dcinside.com/_option_write.php", UriKind.Absolute), "POST", data).Result;
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

                // 임시코드: 9줄을 합쳐서 읽어들인다
                var sb = new StringBuilder();
                for (int t = 0; t < 9; t++)
                    sb.AppendLine(sr.ReadLine());
                string line2 = sb.ToString();

                DCInsideArticle article = new DCInsideArticle(this);

                // Number
                Match matchGetNumber = DCRegexManager.ListArticleNumber.Match(line);
                if (!matchGetNumber.Success) break;
                article.ID = matchGetNumber.Groups[1].Value;

                Match matchArticleData = DCRegexManager.ListArticleData.Match(line2);
                if (!matchArticleData.Success) continue;

                article.HasImage = matchArticleData.Groups["HasPicture"].Length != 0;
                article.Title = matchArticleData.Groups["Title"].Value;
                article.CommentCount = matchArticleData.Groups["ReplyCount"].Length == 0 ? 0 : int.Parse(matchArticleData.Groups["ReplyCount"].Value);
                article.Name = matchArticleData.Groups["Name"].Value.Trim();
                article.Date = DateTime.Parse(matchArticleData.Groups["Date"].Value);

                if (line2.Contains("gallercon.gif"))
                    article.MemberStatus = MemberStatus.Fix;
                else if (line2.Contains("gallercon1.gif"))
                    article.MemberStatus = MemberStatus.Default;
                else
                    article.MemberStatus = MemberStatus.Anonymous;


                result.Add(article);
            }

            return result;
        }

        // 글의 텍스트와 코멘트를 읽어서 채워넣는다
        private bool UpdateArticleTextAndComments(string input, out List<Picture> pictures, out List<DCInsideComment> comments, out string commentUserID, out string text)
        {
            text = string.Empty;
            pictures = new List<Picture>();
            comments = new List<DCInsideComment>();
            commentUserID = string.Empty;

            // 1. 이미지들을 찾는다
            // <img id='dc_image_elm_*.. src="()"/> 이미지
            // <img  id=dc_image_elm0 src='http://dcimg1.dcinside.com/viewimage.php?id=windowsphone&no=29bcc427b78a77a16fb3dab004c86b6fc3a0be4a5f9fd1a8cc77865e83c2029da6f5d553d560d273a5d0802458ed844942b60ffcef4cc95a9e820f3d0eb76388a4ded971bc29b6cc1fd6a780e7e52f627fdf1b9b6a40491c7fa25f4acaa4663f080794f8abd4e01cc6&f_no=7bee837eb38760f73f8081e240847d6ecaa51e16c7795ecc2584471ef43a7f730867c7d42ef66cf9f0827af5263d'width=550  /></a><br/> <br/>        </p>

            StringEngine se = new StringEngine(input);
            
            Match match;

            while (se.Next(DCRegexManager.TextImage, out match))
            {
                string url = match.Groups[1].Value;
                string browseurl = url;

                int fIdx = url.IndexOf("&f_no=");
                if (fIdx != -1)
                    url = url.Substring(0, fIdx);

                pictures.Add(new Picture(url, browseurl));
            }

            // div를 개수를 세서 안에 있는 div 
            if (!se.Next(DCRegexManager.TextStart, out match))
                return false;

            int start = se.Cursor;
            int count = 1;            

            while (count > 0)
            {
                if (!se.Next(DCRegexManager.TextDIV, out match))
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
                text = input.Substring(start).Trim();
                return true;
            }
            else
            {
                text = input.Substring(start, match.Index - start).Trim();
            }

            comments.Clear();

            // 댓글 가져오기
            while (se.Next(DCRegexManager.CommentStart, out match))
            {
                string line;

                if (!se.GetNextLine(out line)) continue;
                match = DCRegexManager.CommentName.Match(line);
                if (!match.Success) continue;

                var cmt = new DCInsideComment();
                cmt.Level = 0;
                cmt.Name = match.Groups[2].Value.Trim();

                if (line.Contains("gallercon.gif"))
                    cmt.MemberStatus = MemberStatus.Fix;
                else if (line.Contains("gallercon1.gif"))
                    cmt.MemberStatus = MemberStatus.Default;
                else
                    cmt.MemberStatus = MemberStatus.Anonymous;

                // 내용
                if (!se.Next(DCRegexManager.CommentText, out match)) continue;
                cmt.Text = match.Groups[1].Value.Trim();

                comments.Add(cmt);
            }

            // CommentUserID 얻기            
            if (se.Next(DCRegexManager.TextCommentUserID, out match))
            {
                commentUserID = match.Groups[1].Value;
            }

            return true;
        }

        private bool UploadPictures(List<AttachmentStream> attachmentStreams, out string flData, out string oflData)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = AdapterFactory.Instance.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                MultipartFormDataContent content = new MultipartFormDataContent();

                int count = 0;
                foreach (var attachmentStream in attachmentStreams)
                {
                    var streamContent = new StreamContent(attachmentStream.Stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(attachmentStream.ContentType);
                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = attachmentStream.Filename,
                        Name = string.Format("upload[{0}]", count),
                    };

                    content.Add(streamContent);
                    count++;
                }

                content.Add(new StringContent(id), "imgId");
                content.Add(new StringContent("write"), "mode");
                content.Add(new StringContent("6"), "img_num");
                content.Add(new StringContent("mobile_key"), "1");

                var msg = client.PostAsync(@"http://upload.dcinside.com/upload_imgfree_mobile.php", content).Result;
                var responseStream = msg.Content.ReadAsStreamAsync().Result;

                // 여기서 FL_DATA와 OFL_DATA를 뽑아낸다                
                flData = null;
                oflData = null;
                using (var reader = new StreamReader(responseStream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (flData == null)
                        {
                            var flDataRegexMatch = DCRegexManager.WriteFLData.Match(line);
                            if (flDataRegexMatch.Success)
                                flData = flDataRegexMatch.Groups[1].Value;
                            continue;
                        }

                        if (oflData == null)
                        {
                            var oflDataRegexMatch = DCRegexManager.WriteOFLData.Match(line);
                            if (oflDataRegexMatch.Success)
                                oflData = oflDataRegexMatch.Groups[1].Value;
                            continue;
                        }

                        break;
                    }
                }
            }            

            return true;
        }

        private bool UploadArticle(string title, string text, string code, string mobileKey, string flData, string oflData)
        {
            using (var handler = new HttpClientHandler() { CookieContainer = AdapterFactory.Instance.CookieContainer })
            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.Referrer = new Uri(string.Format("http://m.dcinside.com/write.php?id={0}&mode=write", id), UriKind.Absolute);
                MultipartFormDataContent content = new MultipartFormDataContent();

                content.Add(new StringContent(title), "subject");
                content.Add(new StringContent(text), "memo");                
                content.Add(new StringContent("write"), "mode");                
                content.Add(new StringContent(id), "id");
                content.Add(new StringContent(code), "code");
                content.Add(new StringContent(mobileKey), "mobile_key");                

                if (flData != null)
                    content.Add(new StringContent(flData), "FL_DATA");

                if (oflData != null)
                    content.Add(new StringContent(oflData), "OFL_DATA");

                var response = client.PostAsync(@"http://upload.dcinside.com/g_write.php", content).Result;                
            }

            return true;
        }
    }
}
