using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DCView;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.IO;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // DCViewBoard 테스트
            // 1. 로그인
            DCInsideAuth auth = new DCInsideAuth();
            auth.Login("dcviewtest", "dc6324").Wait();

                        
            // 2. 글 읽어오기
            DCInsideBoard board = new DCInsideBoard("babyface");

            /*
            CancellationTokenSource cts = new CancellationTokenSource();
            Task task = board.GetNextArticleList(cts.Token);
            
            task.Wait();*/

            /*foreach (Article article in board.Articles)
            {
                Console.WriteLine("{0}|{1}|{2}|{3}",
                    article.ID,
                    article.Title,
                    article.Name,
                    article.CommentCount);
            }*/

            CancellationTokenSource cts = new CancellationTokenSource();

            using (var pic1 = File.Open(@"Y:\1.jpg", FileMode.Open))
            using (var pic2 = File.Open(@"Y:\2.gif", FileMode.Open))
            {
                List<AttachmentStream> pics = new List<AttachmentStream>() { 
                    new AttachmentStream(){ Filename = "1.jpg", Stream = pic1, ContentType="image/jpg" },
                    new AttachmentStream() {Filename="2.gif", Stream = pic2},
                };
                board.WriteArticle("1", "2", pics, cts.Token).Wait();
                
            }
        }
    }
}
