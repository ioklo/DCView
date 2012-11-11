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
using System.IO.IsolatedStorage;
using System.IO;
using System.Text;
using DCView.Util;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MyApps.Common;
using System.Linq;

namespace DCView
{
    public class DCInsideSite : ISite
    {
        List<IBoard> boards = new List<IBoard>();

        public void Load()
        {
            // DCView_list.txt 생성 
            MyApps.Common.Util.CopyResourceToStorage("Data/idlist.txt", "/DCView_list.txt");

            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var reader = new StreamReader(storage.OpenFile("/DCView_list.txt", FileMode.Open), Encoding.UTF8))
            {
                string line = null;
                while (true)
                {
                    string id, name;
                    bool adult;

                    if ((line = reader.ReadLine()) == null) break;
                    id = line;

                    if ((line = reader.ReadLine()) == null) break;
                    name = line;

                    if ((line = reader.ReadLine()) == null) break;
                    adult = line == "1";

                    // 성인갤은 집어넣질 않는다
                    if (adult) continue;
                    boards.Add(new DCInsideBoard(this, id, name));
                }
            }
        }

        string ISite.ID
        {
            get { return "dcinside";  }
        }

        string ISite.Name
        {
            get { return "디시인사이드"; }
        }

        bool ISite.CanLogin { get { return true; } }

        IEnumerable<IBoard> ISite.Boards
        {
            get { return boards; }
        }

        bool ISite.Refresh(Action<string, int> OnStatusChanged)
        {
            try
            {
                DCViewWebClient client = new DCViewWebClient();

                client.DownloadProgressChanged += (obj, args) =>
                {
                    OnStatusChanged(
                        string.Format("다운로드 중... {0}/{1} ", args.BytesReceived, args.TotalBytesToReceive),
                        (int)(args.ProgressPercentage * 0.8));
                };

                CancellationTokenSource cts = new CancellationTokenSource();
                var result = client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute), cts.Token).GetResult();

                OnStatusChanged("결과 분석중입니다", 80);

                Regex regex = new Regex("<a href=\"http://m\\.dcinside\\.com/list\\.php\\?id=(\\w+)\">([^<]+)((</a>)|(<div class='icon_19'></div></a>))");

                var results = from match in regex.Matches(result).OfType<Match>()
                              let id = match.Groups[1].Value
                              let name = match.Groups[2].Value
                              let adult = (match.Groups[5].Value != String.Empty)
                              where !adult
                              select new DCInsideBoard(this, id, name);

                OnStatusChanged("리스트를 저장합니다", 90);

                // 3. 변경된 내용을 파일에 저장하면서 갤러리 초기화
                boards.Clear();

                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                using (var writer = new StreamWriter(storage.OpenFile("/DCView_list.txt", FileMode.Create)))
                {
                    foreach (DCInsideBoard board in results)
                    {
                        writer.WriteLine(board.ID);
                        writer.WriteLine(board.Name);
                        writer.WriteLine(0); // Adult 인지 여부인데.. 없죠

                        boards.Add(board);
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        
        IBoard ISite.GetBoard(string boardID, string boardName)
        {
            return new DCInsideBoard(this, boardID, boardName);
        }
    }
}
