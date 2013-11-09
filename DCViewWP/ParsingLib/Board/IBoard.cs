using System;
using System.Collections.Generic;
using DCView.Misc;

namespace DCView.Board
{
    // 게시판이 할 수 있는 일들 모음
    public interface IBoard
    {
        ISite Site { get; }
        string ID { get; }   // 게시판 접근용 아이디
        string Name { get; set; } // 게시판의 이름
        string DisplayTitle { get; } // 화면 위에 보여줄 이름

        Uri Uri { get; }
        ILister<IArticle> GetArticleLister(int page);
        ILister<IArticle> GetSearchLister(string text, SearchType searchType);
        bool WriteArticle(string title, string text, List<AttachmentStream> attachments);
        bool DeleteArticle(string articleID);

        // 토글 가능한 옵션
        // 옵션 메뉴
        // 개념글 보기 켬/ 개념글 보기 끔/ bool
        // string, string
        IEnumerable<IBoardOption> BoardOptions { get; }
    }
}