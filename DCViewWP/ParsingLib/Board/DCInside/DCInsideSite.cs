using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Linq;
using DCView.Adapter;
using DCView.Misc;

namespace DCView.Board
{
    public class DCInsideSite : ISite
    {
        List<IBoard> boards = new List<IBoard>();
        DCInsideCredential credential = new DCInsideCredential();
        
        public void Load()
        {
            // DCView_list.txt 생성
            AdapterFactory.Instance.CopyResourceToStorage("Data/idlist.txt", "/DCView_list.txt");

            using (var stream = AdapterFactory.Instance.OpenReadStorageFile("/DCView_list.txt"))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
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

        ICredential ISite.Credential
        {
            get { return credential; }
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
                var client = AdapterFactory.Instance.CreateWebClient();

                client.DownloadProgressChanged += (obj, BytesReceived, TotalBytesToReceive, ProgressPercentage) =>
                {
                    OnStatusChanged(
                        string.Format("다운로드 중... {0}/{1} ", BytesReceived, TotalBytesToReceive),
                        (int)(ProgressPercentage * 0.8));
                };

                CancellationTokenSource cts = new CancellationTokenSource();
                var result = client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute)).GetResult();

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
                
                using (var stream = AdapterFactory.Instance.OpenWriteStorageFile("/DCView_list.txt"))
                using (var writer = new StreamWriter(stream))
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
