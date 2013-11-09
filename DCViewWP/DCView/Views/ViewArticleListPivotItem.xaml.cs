using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DCView.Board;
using DCView.Misc;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace DCView
{
    // 아티클을 보여줌
    public partial class ViewArticleListPivotItem : PivotItem, INotifyActivated
    {
        IBoard board = null;
        Button nextPageButton = null;        
        ILister<IArticle> articleLister;
        protected ViewArticle viewArticlePage = null; // parent page        
        protected ApplicationBar appBar = null;   // 목록에서의 앱바
        
        public ViewArticleListPivotItem(ViewArticle viewArticlePage, IBoard board)
        {
            InitializeComponent();            
            InitializeAppBar(board);
            InitializeNextButton();

            this.viewArticlePage = viewArticlePage;
            this.board = board;
            this.Header = "목록";            
        }

        private void InitializeAppBar(IBoard board)
        {
            appBar = new ApplicationBar();
            appBar.IsMenuEnabled = true;
            appBar.IsVisible = true;

            var refreshListIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Data/appbar.refresh.rest.png", UriKind.Relative),
                Text = "새로고침"
            };
            refreshListIconButton.Click += refreshListIconButton_Click;
            appBar.Buttons.Add(refreshListIconButton);

            if (board.CanWriteArticle())
            {
                var writeIconButton = new ApplicationBarIconButton()
                {
                    IconUri = new Uri("/Data/appbar.edit.rest.png", UriKind.Relative),
                    Text = "글쓰기"
                };
                writeIconButton.Click += writeIconButton_Click;
                appBar.Buttons.Add(writeIconButton);
            }

            if (board.CanSearch())
            {
                var searchIconButton = new ApplicationBarIconButton()
                {
                    IconUri = new Uri("/Data/appbar.feature.search.rest.png", UriKind.Relative),
                    Text = "검색"
                };
                searchIconButton.Click += searchIconButton_Click;
                appBar.Buttons.Add(searchIconButton);
            }

            foreach (var value in board.BoardOptions)
            {
                var option = value;
                var menuItem = new ApplicationBarMenuItem();

                menuItem.Text = option.Toggle ? value.Display + " 끔" : value.Display + " 켬";
                menuItem.Click += (o, e) =>
                {
                    option.Toggle = !option.Toggle;
                    menuItem.Text = value.Toggle ? value.Display + " 끔" : value.Display + " 켬";

                    RefreshArticleList();
                };
                appBar.MenuItems.Add(menuItem);
            }


            var webViewList = new ApplicationBarMenuItem();
            webViewList.Text = "웹브라우저로 보기";
            webViewList.Click += webViewList_Click;
            appBar.MenuItems.Add(webViewList);            

            var wifiSetting = new ApplicationBarMenuItem();
            wifiSetting.Text = "와이파이 설정";
            wifiSetting.Click += wifiSetting_Click;
            appBar.MenuItems.Add(wifiSetting);
        }

        private void wifiSetting_Click(object sender, EventArgs e)
        {
            ConnectionSettingsTask task = new ConnectionSettingsTask();
            task.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            task.Show();
        }

        private void InitializeNextButton()
        {
            // 다음글 버튼
            nextPageButton = new Button();
            nextPageButton.Content = "다음글";
            nextPageButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            nextPageButton.Click += (o1, e1) => { GetNextArticleList(); };
        }

        protected virtual ILister<IArticle> GetLister()
        {
            return board.GetArticleLister(0);
        }        

        /* 외부 인터페이스 */

        // 글 목록 초기화 하기
        public void RefreshArticleList()
        {
            articleLister = GetLister();
            ArticleList.Items.Clear();
            ArticleList.Items.Add(nextPageButton);
            GetNextArticleList();
        }        

        // 다음 글 목록 얻어오기
        private async void GetNextArticleList()
        {
            nextPageButton.IsEnabled = false;
            LoadingArticleListProgressBar.IsIndeterminate = true;

            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                IEnumerable<IArticle> newArticles = null;
                bool bEnded = await Task.Factory.StartNew(
                    () => { return articleLister.Next(out newArticles); }, cts.Token);

                // TODO: 리스트의 마지막일때 처리

                foreach (var item in newArticles)
                {
                    ArticleList.Items.Insert(ArticleList.Items.Count - 1, new ArticleViewModel(item));
                }
            }
            catch
            {
                MessageBox.Show("목록을 가져오는데 실패했습니다.");                
            }

            nextPageButton.IsEnabled = true;
            LoadingArticleListProgressBar.IsIndeterminate = false;
        }

        // 앱바를 업데이트 하기
        private void UpdateAppBar()
        {
            viewArticlePage.ApplicationBar = appBar;
        }

        // 검색창을 껐다 켰다 하기
        private void ToggleSearchPanel()
        {
            if (SearchPanel.Visibility == Visibility.Visible)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                return;
            }

            SearchPanel.Visibility = Visibility.Visible;
            SearchTextBox.Focus();
        }

        // 해당 Entry 삭제
        public void DeleteArticleEntry(string articleID)
        {
            int size = ArticleList.Items.Count;
            for(int t = 0; t < size; t++)
            {
                var viewModel = ArticleList.Items[t] as ArticleViewModel;
                if (viewModel == null) continue;

                if (viewModel.Article.ID == articleID)
                {
                    ArticleList.Items.RemoveAt(t);                    
                    return;
                }
            }
        }

        /* 이벤트 핸들러 */

        // 검색창의 검색 버튼 클릭
        protected void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchType searchType = SearchType.Subject;
            if (SearchTypeSubject.IsChecked ?? false)
                searchType = SearchType.Subject;
            else if (SearchTypeContent.IsChecked ?? false)
                searchType = SearchType.Content;
            else if (SearchTypeName.IsChecked ?? false)
                searchType = SearchType.Name;

            // 검색창 닫기
            SearchPanel.Visibility = Visibility.Collapsed;

            // 메인페이지에 검색 페이지로 가도록 요청
            viewArticlePage.Search(board, SearchTextBox.Text, searchType);            
        }

        // 검색창의 검색 종류 라디오 버튼 클릭
        protected void SearchType_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }

        // 리스트에서 손가락을 뗏을 때
        protected void ArticleList_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.Focus();
        }

        // 글 목록에서 글을 클릭했을 때
        protected void ArticleListItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {            
            IArticle article = (sender as FrameworkElement).Tag as IArticle;
            if (article == null)
                return;

            viewArticlePage.SelectArticle(article);            
        }
        

        /* 앱바 이벤트 핸들러 */

        // 다시 읽기 아이콘 클릭
        private void refreshListIconButton_Click(object sender, EventArgs e)
        {
            RefreshArticleList();
        }

        // 글쓰기 아이콘 클릭
        private void writeIconButton_Click(object sender, EventArgs e)
        {
            viewArticlePage.ShowWriteForm(board);
        }

        // 검색 아이콘 클릭
        private void searchIconButton_Click(object sender, EventArgs e)
        {
            if (!board.CanSearch())
            {
                MessageBox.Show("검색을 지원하지 않습니다");
                return;
            }

            ToggleSearchPanel();
        }

        // 웹브라우저에서 보기 메뉴 클릭
        private void webViewList_Click(object sender, EventArgs e)
        {
            var wbTask = new WebBrowserTask();
            wbTask.Uri = board.Uri;
            wbTask.Show();
        }

        void INotifyActivated.OnActivated()
        {
            UpdateAppBar();
        }
    }
}
