﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Threading;
using System.ComponentModel;
using MyApps.Common;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows.Navigation;
using System.Text;
using System.Text.RegularExpressions;

namespace MyApps.DCView
{
    public partial class SelectGallery : PhoneApplicationPage
    {
        Dictionary<string, Gallery> galleries = new Dictionary<string, Gallery>();
        bool bModified = false;

        // 하는 일        
        // 1. 즐겨찾기로부터 갤러리 들어가기
        // 2. 전체 갤러리 리스트를 클릭해서 갤러리 들어가기
        // 3. 즐겨찾기 추가, 제거

        void LoadGalleryList()
        {
            // DCView_list.txt 생성 
            Util.CopyResourceToStorage("Data/idlist.txt", "/DCView_list.txt");

            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using( var reader = new StreamReader(storage.OpenFile("/DCView_list.txt", FileMode.Open), Encoding.UTF8))
            {
                string line = null;
                while (true)
                {
                    var gallery = new Gallery();
                    
                    if ((line = reader.ReadLine())== null) break;
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

        public SelectGallery()
        {
            InitializeComponent();
          
            // 갤러리 목록 읽어들이기..            
            LoadGalleryList();
            SearchResult.ItemsSource = new List<Gallery>(galleries.Values);
        }

        void SaveFavorite()
        {
            if (!bModified)
                return;

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
        
        // 즐겨찾기를 눌렀을 때
        private void Favorites_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Gallery gal = Favorites.SelectedItem as Gallery;
            if (gal == null) return;

            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?site={0}&id={1}&name={2}&pcsite={3}", gal.Site ,gal.ID, gal.Name, gal.PCSite), UriKind.Relative);
            NavigationService.Navigate(uri);        
        }

        // 검색결과창에서 갤러리를 눌렀을 때
        private void SearchResult_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Gallery gal = SearchResult.SelectedItem as Gallery;
            if (gal == null) return;

            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?site={0}&id={1}&name={2}&pcsite={3}", gal.Site, gal.ID, gal.Name, gal.PCSite), UriKind.Relative);
            NavigationService.Navigate(uri);
        }       
        
        
        // 나갈 때..        
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            LoginInfo.Instance.PropertyChanged -= new PropertyChangedEventHandler(Login_PropertyChanged);

            // 세팅을 저장한다
            SaveFavorite();
            LoginInfo.Instance.Save();
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        // 들어올때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 가정: 이 페이지를 새로 만들어서 들어올 곳은 Login 페이지 밖에 없다
            if (e.NavigationMode == NavigationMode.New)
            {
                // 로그인 페이지를 통해 들어왔으면 제거
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }


            // 세팅 페이지 
            LoginForm.DataContext = LoginInfo.Instance;
            LoginInfo.Instance.PropertyChanged += new PropertyChangedEventHandler(Login_PropertyChanged);
            Login_PropertyChanged(LoginInfo.Instance, new PropertyChangedEventArgs("LoginState"));

            if (!IsolatedStorageSettings.ApplicationSettings.Contains("DCView.passive_loadimg"))
                IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] = false;

            PassiveLoadImgCheckBox.IsChecked = (bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"];
            FontSizeCheckBox.IsChecked = (App.FontSize)IsolatedStorageSettings.ApplicationSettings["DCView.fontsize"] == App.FontSize.Large;
        }

        void Login_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Error")
            {
                MessageBox.Show("로그인 에러");
            }
            else if (e.PropertyName == "LoginState")
            {
                switch (LoginInfo.Instance.LoginState)
                {
                    case LoginInfo.State.NotLogin:
                        SubmitButton.Content = "로그인";
                        SubmitButton.IsEnabled = true;                        
                        ProgressBar.IsIndeterminate = false;
                        break;

                    case LoginInfo.State.LoggingIn:
                        SubmitButton.Content = "로그인 중";
                        SubmitButton.IsEnabled = false;                        
                        ProgressBar.IsIndeterminate = true;
                        break;

                    case LoginInfo.State.LoggedIn:
                        SubmitButton.Content = "로그아웃";
                        SubmitButton.IsEnabled = true;                        
                        ProgressBar.IsIndeterminate = false;                        
                        break;
                }
            }            
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            switch (LoginInfo.Instance.LoginState)
            {
                case LoginInfo.State.NotLogin:
                    LoginInfo.Instance.Login();
                    break;

                case LoginInfo.State.LoggingIn:
                    LoginInfo.Instance.Cancel();
                    break;

                case LoginInfo.State.LoggedIn:
                    LoginInfo.Instance.Logout();
                    break;
            }
        }
        
