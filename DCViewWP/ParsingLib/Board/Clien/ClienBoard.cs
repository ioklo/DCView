﻿using System;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using DCView.Misc;
using DCView.Adapter;


namespace DCView.Board
{
    public class ClienBoard : IBoard
    {
        ISite site;
        string id;
        string name;

        public ClienBoard(ClienSite clien, string id)
        {
            this.site = clien;
            this.id = id;
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
            set { name = value; }
        }

        public bool CanWriteArticle { get { return false; } }

        public Uri Uri
        {
            get { return new Uri(string.Format("http://www.clien.net/cs2/bbs/board.php?bo_table={0}", id), UriKind.Absolute); }
        }

        public IEnumerable<IBoardOption> BoardOptions
        {
            get
            {
                yield break;
            }
        }

        ILister<IArticle> IBoard.GetArticleLister(int page)
        {
            return new ArticleLister(this, id, page);
        }

        [NotSupported]
        public ILister<IArticle> GetSearchLister(string text, SearchType searchType)
        {
            // 지원하지 않는 기능
            throw new NotSupportedException();
        }

        [NotSupported]
        public bool WriteArticle(string title, string text, System.Collections.Generic.List<AttachmentStream> attachments)
        {
            return false;
        }

        [NotSupported]
        public bool DeleteArticle(string articleID)
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

            public bool Next(out System.Collections.Generic.IEnumerable<IArticle> elems)
            {
                string result = AdapterFactory.Instance.CreateWebClient(false).DownloadStringAsyncTask(
                    new Uri(string.Format("http://www.clien.net/cs2/bbs/board.php?bo_table={0}&page={1}&{2}", id, page + 1, DateTime.Now.Ticks), UriKind.Absolute))
                    .Result;

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
                    if (!se.Next(new Regex(@"<td\s+class=""post_subject"">.*?<a[^>]*?>(.*?)</a>\s*(<span>\[(\d+)\]</span>)?"), out match)) continue;
                    if (!match.Success) continue;

                    article.Title = match.Groups[1].Value;
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
