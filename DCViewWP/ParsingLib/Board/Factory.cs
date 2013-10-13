using System.Collections.Generic;
using System.Threading.Tasks;
namespace DCView.Board
{
    // 사이트 관리용 팩토리 
    public static class Factory
    {
        static List<ISite> sites;

        static Factory()
        {
            sites = new List<ISite>() { new DCInsideSite(), new ClienSite() };
        }

        static public IEnumerable<ISite> Sites { get { return sites; } }

        static public ISite GetSite(string siteID)
        {
            foreach (var site in Factory.Sites)
            {
                if (site.ID == siteID)
                    return site;
            }

            return null;
        }

        static public IBoard GetBoard(string siteID, string boardID, string boardName)
        {
            foreach (var site in sites)
            {
                if (site.ID == siteID)
                    return site.GetBoard(boardID, boardName);
            }

            // board가 없으면 리턴
            return null;
        }

        static IBoard GetBoardByURL(string url)
        {
            foreach (var site in sites)
            {
                IBoard board = site.GetBoardByURL(url);
                if (board != null)
                    return board;
            }

            return null;
        }

        static IArticle GetArticleByURL(string url)
        {
            foreach(var site in sites)
            {
                IArticle article = site.GetArticleByURL(url);
                if (article != null) return article;
            }

            return null;
        }
    }
}