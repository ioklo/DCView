

using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net;
using System;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using System.Linq;
using DCView.Board;
using DCView.Misc;
using System.Reflection;
using DCView.Adapter;
using System.IO;
using System.Collections.Generic;

namespace DCView
{
    public partial class MainPanorama : PhoneApplicationPage
    {
        // application bar 설정
        ApplicationBar favoriteApplicationBar, allApplicationBar, settingApplicationBar;  

        // 찾기         
        DispatcherTimer dt = null; // 1초 기다리는 타이머
        CancellationTokenSource cancelTokenSource = null;
        ObservableCollection<IBoard> searchResult = null;

        public class SearchSiteItem
        {
            public string Name { get { return Site.Name; } }
            public ISite Site { get; set; }
        }
        

        // WP7 compatibility
        private static string GetVersionNumber()
        {
            var asm = Assembly.GetExecutingAssembly();
            var parts = asm.FullName.Split(',');
            return parts[1].Split('=')[1];
        }
        
        public MainPanorama()
        {
            InitializeComponent();
            VersionText.Text = GetVersionNumber();
            InitializeApplicationBar();

            ApplicationBar = favoriteApplicationBar;
            Favorites.ItemsSource = App.Current.Favorites.All;
        }

        // 내부 함수
        private void InitializeApplicationBar()
        {
            favoriteApplicationBar = new ApplicationBar();

            allApplicationBar = new ApplicationBar();
            ApplicationBarIconButton refreshListIconButton = new ApplicationBarIconButton();
            refreshListIconButton.IconUri = new Uri("/Data/appbar.refresh.rest.png", UriKind.Relative);
            refreshListIconButton.Click += RefreshGalleryListButton_Click;
            refreshListIconButton.Text = "새로고침";
            allApplicationBar.Buttons.Add(refreshListIconButton);

            settingApplicationBar = new ApplicationBar();
        }

