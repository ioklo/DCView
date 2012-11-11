using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Threading;
using System.Linq;
using MyApps.Common;
using System.Collections.Specialized;

using System.Text;
using DCView.Util;

namespace DCView
{
    public class DCInsideLister : ILister<IArticle>
    {
        DCInsideBoard board;
        int page;
        int lastArticleID = int.MaxValue;

        public DCInsideLister(DCInsideBoard board, int page)
        {
            this.board = board;
            this.page = page;
        }

        public bool Next(CancellationToken ct, out IEnumerable<IArticle> result)
        {
            result = null;
            IEnumerable<DCInsideArticle> articles;
            if (!board.GetArticleList(page, ct, out articles))
                throw new Exception();

            // 성공했으면 다음에 읽을 page를 하나 올려준다
            page++;

            var resultList = new List<IArticle>();
            int minID = int.MaxValue;
            foreach(var article in articles)
            {
                int id = int.Parse(article.ID);
                if (id < minID) minID = id;

                if (id < lastArticleID)
                    resultList.Add(article);                                    
            }

            lastArticleID = minID;
            result = resultList;
            return true;            
        }        
    }


    public class DCInsideBoard : IBoard
    {
        // 내부 변수
        DCInsideSite site;
        string id;
        string name;
        
        public DCInsideBoard(DCInsideSite site, string id, string name)
        {
            this.site = site;
            this.id = id;
            this.name = name;
        }

        public ISite Site { get { return site; } }
        public string Name { get { return name; } }
        public string ID { get { return id; } }
        public string DisplayTitle { get { return name + " 갤러리"; } }
        public bool CanWriteArticle { get { return true; } }
        public bool CanSearch { get { return true; } }

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
            return new DCInsideLister(this, page);
        }

        public ILister<IArticle> GetSearchLister(string text, SearchType type)
        {
            return new DCInsideBoardSearchLister(this, text, type);
        }

        // 동기 함수, 비동기 처리는 밖에서 해주는 것
        public bool WriteArticle(string title, string text, List<AttachmentStream> attachmentStreams, CancellationToken ct)
        {
            string code, mobileKey;
            if (!GetCodeAndMobileKey(ct, out code, out mobileKey))
                return false;

            string flData = null, oflData = null;
            if (attachmentStreams != null && attachmentStreams.Count > 0)
                if (!UploadPictures(attachmentStreams, out flData, out oflData))
                    return false; // 업로드 실패

            if (!UploadArticle(title, text, code, mobileKey, flData, oflData))
                return false;

            return true;
        }


        // 인터페이스 끝
        public Uri GetArticleUri(DCInsideArticle article)
        {
            return new Uri(string.Format("http://gall.dcinside.com/list.php?id={0}&no={1}", id, article.ID), UriKind.Absolute);
        }

