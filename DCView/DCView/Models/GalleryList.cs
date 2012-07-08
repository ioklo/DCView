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

namespace MyApps.Models
{
    public class GalleryList
    {
        Dictionary<string, Gallery> galleries = new Dictionary<string, Gallery>();
        bool modified = false;


        public IList<Gallery> All { get; }
        public IList<Gallery> Favorites { get; }

        
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

        public Task<bool> RefreshAll(DownloadProgressChangedEventHandler progressEventHandler)
        {
            WebClientEx client = new WebClientEx();

            client.Encoding = Encoding.UTF8;
            client.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";

            client.DownloadProgressChanged += progressEventHandler;
            client.DownloadStringAsyncTask(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute))
                .ContinueWith<string>( prevTask =>
                {

                    if (prevTask.IsCanceled || e1.Error != null)
                    {
                        MessageBox.Show("갤러리 목록을 얻어내는데 실패했습니다. 잠시 후 다시 실행해보세요");
                        return;
                    }

                ThreadPool.QueueUserWorkItem((state) =>
                {
                    // 2. 파싱해서
                    Dispatcher.BeginInvoke(() =>
                    {
                        RefreshStatus.Text = "결과 분석중입니다";
                        RefreshProgress.Value = 80;
                    });

                    Regex regex = new Regex("<a href=\"http://m\\.dcinside\\.com/list\\.php\\?id=(\\w+)\">([^<]+)((</a>)|(<div class='icon_19'></div></a>))");

                    var results = from match in regex.Matches(e1.Result).OfType<Match>()
                                  select new Gallery()
                                  {
                                      ID = match.Groups[1].Value,
                                      Name = match.Groups[2].Value,
                                      Adult = (match.Groups[5].Value != String.Empty)
                                  };

                    Dispatcher.BeginInvoke(() =>
                    {
                        RefreshStatus.Text = "설정 파일에 저장합니다";
                        RefreshProgress.Value = 90;
                    });

                    // 3. 파일에 저장
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
