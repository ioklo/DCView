using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCView.Board
{
    public interface ISite
    {
        string ID { get; }
        string Name { get; }                // 이 사이트의 대표 이름
        bool CanLogin { get; }

        bool Refresh(Action<string, int> OnStatusChanged);

        // 전체 보드를 다 읽어들인다
        Task<IEnumerable<IBoard>> GetBoards();
        IBoard GetBoard(string boardID, string boardName);

        IBoard GetBoardByURL(string url);
        IArticle GetArticleByURL(string url);

        ICredential Credential { get; } // 로그인 정보
    }
}