        public bool GetArticleList(int page, CancellationToken ct, out IEnumerable<DCInsideArticle> articles)
        {
            // 현재 페이지에 대해서 
            DCViewWebClient webClient = new DCViewWebClient();

            // 접속할 사이트
            string url = string.Format("http://m.dcinside.com/list.php?id={0}&page={1}&nocache={2}", 
                id, 
                page + 1, 
                DateTime.Now.Ticks);

            // 페이지를 받고
            string result = webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute), ct).GetResult();
                
            // 결과스트링에서 게시물 목록을 뽑아낸다.                
            articles = GetArticleListFromString(result);

            return true;
        }       

        public bool GetArticleText(DCInsideArticle article, CancellationToken ct, out string text)
        {
            DCViewWebClient webClient = new DCViewWebClient();            
            string url = string.Format("http://m.dcinside.com/view.php?id={0}&no={1}&nocache={2}", id, article.ID, DateTime.Now.Ticks);

            string result = webClient.DownloadStringAsyncTask(new Uri(url, UriKind.Absolute), ct).GetResult();

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

        public Task GetNextCommentList(IArticle article, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
        
        
        
        private bool GetCodeAndMobileKey(CancellationToken ct, out string code, out string mobileKey)
        {
            code = null;
            mobileKey = null;

            // 일단 code와 mobile_key를 얻는다
            DCViewWebClient client = new DCViewWebClient();
            client.Headers["Referer"] = string.Format("http://m.dcinside.com/list.php?id={0}", id);

            string response = client.DownloadStringAsyncTask(
                new Uri("http://m.dcinside.com/write.php?id=windowsphone&mode=write", UriKind.Absolute),
                ct).GetResult();

            Regex codeRegex = new Regex("<input type=\"hidden\" name=\"code\" value=\"([^\"]*)\"");

            StringEngine se = new StringEngine(response);
            Match match;

            if (!se.Next(codeRegex, out match))
                return false;

            code = match.Groups[1].Value;

            Regex mobileKeyRegex = new Regex("<input type=\"hidden\" name=\"mobile_key\" id=\"mobile_key\" value=\"([^\"]*)\"");
            if (!se.Next(mobileKeyRegex, out match))
                return false;


            mobileKey = match.Groups[1].Value;
            return true;
        }

        public bool WriteComment(DCInsideArticle article, string text, CancellationToken ct)
        {
            if (article.CommentUserID.Length == 0)
            {
                // 한번만 더 시도하고, (이전에 로그인을 안했을 수도 있으므로)
                string dummyText;
                if (!GetArticleText(article, ct, out dummyText))
                    return false;

                // 그래도 commentUserID를 얻지 못했다면
                if (article.CommentUserID.Length == 0)
                    return false;
            }

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

            webClient.UploadStringAsyncTask(new Uri("http://m.dcinside.com/_option_write.php", UriKind.Absolute), "POST", data, ct).GetResult();
            return true;
        }

        private static Regex getNumber = new Regex("no=(\\d+)[^>]*>");
        private static Regex getArticleData = new Regex("<span class=\"list_right\"><span class=\"((list_pic_n)|(list_pic_y))\"></span>([^>]*)<span class=\"list_pic_re\">(\\[(\\d+)\\])?</span><br /><span class=\"list_pic_galler\">([^<]*)(<img[^>]*>)?<span>([^>]*)</span></span></span></a></li>");
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

                DCInsideArticle article = new DCInsideArticle(this);

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

            Regex imageRegex = new Regex("<img\\s+id=dc_image_elm[^>]*src='(http://dcimg[^']*)'", RegexOptions.IgnoreCase);
            Match match;

            while (se.Next(imageRegex, out match))
            {
                pictures.Add(
                    new Picture(
                        new Uri(match.Groups[1].Value, UriKind.Absolute), "http://gall.dcinside.com"));
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
                text = input.Substring(start).Trim();
                return true;
            }
            else
            {
                text = input.Substring(start, match.Index - start).Trim();
            }

            Regex commentStart = new Regex("<div\\s+class=\"m_reply_list m_list\">");
            Regex getCommentName = new Regex("<p>(<a[^>]*>)?\\[([^<]*)(<img[^>]*>)?\\](</a>)?</p>");
            Regex getCommentText = new Regex("<div class=\"m_list_text\">([^<]*)</div>");

            comments.Clear();

            // 댓글 가져오기
            while (se.Next(commentStart, out match))
            {
                string line;

                if (!se.GetNextLine(out line)) continue;
                match = getCommentName.Match(line);
                if (!match.Success) continue;

                var cmt = new DCInsideComment();
                cmt.Level = 0;
                cmt.Name = HttpUtility.HtmlDecode(match.Groups[2].Value.Trim());

                // 내용
                if (!se.Next(getCommentText, out match)) continue;
                cmt.Text = HttpUtility.HtmlDecode(match.Groups[1].Value.Trim());

                comments.Add(cmt);
            }

            // CommentUserID 얻기
            Regex userRegex = new Regex("<input[^>]*id=\"user_no\"[^>]*value=\"(\\d+)\"/>");
            if (se.Next(userRegex, out match))
            {
                commentUserID = match.Groups[1].Value;
            }

            return true;
        }

        private bool UploadPictures(List<AttachmentStream> attachmentStreams, out string flData, out string oflData)
        {
            // 그림 업로드
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(@"http://upload.dcinside.com/upload_imgfree_mobile.php");

            string boundary = DateTime.Now.Ticks.ToString("x");
            httpRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpRequest.CookieContainer = WebClientEx.CookieContainer;
            httpRequest.Method = "POST";

            Task<Stream> requestStreamTask = Task.Factory.FromAsync<Stream>(
                httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, null);
            requestStreamTask.Wait();

            Stream stream = requestStreamTask.Result;

            var writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            // upload[0]의 이름을 업로드
            int count = 0;
            foreach (var attachmentStream in attachmentStreams)
            {
                // 헤더
                writer.WriteLine("--" + boundary);
                writer.WriteLine("Content-Disposition: form-data; name=\"upload[{0}]\"; filename=\"{1}\"",
                        count,
                        attachmentStream.Filename
                    );
                writer.WriteLine("Content-Type: {0}", attachmentStream.ContentType);
                writer.WriteLine(); // 헤더가 끝났을 때 새 줄

                attachmentStream.Stream.CopyTo(stream);

                writer.WriteLine(); // 내용이 끝났을 때 새 줄을 넣어준다

                count++;
            }

            // 그 다음에 붙여야 할 것들..
            // upload의 끝을 알려야 하나..
            writer.WriteLine("--" + boundary);
            writer.WriteLine("Content-Disposition: form-data; name=\"upload[{0}]\"; filename=\"\"", count);
            writer.WriteLine("Content-Type: application/octet-stream");
            writer.WriteLine(); // 헤더가 끝났을 때 새 줄

            // 아무 내용 없고,
            writer.WriteLine(); // 내용이 끝났을 때 새 줄을 넣어준다

            var dict = new Dictionary<string, string>();
            dict.Add("imgId", id);
            dict.Add("mode", "write");
            dict.Add("mobile_key", "1");

            foreach (var kv in dict)
            {
                // 여기에 필요한 변수들을 넣는다
                writer.WriteLine("--" + boundary);
                writer.WriteLine("Content-Disposition: form-data; name=\"{0}\"", kv.Key);
                writer.WriteLine();
                writer.WriteLine(kv.Value);
            }
            // 스트림의 끝을 알림
            writer.WriteLine("--" + boundary + "--");
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)Task<WebResponse>.Factory.FromAsync(
                httpRequest.BeginGetResponse, 
                httpRequest.EndGetResponse, 
                null).GetResult();
            Stream responseStream = response.GetResponseStream();

            // 여기서 FL_DATA와 OFL_DATA를 뽑아낸다
            Regex fl_dataRegex = new Regex(@"\('FL_DATA'\).value\s+=\s+'([^']*?)'");
            Regex ofl_dataRegex = new Regex(@"\('OFL_DATA'\)\.value\s*=\s*'([^']*?)'");

            flData = null;
            oflData = null;
            using (var reader = new StreamReader(responseStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (flData == null)
                    {
                        var flDataRegexMatch = fl_dataRegex.Match(line);
                        if (flDataRegexMatch.Success)
                            flData = flDataRegexMatch.Groups[1].Value;
                        continue;
                    }

                    if (oflData == null)
                    {
                        var oflDataRegexMatch = ofl_dataRegex.Match(line);
                        if (oflDataRegexMatch.Success)
                            oflData = oflDataRegexMatch.Groups[1].Value;
                    }
                }
            }

            return true;
        }

        private bool UploadArticle(string title, string text, string code, string mobileKey, string flData, string oflData)
        {
            // 이제 
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(@"http://upload.dcinside.com/g_write.php");

            string boundary = "---------" + DateTime.Now.Ticks.ToString("x");
            httpRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpRequest.CookieContainer = WebClientEx.CookieContainer;
            httpRequest.Method = "POST";

            httpRequest.Headers["Referer"] = string.Format("http://m.dcinside.com/write.php?id={0}&mode=write", id);
            // httpRequest.Referer = string.Format("http://m.dcinside.com/write.php?id={0}&mode=write", id);

            Stream stream = Task<Stream>.Factory.FromAsync(
                httpRequest.BeginGetRequestStream, httpRequest.EndGetRequestStream, null).GetResult();            
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;

            
            var nvc = new List<Tuple<string, string>>();

            nvc.Add(Tuple.Create("subject", title));
            nvc.Add(Tuple.Create("memo", text));
            // nvc.Add("user_id", HttpUtility.UrlEncode("dcviewtest"));
            nvc.Add(Tuple.Create("mode", "write"));
            nvc.Add(Tuple.Create("id", id));
            nvc.Add(Tuple.Create("code", code));
            nvc.Add(Tuple.Create("mobile_key", mobileKey));
            if (flData != null)
                nvc.Add(Tuple.Create("FL_DATA", flData));

            if (oflData != null)
                nvc.Add(Tuple.Create("OFL_DATA", oflData));

            foreach (var kv in nvc)
            {
                // 여기에 필요한 변수들을 넣는다
                writer.WriteLine("--" + boundary);
                writer.WriteLine("Content-Disposition: form-data; name=\"{0}\"", kv.Item1);
                writer.WriteLine("Content-Type: text/plain; charset=utf-8");
                writer.WriteLine();
                writer.WriteLine(kv.Item2);
            }

            // 다 됐으면 
            writer.WriteLine("--" + boundary + "--");
            stream.Close();

            WebResponse response = Task<WebResponse>.Factory.FromAsync(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null).GetResult();

            var mem = new MemoryStream();
            response.GetResponseStream().CopyTo(mem);

            string s = Encoding.UTF8.GetString(mem.ToArray(), 0, (int)mem.Length);

            int a = s.Length;

            return true;
        }
    }
}