        // 찾기         
        DispatcherTimer dt;
        BackgroundWorker searchWorker = null;
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dt == null)
            {
                dt = new DispatcherTimer();
                dt.Interval = new TimeSpan(0, 0, 1);

                // 1초 있다가 
                dt.Tick += (o, ea) =>
                {
                    // 더이상 수행은 멈추고 
                    dt.Stop();

                    searchWorker = new BackgroundWorker();
                    searchWorker.WorkerSupportsCancellation = true;
                    searchWorker.DoWork += new DoWorkEventHandler(searchWorker_DoWork);
                    searchWorker.RunWorkerAsync(String.Copy(SearchBox.Text));                    
                };
            }

            // 현재 돌아가고 있는 searchWorker는 중지한다. 
            if (searchWorker != null)
            {
                searchWorker.CancelAsync();
                searchWorker = null;                
            }

            dt.Stop();
            dt.Start();
        }

        void searchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            // 캔슬당했으면 그만 둬야함
            if (bw.CancellationPending) return;

            List<Gallery> result = new List<Gallery>();
            string text = e.Argument as string;

            foreach (Gallery gal in galleries.Values)
            {
                if (bw.CancellationPending)
                    break;

                if (gal.ID.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 || 
                    gal.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    result.Add(gal);
            }

            Dispatcher.BeginInvoke(() =>
            {
                SearchResult.ItemsSource = result;
            });
        }

        // 전체리스트에서 체크박스를 눌렀을때는 
        private void CheckBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
        }

        private void RemoveFavorite(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            var gal = item.Tag as Gallery;
            gal.IsFavorite = false;
        }

        private void RemoveLoginInfo(object sender, RoutedEventArgs e)
        {
            LoginInfo.Instance.Delete();
        }

        private void PassiveLoadImage(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] = PassiveLoadImgCheckBox.IsChecked;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/Login.xaml?action=back", UriKind.Relative));
        }

        private void RefreshGalleryListButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Visibility = Visibility.Collapsed;
            SearchResult.Visibility = Visibility.Collapsed;
            RefreshGalleryListButton.Visibility = Visibility.Collapsed;
            RefreshPanel.Visibility = Visibility.Visible;

            // 태스크 정의
            // 1. 읽어오고..
            BackgroundWorker bw = new BackgroundWorker();            
            WebClientEx client = new WebClientEx();

            client.Encoding = Encoding.UTF8;
            client.Headers["User-Agent"] = "Mozilla/5.0 (Linux; U; Android 2.1-update1; ko-kr; Nexus One Build/ERE27) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
            
            client.DownloadProgressChanged += (o1, e1) =>
                {
                    RefreshStatus.Text = string.Format("{0}/{1} 다운로드 중입니다", e1.BytesReceived, e1.TotalBytesToReceive);
                    RefreshProgress.Value = e1.ProgressPercentage * 0.8;
                };

            client.DownloadStringCompleted += (o1, e1) =>
                {
                    if( e1.Cancelled || e1.Error != null )
                    {
                        MessageBox.Show("갤러리 목록을 얻어내는데 실패했습니다. 잠시 후 다시 실행해보세요");
                        return;
                    }                    
                    
                    ThreadPool.QueueUserWorkItem( (state) =>
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

                                    Dispatcher.BeginInvoke( () =>
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

            client.DownloadStringAsync(new Uri("http://m.dcinside.com/category_gall_total.html", UriKind.Absolute));

        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchResult.Focus();
            }
        }

        private void FontSizeCheckBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!FontSizeCheckBox.IsChecked.HasValue) return;

            IsolatedStorageSettings.ApplicationSettings["DCView.fontsize"] = FontSizeCheckBox.IsChecked.Value ? App.FontSize.Large : App.FontSize.Normal;
            ((App)Application.Current).InitializeFontResource();
        }
    }
}