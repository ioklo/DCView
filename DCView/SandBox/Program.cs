using DCView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SandBox
{
    class Program
    {
        static void Main(string[] args)
        {
            ISite dcinside = new DCInsideSite();
            IBoard board = dcinside.GetBoard("", "");

            ILister<IArticle> articles = board.GetArticleLister(0);

            var cts = new CancellationTokenSource();
            IEnumerable<IArticle> result;
            if (articles.Next(cts.Token, out result))
            {
                var article = result.ElementAt(2);

                int a = 0;
                article.WriteComment("b", cts.Token);
            }
            

            
        }
    }
}
