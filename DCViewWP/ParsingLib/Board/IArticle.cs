using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;

namespace DCView.Board
{
    public interface IArticle
    {
        // 기본 정보
        IBoard Board { get; }
        string ID { get; }
        string Name { get; }
        MemberStatus MemberStatus { get; }
        string Title { get; }
        DateTime Date { get; }

        Uri Uri { get; }

        // 글 리스트만 봤을 때 얻을 수 있는 정보, 나중에 구체적인 정보가 오면 쓸모 없어진다 
        bool HasImage { get; }    
        int CommentCount { get; }
        int ViewCount { get; }

        bool CanWriteComment { get; }

        List<Picture> Pictures { get; }
        ILister<IComment> GetCommentLister();

        // 지금 있는 Text를 invalidate 시키고 다시 얻어온다.
        bool GetText(out string text);
        bool WriteComment(string text);
    }
}
