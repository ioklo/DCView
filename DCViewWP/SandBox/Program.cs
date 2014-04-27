using DCView;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DCView.Board;
using DCView.Misc;
using System.Net;
using System.Collections;
using System.Reflection;
using DCView.Adapter;

namespace SandBox
{
    class DCInsideTest
    {
        public static CookieCollection GetAllCookies(CookieContainer cookieJar)
        {
            CookieCollection cookieCollection = new CookieCollection();

            Hashtable table = (Hashtable)cookieJar.GetType().InvokeMember("m_domainTable",
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.GetField |
                                                                            BindingFlags.Instance,
                                                                            null,
                                                                            cookieJar,
                                                                            new object[] { });

            foreach (var tableKey in table.Keys)
            {
                String str_tableKey = (string)tableKey;

                if (str_tableKey[0] == '.')
                {
                    str_tableKey = str_tableKey.Substring(1);
                }

                SortedList list = (SortedList)table[tableKey].GetType().InvokeMember("m_list",
                                                                            BindingFlags.NonPublic |
                                                                            BindingFlags.GetField |
                                                                            BindingFlags.Instance,
                                                                            null,
                                                                            table[tableKey],
                                                                            new object[] { });

                foreach (var listKey in list.Keys)
                {
                    String url = "https://" + str_tableKey + (string)listKey;
                    cookieCollection.Add(cookieJar.GetCookies(new Uri(url)));
                }
            }

            return cookieCollection;
        }

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

            IBoard board = site.GetBoard("parkjunggum");
            board.WriteArticle("테스트", "test", null);

            var collection = GetAllCookies(AdapterFactory.Instance.CookieContainer);
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

        public static void ListArticle()
        {
            ISite dcinside = new DCInsideSite();
            IBoard board = dcinside.GetBoard("windowsphone");

            ILister<IArticle> articleLister = board.GetArticleLister(0);
            IEnumerable<IArticle> articles;

            if (articleLister.Next(out articles))
            {
                foreach (var article in articles)
                    Console.WriteLine("{0} {1}", article.ID, article.Title);
            }
        }

    }

    class ClienTest
    {
        public static void ListArticle()
        {
            
            // ISite dcinside = new DCInsideSite();
            // IBoard board = dcinside.GetBoard("windowsphone", "윈도우폰");

            ISite site = new ClienSite();
            IBoard board = site.GetBoard("news");
            board.Name = "모두의 공원";

            //ILister<IArticle> articles = board.GetArticleLister(0);
            ILister<IArticle> articles = board.GetSearchLister("검색", SearchType.Subject); // .GetArticleLister(0);

            IEnumerable<IArticle> result;
            if (articles.Next(out result))
            {
                foreach (var article in result)
                {
                    Console.WriteLine(article.Title);
                }


                /*string text;
                result.First().GetText(out text);

                Console.WriteLine(text);

                StringHtmlEntityConverter conv = new DCView.Misc.StringHtmlEntityConverter();
                foreach (IHtmlEntity entity in HtmlLexer.Lex(text))
                {
                    Console.WriteLine(entity);
                }*/
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SandboxAdapterFactory.Init();

            // DCInsideTest.DeleteArticle();
            // DCInsideTest.ListArticle();

            DCInsideTest.WriteArticle();

            return;
        }
    }
}
