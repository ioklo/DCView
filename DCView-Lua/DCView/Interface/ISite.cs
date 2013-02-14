using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView
{
    public interface ISite
    {
        string ID { get; }
        string Name { get; }                // 이 사이트의 대표 이름
        IEnumerable<IBoard> Boards { get; } // 이 사이트에서 접근할 수 있는 게시판
        bool CanLogin { get; }

        bool Refresh(Action<string, int> OnStatusChanged);
        void Load();    // 전체 보드를 다 읽어온다
        IBoard GetBoard(string boardID, string boardName);

        ICredential Credential { get; } // 로그인 정보
    }
}
