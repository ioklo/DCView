using System;
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
using System.Threading.Tasks;
using Microsoft.Phone.Shell;

namespace DCView
{
    public partial class MainPanorama : PhoneApplicationPage
    {   
        public MainPanorama()
        {
            InitializeComponent();
            InitializeApplicationBar();

            ApplicationBar = favoriteApplicationBar;
            
            SearchResult.ItemsSource = App.Current.GalleryList.All;
            Favorites.ItemsSource = App.Current.GalleryList.Favorites;
        }

        // 나갈 때..        
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // 세팅을 저장한다
            App.Current.GalleryList.SaveFavorite();
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        // 페이지에 들어올때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            // 설정 페이지
            if (!IsolatedStorageSettings.ApplicationSettings.Contains("DCView.passive_loadimg"))
                IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] = false;

            PassiveLoadImgCheckBox.IsChecked = (bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"];
            FontSizeCheckBox.IsChecked = (App.FontSize)IsolatedStorageSettings.ApplicationSettings["DCView.fontsize"] == App.FontSize.Large;
        }

        // application bar 설정
        ApplicationBar favoriteApplicationBar, allApplicationBar, settingApplicationBar;

        void InitializeApplicationBar()
        {
            favoriteApplicationBar = new ApplicationBar();

            allApplicationBar = new ApplicationBar();
            ApplicationBarIconButton refreshListIconButton = new ApplicationBarIconButton();
            refreshListIconButton.IconUri = new Uri("/appbar.refresh.rest.png", UriKind.Relative);
            refreshListIconButton.Click += RefreshGalleryListButton_Click;
            refreshListIconButton.Text = "새로고침";
            allApplicationBar.Buttons.Add(refreshListIconButton);

            settingApplicationBar = new ApplicationBar();
        }

        private void PanoramaMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanoramaMain.SelectedItem == PanoramaFavorite)
            {
                ApplicationBar = favoriteApplicationBar;
            }
            else if (PanoramaMain.SelectedItem == PanoramaAll)
            {
                ApplicationBar = allApplicationBar;
            }
            else if (PanoramaMain.SelectedItem == PanoramaSetting)
            {
                ApplicationBar = settingApplicationBar;
            }

        }


        // Panorama 1. 즐겨찾기에 관련된 
        
        // 즐겨찾기를 눌렀을 때
        private void Favorites_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Gallery gal = ((FrameworkElement)sender).Tag as Gallery;
            if (gal == null) return;

            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?id={0}", gal.ID), UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        // 삭제 명령
        private void RemoveFavorite(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            var gal = item.Tag as Gallery;

            App.Current.GalleryList.RemoveFavorite(gal);            
        }

        // Panorama 2. 전체 갤러리 창

        // 검색결과창에서 갤러리를 눌렀을 때
        private void SearchResult_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Gallery gal = ((FrameworkElement)sender).Tag as Gallery;
            if (gal == null) return;

            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?id={0}", gal.ID), UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        // 즐겨찾기에 추가
        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            Gallery gal = ((FrameworkElement)sender).Tag as Gallery;
            if (gal == null) return;

            App.Current.GalleryList.AddFavorite(gal.ID, gal.Name);
        }       

        // 엔터를 눌렀을 때 결과창만 보여주기
        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchResult.Focus();
            }
        }
        
        // 찾기         
        DispatcherTimer dt = null; // 1초 기다리는 타이머
        CancellationTokenSource cancelTokenSource = null;
        ObservableCollection<Gallery> searchResult = null;
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 1초 기다리는 타이머가 없다면 만든다.
            if (dt == null)
            {
                dt = new DispatcherTimer();
                dt.Interval = new TimeSpan(0, 0, 0, 1, 500);

                // 1초 있다가 할일을 정의해주고
                dt.Tick += (o, ea) =>
                {
                    // 더이상 수행은 멈추고 
                    dt.Stop();

                    if (SearchBox.Text.Length == 0)
                    {
                        SearchResult.ItemsSource = App.Current.GalleryList.All;
                        return;
                    }

                    // 새로운 cancellation
                    cancelTokenSource = new CancellationTokenSource();
                    searchResult = new ObservableCollection<Gallery>();
                    SearchResult.ItemsSource = searchResult;

                    App.Current.GalleryList.Search(SearchBox.Text, cancelTokenSource.Token, gallery =>
                    {
                        Dispatcher.BeginInvoke(() => { searchResult.Add(gallery); });
                    }).Start();
                };
            }

            // 1초를 기다리는 중이었고
            // 취소 토큰이 있다면
            if (cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
                cancelTokenSource = null;
            }

            // 다시 시작            
            dt.Stop();
            dt.Start();            
        }
        
        void OnRefreshStatusChangedEventHandler(GalleryList.RefreshStatus status, DownloadProgressChangedEventArgs downloadArgs)
        {
            switch(status)
            {
                case GalleryList.RefreshStatus.Downloading:
                    Dispatcher.BeginInvoke(() => 
                    {
                        RefreshStatus.Text = string.Format("다운로드 중... {0}/{1} ", downloadArgs.BytesReceived, downloadArgs.TotalBytesToReceive);
                        RefreshProgress.Value = downloadArgs.ProgressPercentage * 0.8;
                    });
                    break;

                case GalleryList.RefreshStatus.Parsing:
                    Dispatcher.BeginInvoke(() => 
                    {
                        RefreshStatus.Text = "결과 분석중입니다";
                        RefreshProgress.Value = 80;
                    });
                    break;

                case GalleryList.RefreshStatus.Saving:
                    Dispatcher.BeginInvoke(() =>
                    {
                        RefreshStatus.Text = "리스트를 저장합니다";
                        RefreshProgress.Value = 90;
                    });
                    break;
            }
        }

        private void RefreshGalleryListButton_Click(object sender, EventArgs e)
        {
            SearchBox.Visibility = Visibility.Collapsed;
            SearchResult.Visibility = Visibility.Collapsed;
            RefreshPanel.Visibility = Visibility.Visible;

            var refreshTask = App.Current.GalleryList.RefreshAll(OnRefreshStatusChangedEventHandler);

            refreshTask.ContinueWith(prevTask =>
            {
                if (!prevTask.Result)
                {
                    MessageBox.Show("갤러리 목록을 얻어내는데 실패했습니다. 잠시 후 다시 실행해보세요");
                }
                
                Dispatcher.BeginInvoke(() =>
                {
                    SearchResult.ItemsSource = App.Current.GalleryList.All;

                    SearchBox.Text = "";
                    SearchBox.Visibility = Visibility.Visible;
                    SearchResult.Visibility = Visibility.Visible;                    
                    RefreshPanel.Visibility = Visibility.Collapsed;
                });

            });
        }

        // 3. 설정창.

        private void FontSizeCheckBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!FontSizeCheckBox.IsChecked.HasValue) return;

            IsolatedStorageSettings.ApplicationSettings["DCView.fontsize"] = FontSizeCheckBox.IsChecked.Value ? App.FontSize.Large : App.FontSize.Normal;
            ((App)Application.Current).InitializeFontResource();
        }

        private void PassiveLoadImage(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] = PassiveLoadImgCheckBox.IsChecked;
        }

        

    }
}