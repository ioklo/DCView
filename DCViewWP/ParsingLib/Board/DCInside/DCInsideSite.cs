﻿using System;
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
using System.Threading.Tasks;

namespace DCView.Board
{
    public class DCInsideSite : ISite
    {
        List<IBoard> boards;
        DCInsideCredential credential = new DCInsideCredential();
        
        private List<IBoard> LoadBoards()
        {
            List<IBoard> result = new List<IBoard>();

            // dc_list.txt 생성
            AdapterFactory.Instance.CopyResourceToStorage("Data/dc_list.txt", "/dc_list.txt");

            using (var stream = AdapterFactory.Instance.OpenReadStorageFile("/dc_list.txt"))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                
                // 첫줄은 버전 용도.. (일단 쓰지 말자)
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
                    result.Add(new DCInsideBoard(this, id) { Name = name });
                }
            }

            return result;
        }

        public ICredential Credential
        {
            get { return credential; }
        }
        public string ID
        {
            get { return "dcinside";  }
        }
        public string Name { get { return "디시인사이드"; } }
        public bool CanLogin { get { return true; } }
        public bool Refresh(Action<string, int> OnStatusChanged)
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
                var result = client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute)).Result;

                OnStatusChanged("결과 분석중입니다", 80);

                Regex regex = new Regex("<a href=\"http://m\\.dcinside\\.com/list\\.php\\?id=(\\w+)\">([^<]+)((</a>)|(<div class='icon_19'></div></a>))");

                var results = from match in regex.Matches(result).OfType<Match>()
                              let id = match.Groups[1].Value
                              let name = match.Groups[2].Value
                              let adult = (match.Groups[5].Value != String.Empty)
                              where !adult
                              select new DCInsideBoard(this, id) { Name = name };

                OnStatusChanged("리스트를 저장합니다", 90);

                // 3. 변경된 내용을 파일에 저장하면서 갤러리 초기화
                boards.Clear();

                using (var stream = AdapterFactory.Instance.OpenWriteStorageFile("/dc_list.txt"))
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

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

        public IBoard GetBoard(string boardID)
        {
            return new DCInsideBoard(this, boardID);
        }

        public IBoard GetBoardByURL(string url)
        {
            return null;
        }

        public IArticle GetArticleByURL(string url)
        {
            return null;
        }

        // 
        public async Task<IEnumerable<IBoard>> GetBoards()
        {
            if (boards == null)
            {
                boards = await Task<List<IBoard>>.Factory.StartNew(LoadBoards);
            }

            return boards;
        }
    }
}
