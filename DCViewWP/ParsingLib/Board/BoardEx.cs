using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DCView.Board
{
    public static class BoardEx
    {
        public static bool Check(IBoard board, string method)
        {
            return board.GetType().GetMethod(method).GetCustomAttributes(typeof(NotSupportedAttribute), false).Length == 0;
        }

        public static bool CanWriteArticle(this IBoard board)
        {
            return Check(board, "WriteArticle");
        }

        public static bool CanSearch(this IBoard board)
        {
            return Check(board, "GetSearchLister");
        }

        public static bool CanDeleteArticle(this IBoard board)
        {
            return Check(board, "DeleteArticle");
        }
    }
}
