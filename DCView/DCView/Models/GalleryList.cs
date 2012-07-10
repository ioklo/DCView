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
using MyApps.DCView;
using MyApps.Common;
using System.IO.IsolatedStorage;
using System.IO;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;

namespace MyApps.Models
{
    public class GalleryList
    {
        // favorite과 gallery의 분리
        Dictionary<string, Gallery> galleries = new Dictionary<string, Gallery>();
        
        List<Gallery> favorites = new List<Gallery>();
        bool modifiedFavorites = false;

        public IEnumerable<Gallery> All 
        {
            get
            {
                return galleries.Values;
            }
        }

        public IEnumerable<Gallery> Favorites
        {
            get
            {
                return favorites;
            }
        }

        public void Load()
        {
            // DCView_list.txt 생성 
            Util.CopyResourceToStorage("Data/idlist.txt", "/DCView_list.txt");

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
                    galleries[id] = new Gallery(id, name);
                }
            }

            if (!storage.FileExists("/DCView_favorites2.txt"))
            {
                // 이전 데이터 마이그레이션
                if (storage.FileExists("/DCView_favorites.txt"))
                {
                    using (var writer = new StreamWriter(storage.CreateFile("/DCView_favorites2.txt")))
                    using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites.txt", FileMode.Open)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Gallery gallery;
                            if (galleries.TryGetValue(line, out gallery))
                            {
                                writer.WriteLine(gallery.Name);
                                writer.WriteLine(gallery.ID);
                            }
                        }
                    }
                }
                else
                {
                    using (var stream = new StreamWriter(storage.CreateFile("/DCView_favorites2.txt")))
                    {
                        stream.WriteLine("윈도우폰");
                        stream.WriteLine("windowsphone");
                    }
                }
            }

            // favorite 리스트를 얻어온다..
            using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites2.txt", FileMode.Open)))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string name = line;
                    string id = reader.ReadLine();

                    if (id == null) break;

                    favorites.Add(new Gallery(id, name));
                }
            }
        }

        public void AddFavorite(string id, string name)
        {
            // 이미 있는지 검색한다
            foreach (var entry in favorites)
                if (entry.ID == id)
                    return;

            favorites.Add(new Gallery(id, name));
        }

        public void RemoveFavorite(Gallery gallery)
        {
            favorites.Remove(gallery);
        }

        public void SaveFavorite()
        {
            if (!modifiedFavorites) return;

            // favorite 저장
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var writer = new StreamWriter(storage.OpenFile("/DCView_favorites.txt", FileMode.Create)))
            {
                foreach (var gal in galleries.Values)
                {
                    if (gal.IsFavorite)
                        writer.WriteLine(gal.ID);
                }
            }
        }        

        public Task Search(string text, CancellationToken token, Action<Gallery> OnSearchGallery)
        {            
            return new Task( () => 
            {
                foreach (Gallery gal in galleries.Values)
                {
                    if (token.IsCancellationRequested)                        
                        token.ThrowIfCancellationRequested();

                    if (gal.ID.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                        gal.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                        OnSearchGallery(gal);
                }

                if (token.IsCancellationRequested)
                    token.ThrowIfCancellationRequested();
            });
        }

        public enum RefreshStatus
        {
            Downloading,
            Parsing,
            Saving
        }
        public delegate void RefreshStatusChangedEventHandler(RefreshStatus status, DownloadProgressChangedEventArgs data);

        public Task<bool> RefreshAll(RefreshStatusChangedEventHandler eventHandler)
        {
            WebClientEx client = new WebClientEx();

            client.Encoding = Encoding.UTF8;
            client.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";

            client.DownloadProgressChanged += (obj, args) =>
            {
                eventHandler(RefreshStatus.Downloading, args);
            };

            Task<string> downloadTask = client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute));

            return downloadTask.ContinueWith<bool>( prevTask =>
            {
                if (prevTask.IsCanceled || prevTask.Exception != null)
                {
                    return false;
                }

                eventHandler(RefreshStatus.Parsing, null);

                Regex regex = new Regex("<a href=\"http://m\\.dcinside\\.com/list\\.php\\?id=(\\w+)\">([^<]+)((</a>)|(<div class='icon_19'></div></a>))");

                var results = from match in regex.Matches(prevTask.Result).OfType<Match>()
                              let id = match.Groups[1].Value
                              let name = match.Groups[2].Value
                              let adult = (match.Groups[5].Value != String.Empty)
                              where !adult
                              select new Gallery(id, name);

                eventHandler(RefreshStatus.Saving, null);

                // 3. 변경된 내용을 파일에 저장하면서 갤러리 초기화
                galleries.Clear();

                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                using (var writer = new StreamWriter(storage.OpenFile("/DCView_list.txt", FileMode.Create)))
                {
                    foreach (Gallery gallery in results)
                    {
                        writer.WriteLine(gallery.ID);
                        writer.WriteLine(gallery.Name);
                        writer.WriteLine(0); // Adult 인지 여부인데.. 없죠

                        galleries[gallery.ID] = gallery;
                    }
                }

                return true;
            });            
        }
    }
}
