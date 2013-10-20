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
    class DCInsideTest
    {
        static void GetURL()
        {
            // IArticle article = Factory.GetArticleByURL("http://gall.dcinside.com/board/view/?id=windowsphone&no=33333&page=1");            
        }


        // 텍스트 올리기 테스트
        public static void WriteArticle()
        {
            DCInsideSite site = new DCInsideSite();
            DCInsideCredential cred = new DCInsideCredential();

            // 로그인
            var result = cred.Login("dcviewtest", "1111", null, null).Result;
            if (!result) return;

            IBoard board = site.GetBoard("babyface");
            board.WriteArticle("테스트", "test", null);
        }

        public static void DeleteArticle()
        {
            DCInsideSite site = new DCInsideSite();
            DCInsideCredential cred = new DCInsideCredential();

            // 로그인
            var result = cred.Login("dcviewtest", "1111", null, null).Result;
            IBoard board = site.GetBoard("babyface");            
            board.DeleteArticle("16399");
        }

    }

    class ClienTest
    {
        static void ListArticle(string[] args)
        {
            
            // ISite dcinside = new DCInsideSite();
            // IBoard board = dcinside.GetBoard("windowsphone", "윈도우폰");

            ISite site = new ClienSite();
            IBoard board = site.GetBoard("news");
            board.Name = "모두의 공원";

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
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SandboxAdapterFactory.Init();

            DCInsideTest.DeleteArticle();
            return;
        }
    }
}
