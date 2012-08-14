using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DCView
{
    // 게시판이 할 수 있는 일들 모음
    public interface IBoard
    {
        ReadOnlyObservableCollection<Article> Articles { get; }

        // 
        Uri GetBoardUri();
        Uri GetArticleUri(Article article);

        // 보기
        void ResetArticleList(int page);
        Task GetNextArticleList(CancellationToken cts);
        
        Task GetArticleText(Article article, CancellationToken cts);
        Task GetNextCommentList(Article article, CancellationToken cts);

        // 쓰기
        Task WriteArticle(string title, string text, List<Picture> pics, CancellationToken cts);
        Task WriteComment(Article article, string text, CancellationToken cts);        
    }
}