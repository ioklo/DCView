using System;
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
            return new ClienArticleLister(this, id, page);
        }
        
        public ILister<IArticle> GetSearchLister(string text, SearchType searchType)
        {
            return new ClienSearchArticleLister(this, id, text, searchType);
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
    }
}
