using System;
using System.Collections.Generic;

namespace DCView.Board
{
    public class DCInsideLister : ILister<IArticle>
    {
        DCInsideBoard board;
        int page;
        bool viewRecommend;
        int lastArticleID = int.MaxValue;

        public DCInsideLister(DCInsideBoard board, int page, bool viewRecommend)
        {
            this.board = board;
            this.page = page;
            this.viewRecommend = viewRecommend;
        }

        public bool Next(out IEnumerable<IArticle> result)
        {
            result = null;
            IEnumerable<DCInsideArticle> articles;
            if (!board.GetArticleList(page, viewRecommend, out articles))
                throw new Exception();

            // 성공했으면 다음에 읽을 page를 하나 올려준다
            page++;

            var resultList = new List<IArticle>();
            int minID = int.MaxValue;
            foreach (var article in articles)
            {
                int id = int.Parse(article.ID);
                if (id < minID) minID = id;

                if (id < lastArticleID)
                {
                    resultList.Add(article);
                    lastArticleID = minID;
                }
            }

            result = resultList;
            return true;
        }
    }

}