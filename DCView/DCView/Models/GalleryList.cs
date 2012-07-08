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

namespace MyApps.Models
{
    public class GalleryList
    {
        Dictionary<string, Gallery> galleries = new Dictionary<string, Gallery>();
        bool modified = false;

        public IEnumerable<Gallery> All 
        {
            get
            {
                return galleries.Values;
            }
        }

        public IEnumerable<Gallery> Favorites;

        
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
                    var gallery = new Gallery();

                    if ((line = reader.ReadLine()) == null) break;
                    gallery.ID = line;

                    if ((line = reader.ReadLine()) == null) break;
                    gallery.Name = line;

                    if ((line = reader.ReadLine()) == null) break;
                    gallery.Adult = line == "1";

                    gallery.IsFavorite = false;

                    gallery.PropertyChanged += (o, e) =>
                    {
                        bModified = true;
                        if (gallery.IsFavorite)
                            Favorites.Items.Add(gallery);
                        else
                            Favorites.Items.Remove(gallery);
                    };

                    galleries[gallery.ID] = gallery;
                }
            }


            if (!storage.FileExists("/DCView_favorites.txt"))
            {
                using (var stream = new StreamWriter(storage.CreateFile("/DCView_favorites.txt")))
                {
                    stream.WriteLine("windowsphone");
                }
            }

            // favorite 리스트를 얻어온다..
            using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites.txt", FileMode.Open)))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    Gallery gal;
                    if (!galleries.TryGetValue(line, out gal)) continue;

                    gal.IsFavorite = true;
                }
            }
        }

        public void SaveFavorite()
        {
            if (!modified) return;

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

        enum RefreshStatus
        {
            Downloading,
            Parsing,
            Saving
        }
        
        public delegate void RefreshStatusChangedEventHandler(RefreshStatus status, int data);

        public Task<bool> RefreshAll(RefreshStatusChangedEventHandler eventHandler)
        {
            WebClientEx client = new WebClientEx();

            client.Encoding = Encoding.UTF8;
            client.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";

            client.DownloadProgressChanged += (obj, args) =>
            {
                eventHandler(RefreshStatus.Downloading, args.BytesReceived * 100 / args.TotalBytesToReceive);                                        
            };

            client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute))
                .ContinueWith<bool>( prevTask =>
                {
                    if (prevTask.IsCanceled || prevTask.Exception != null)
                    {
                        return false;
                    }

                    eventHandler(RefreshStatus.Parsing, 0);                
                    
                    
                    Regex regex = new Regex("<a href=\"http://m\\.dcinside\\.com/list\\.php\\?id=(\\w+)\">([^<]+)((</a>)|(<div class='icon_19'></div></a>))");

                    var results = from match in regex.Matches(e1.Result).OfType<Match>()
                                  select new Gallery()
                                  {
                                      ID = match.Groups[1].Value,
                                      Name = match.Groups[2].Value,
                                      Adult = (match.Groups[5].Value != String.Empty)
                                  };

                    eventHandler(RefreshStatus.Saving, 0);
                    
                    // 3. 변경된 내용을 파일에 저장
                    galleries.Clear();
                    var storage = IsolatedStorageFile.GetUserStoreForApplication();
                    using (var writer = new StreamWriter(storage.OpenFile("/DCView_list.txt", FileMode.Create)))
                    {
                        foreach (var entry in results)
                        {
                            Gallery gallery = entry;
                            if (gallery.Adult) continue;

                            writer.WriteLine(entry.ID);
                            writer.WriteLine(entry.Name);
                            writer.WriteLine(entry.Adult ? 1 : 0);
                            gallery.IsFavorite = false;

                            gallery.PropertyChanged += (o2, e2) =>
                            {
                                bModified = true;
                                if (gallery.IsFavorite)
                                    Favorites.Items.Add(gallery);
                                else
                                    Favorites.Items.Remove(gallery);
                            };

                            galleries[gallery.ID] = gallery;
                        }
                    }

                    Dispatcher.BeginInvoke(() => { Favorites.Items.Clear(); });

                    // 4. favorite 다시 읽어오기 ..
                    using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites.txt", FileMode.Open)))
                    {
                        string line = null;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Gallery gal;
                            if (!galleries.TryGetValue(line, out gal)) continue;

                            Dispatcher.BeginInvoke(() =>
                            { gal.IsFavorite = true; }
                            );
                        }
                    }

                    // 5. 
                    Dispatcher.BeginInvoke(() =>
                    {
                        RefreshStatus.Text = "리스트 갱신";
                        RefreshProgress.Value = 95;

                        SearchResult.ItemsSource = new List<Gallery>(galleries.Values);

                        SearchBox.Text = "";
                        SearchBox.Visibility = Visibility.Visible;
                        SearchResult.Visibility = Visibility.Visible;
                        RefreshGalleryListButton.Visibility = Visibility.Visible;
                        RefreshPanel.Visibility = Visibility.Collapsed;
                    });


                });
            };


            
        }
    }
}
