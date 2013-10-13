using DCView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DCView.Board;
using DCView.Misc;

namespace SandBox
{
    class Program
    {
        static void Test()
        {
            // IArticle article = Factory.GetArticleByURL("http://gall.dcinside.com/board/view/?id=windowsphone&no=33333&page=1");            
        }

        static void Main(string[] args)
        {
            SandboxAdapterFactory.Init();

            // ISite dcinside = new DCInsideSite();
            // IBoard board = dcinside.GetBoard("windowsphone", "윈도우폰");

            ISite site = new ClienSite();
            IBoard board = site.GetBoard("news", "새로운 소식");

            ILister<IArticle> articles = board.GetArticleLister(0);

            IEnumerable<IArticle> result;
            if (articles.Next(out result))
            {
                string text;
                result.First().GetText(out text);

                Console.WriteLine(text);

                StringHtmlEntityConverter conv = new DCView.Misc.StringHtmlEntityConverter();
                foreach (IHtmlEntity entity in HtmlLexer.Lex(text))
                {
                    Console.WriteLine(entity);
                }

                // Console.WriteLine(text);

                /*foreach (IArticle article in result)
                {


                    Console.WriteLine(article.Title);
                } */               
            }
        }
    }
}