        // 나갈 때..        
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // 세팅을 저장한다
            App.Current.Favorites.Save();
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

        
        private void PanoramaMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanoramaMain.SelectedItem == PanoramaFavorite)
            {
                ApplicationBar = favoriteApplicationBar;
            }
            else if (PanoramaMain.SelectedItem == PanoramaAll)
            {
                ApplicationBar = allApplicationBar;

                if (SearchSite.Items.Count == 0)
                {
                    foreach (var site in Factory.Sites)
                        SearchSite.Items.Add(new SearchSiteItem() { Site = site });
                }
            }
            else if (PanoramaMain.SelectedItem == PanoramaSetting)
            {
                ApplicationBar = settingApplicationBar;
            }
        }


        // Panorama 1. 즐겨찾기에 관련된 
        void NavigateViewArticle(string siteID, string boardID, string boardName)
        {
            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?siteID={0}&boardID={1}&boardName={2}", siteID, boardID, boardName), UriKind.Relative);
            NavigationService.Navigate(uri);
        }
        
        // 즐겨찾기를 눌렀을 때
        private void Favorites_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Favorites.Entry entry = ((FrameworkElement)sender).Tag as Favorites.Entry;
            if (entry == null) return;

            NavigateViewArticle(entry.SiteID, entry.BoardID, entry.DisplayName);
        }

        // 삭제 명령
        private void RemoveFavorite(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            var entry = item.Tag as Favorites.Entry;

            App.Current.Favorites.Remove(entry);
        }

        // Panorama 2. 전체 갤러리 창

        // 검색결과창에서 갤러리를 눌렀을 때
        private void SearchResult_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            IBoard board = ((FrameworkElement)sender).Tag as IBoard;
            if (board == null) return;

            NavigateViewArticle(board.Site.ID, board.ID, board.Name);
        }

        // 즐겨찾기에 추가
        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            IBoard board = ((FrameworkElement)sender).Tag as IBoard;
            if (board == null) return;

            App.Current.Favorites.Add(board.Site.ID, board.ID, board.Name);
        }       

        // 엔터를 눌렀을 때 결과창만 보여주기
        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchResult.Focus();
            }
        }
        
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
                    SearchSiteItem siteItem = SearchSite.SelectedItem as SearchSiteItem;
                    if (siteItem == null) return;

                    // 더이상 수행은 멈추고 
                    dt.Stop();

                    if (SearchBox.Text.Length == 0)
                    {
                        SearchResult.ItemsSource = siteItem.Site.GetBoards().Result;
                        return;
                    }

                    // 새로운 cancellation
                    cancelTokenSource = new CancellationTokenSource();
                    searchResult = new ObservableCollection<IBoard>();
                    SearchResult.ItemsSource = searchResult;

                    string text = SearchBox.Text;

                    Task.Factory.StartNew( () =>
                    {
                        App.Current.SiteManager.Search(text, cancelTokenSource.Token, siteItem.Site, board =>
                        {
                            Dispatcher.BeginInvoke(() => { searchResult.Add(board); });
                        });
                    });
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
        
        void OnRefreshStatusChangedEventHandler(string msg, int percent)
        {
            Dispatcher.BeginInvoke( () => 
            {
                RefreshStatus.Text = msg;
                RefreshProgress.Value = percent;
            });
        }

        // 즐겨찾기 갱신
        private async void RefreshGalleryListButton_Click(object sender, EventArgs e)
        {
            // 현재 선택된 사이트에 대해서 갱신
            SearchSiteItem siteItem = SearchSite.SelectedItem as SearchSiteItem;
            if (siteItem == null) return;
            ISite site = siteItem.Site;

            SearchBox.Visibility = Visibility.Collapsed;
            SearchResult.Visibility = Visibility.Collapsed;
            RefreshPanel.Visibility = Visibility.Visible;

            // dcinside 의존성을 여기에 씀
            bool result = await Task.Factory.StartNew( () => { return site.Refresh(OnRefreshStatusChangedEventHandler); });

            if (!result)
                MessageBox.Show("갤러리 목록을 얻어내는데 실패했습니다. 잠시 후 다시 실행해보세요");

            SearchResult.ItemsSource = await site.GetBoards();

            SearchBox.Text = "";
            SearchBox.Visibility = Visibility.Visible;
            SearchResult.Visibility = Visibility.Visible;                    
            RefreshPanel.Visibility = Visibility.Collapsed;
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri("https://github.com/ioklo/dcview/", UriKind.Absolute);
            task.Show();
        }

        private async void SearchSite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchSiteItem siteItem = SearchSite.SelectedItem as SearchSiteItem;
            if (siteItem == null) return;

            SearchBox.Text = "";
            SearchBox.IsEnabled = false;
            foreach (var button in allApplicationBar.Buttons)
                ((ApplicationBarIconButton)button).IsEnabled = false;

            SearchResult.ItemsSource = await siteItem.Site.GetBoards();
            SearchBox.IsEnabled = true;
            foreach (var button in allApplicationBar.Buttons)
                ((ApplicationBarIconButton)button).IsEnabled = true;           
            
        }

        // 시작 메뉴에 추가
        private void PinStartPage_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            var entry = item.Tag as Favorites.Entry;

            PinToStart(entry.SiteID, entry.BoardID, entry.DisplayName);
        }

        private void PinStartPageGallery_Click(object sender, RoutedEventArgs e)
        {
            IBoard board = ((FrameworkElement)sender).Tag as IBoard;
            if (board == null) return;

            PinToStart(board.Site.ID, board.ID, board.Name);
        }

        private void PinToStart(string siteID, string boardID, string displayName)
        {
            StandardTileData data = new StandardTileData();
            data.Title = displayName;
            data.BackgroundImage = new Uri("/Background.png", UriKind.Relative);
            data.BackContent = string.Empty;
            data.BackBackgroundImage = new Uri("", UriKind.Relative);
            data.BackTitle = string.Empty;

            Uri uri = new Uri(string.Format("/Views/ViewArticle.xaml?siteID={0}&boardID={1}&boardName={2}", siteID, boardID, displayName), UriKind.Relative);

            try
            {
                if (!ShellTile.ActiveTiles.Any(st => st.NavigationUri == uri))
                    ShellTile.Create(uri, data);
                else
                {
                    MessageBox.Show("이미 시작 화면에 고정되어 있습니다");
                }
            }
            catch
            {
            }
            
        }

        private async void PatternUpdate_Click(object sender, RoutedEventArgs e)
        {
            PatternResetButton.IsEnabled = false;
            PatternUpdateButton.IsEnabled = false;

            string msg = await Task.Factory.StartNew(() => DCRegexManager.Update() );
            
            MessageBox.Show(msg);            

            PatternUpdateButton.IsEnabled = true;
            PatternResetButton.IsEnabled = true;
            
        }

        private void PatternReset_Click(object sender, RoutedEventArgs e)
        {
            DCRegexManager.Reset();
        }
    }
}