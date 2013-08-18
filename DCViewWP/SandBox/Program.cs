using DCView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DCView.Board;

namespace SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            SandboxAdapterFactory.Init();

            ISite dcinside = new DCInsideSite();
            IBoard board = dcinside.GetBoard("windowsphone", "윈도우폰");

            ILister<IArticle> articles = board.GetArticleLister(0);

            IEnumerable<IArticle> result;
            if (articles.Next(out result))
            {
                foreach (IArticle article in result)
                {
                    Console.WriteLine(article.Title);
                }
                // var article = result.ElementAt(2);                
                // article.WriteComment("b", cts.Token);
            }
        }
    }
}
