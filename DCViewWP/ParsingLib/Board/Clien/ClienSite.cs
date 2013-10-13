using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DCView.Board
{
    public class ClienSite : ISite
    {
        List<IBoard> boards = new List<IBoard>();

        public ClienSite()
        {
            boards.Add(new ClienBoard(this, "park", "모두의공원"));
            boards.Add(new ClienBoard(this, "news", "새로운소식"));
            boards.Add(new ClienBoard(this, "use", "사용기게시판"));
            boards.Add(new ClienBoard(this, "kin", "아무거나질문"));
            boards.Add(new ClienBoard(this, "lecture", "팁과강좌"));
        }

        ICredential ISite.Credential { get { return null; } }

        string ISite.ID
        {
            get { return "clien"; }
        }

        string ISite.Name
        {
            get { return "클리앙"; }
        }

        bool ISite.CanLogin
        {
            get { return false; }
        }
        
        bool ISite.Refresh(Action<string, int> OnStatusChanged)
        {
            // refresh 하지 않음
            return true;
        }
        
        IBoard ISite.GetBoard(string boardID, string boardName)
        {
            return new ClienBoard(this, boardID, boardName);
        }

        public IBoard GetBoardByURL(string url)
        {
            return null;
        }

        public IArticle GetArticleByURL(string url)
        {
            return null;
        }

        public Task<IEnumerable<IBoard>> GetBoards()
        {
            var src = new TaskCompletionSource<IEnumerable<IBoard>>();
            src.SetResult(boards);
            return src.Task;
        }
    }
}
