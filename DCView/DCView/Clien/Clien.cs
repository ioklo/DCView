using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace DCView
{
    public class Clien : ISite
    {
        List<IBoard> boards = new List<IBoard>();

        public Clien()
        {
            boards.Add(new ClienBoard(this, "park", "모두의 공원"));
            boards.Add(new ClienBoard(this, "news", "새로운 소식"));
        }

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

        System.Collections.Generic.IEnumerable<IBoard> ISite.Boards
        {
            get 
            {
                return boards;
            }
        }

        bool ISite.Refresh(Action<string, int> OnStatusChanged)
        {
            // refresh 하지 않음
            return true;
        }

        void ISite.Load()
        {
            // 할 것 없음
        }

        IBoard ISite.GetBoard(string boardID, string boardName)
        {
            return new ClienBoard(this, boardID, boardName);
        }
    }
}
