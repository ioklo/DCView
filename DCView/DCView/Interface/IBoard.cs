using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.IO;

namespace DCView
{
    public class AttachmentStream
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public string ContentType { get; set; }

        public AttachmentStream()
        {
            ContentType = "application/octet-stream";
        }
        
    }

    public enum SearchType
    {
        Subject = 1,
        Content = 2,
        Name    = 4,
    }

    // 게시판이 할 수 있는 일들 모음
    public interface IBoard
    {
        ISite Site { get; }
        string ID { get; }   // 게시판 접근용 아이디
        string Name { get; } // 게시판의 이름

        Uri Uri { get; }
        ILister<IArticle> GetArticleLister(int page);
        ILister<IArticle> GetSearchLister(string text, SearchType searchType);
        bool WriteArticle(string title, string text, List<AttachmentStream> attachments, CancellationToken ct);
    }
}