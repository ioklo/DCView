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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Collections.Specialized;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using MyApps.Common;
using MyApps.Common.HtmlParser;

namespace DCView
{
    // 글 목록을 보고 읽는 곳
    public partial class ViewArticle : PhoneApplicationPage
    {       
        // 데이터.. articles
        // Tombstoning 당하면 사라져야 정상인걸까? => 그렇다.. 아무것도 없는 처음 상태로 되돌린다
        IBoard board = null;
        IBoard origBoard = null;
        IBoard searchBoard = null;
        
        ApplicationBar listAppBar = null; // 목록에서의 앱바
        ApplicationBar textAppBar = null; // 텍스트에서의 앱바
        ApplicationBar replyAppBar = null; // 텍스트에서의 앱바
        ApplicationBar searchAppBar = null;

        // 댓글쓰기 관련
        TextBox curReplyTextBox = null;
        Button nextPageButton = null;
        
        Article curArticle = null;

        Action<Uri> tapAction = (Action<Uri>)(uri =>
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = uri;
            task.Show();
        });

        private void InitializeAppbar()
        {   
            // 앱바 처리 

            // 1. 글 목록일때의 앱바
            //   i 목록 다시 읽기
            //   i 글쓰기
            //   i 검색
            //   - 웹브라우저에서 보기
            listAppBar = new ApplicationBar();
            listAppBar.IsMenuEnabled = true;
            listAppBar.IsVisible = true;                        

            var refreshListIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.refresh.rest.png", UriKind.Relative),
                Text = "새로고침"
            };
            refreshListIconButton.Click += refreshListIconButton_Click;
            listAppBar.Buttons.Add(refreshListIconButton);

            var writeIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.edit.rest.png", UriKind.Relative),
                Text = "글쓰기"
            };
            writeIconButton.Click += writeIconButton_Click;
            listAppBar.Buttons.Add(writeIconButton);

            var searchIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.feature.search.rest.png", UriKind.Relative),
                Text = "검색"
            };
            searchIconButton.Click += searchIconButton_Click;
            listAppBar.Buttons.Add(searchIconButton);

            var webViewList = new ApplicationBarMenuItem();
            webViewList.Text = "웹브라우저로 보기";
            webViewList.Click += webViewList_Click;
            listAppBar.MenuItems.Add(webViewList);

            // 2. 글 보기일때의 앱바
            //   i 글 다시 읽기
            //   - 웹브라우저에서 보기
            textAppBar = new ApplicationBar();
            textAppBar.IsMenuEnabled = true;
            textAppBar.IsVisible = true;            

            var refreshTextIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.refresh.rest.png", UriKind.Relative),
                Text = "새로고침"
            };
            refreshTextIconButton.Click += refreshTextIconButton_Click;
            textAppBar.Buttons.Add(refreshTextIconButton);

            var webViewText = new ApplicationBarMenuItem();
            webViewText.Text = "웹브라우저로 보기";
            webViewText.Click += webViewText_Click;
            textAppBar.MenuItems.Add(webViewText);

            // 리플 달기 상태에서의 앱바
            // i 완료

            replyAppBar = new ApplicationBar();
            replyAppBar.IsMenuEnabled = true;
            replyAppBar.IsVisible = true;            

            var submitReplyIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.check.rest.png", UriKind.Relative),
                Text = "보내기"
            };
            submitReplyIconButton.Click += submitReplyIconButton_Click;
            replyAppBar.Buttons.Add(submitReplyIconButton);

            // 검색 상태에서의 앱바
            searchAppBar = new ApplicationBar();
            searchAppBar.IsMenuEnabled = true;
            searchAppBar.IsVisible = true;

            var submitSearchIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.check.rest.png", UriKind.Relative),
                Text = "보내기"
            };
            submitSearchIconButton.Click += submitSearchIconButton_Click;
            searchAppBar.Buttons.Add(submitSearchIconButton);


            UpdateAppBar();
        }

        private void UpdateAppBar()
        {
            if (PivotMain.SelectedItem == PivotList && FocusManager.GetFocusedElement() == SearchTextBox)
                this.ApplicationBar = searchAppBar;
            else if (PivotMain.SelectedItem == PivotList)
                this.ApplicationBar = listAppBar;
            else if (PivotMain.SelectedItem == PivotArticle && curReplyTextBox != null)
                this.ApplicationBar = replyAppBar;
            else if (PivotMain.SelectedItem == PivotArticle)
                this.ApplicationBar = textAppBar;
            else
                this.ApplicationBar = null;
        }

        // 공통 함수
        private void ShowErrorMessage(string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ShowErrorMessage(msg));
                return;
            }

            MessageBox.Show(msg);
        }                

        
        // 생성자
        public ViewArticle()
        {
            InitializeComponent();

            //ArticleText = new ArticleTextAdapter(this);
            InitializeAppbar();

            // 일단 고의적으로  PivotArticle을 뺀다
            PivotMain.Items.Remove(PivotArticle);
        }

        // 오버라이드 함수들
        // 백키를 누르면 바로 이전것으로 돌아가지 않고, 목록에 있을때만 뒤로 되돌아간다
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (PivotMain.SelectedItem == PivotArticle)
            {
                PivotMain.SelectedItem = PivotList;
                e.Cancel = true;
            }
        }

        // 페이지에서 나가려고 할 때
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        // 이 페이지로 들어오려고 할 때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 버튼 모두 삭제
            UpdateAppBar();

            // 초기화 되면 archive가 없어진다
            if (board != null)
            {
                return;
            }
       
            // archive 설정
            string id;

            if (!NavigationContext.QueryString.TryGetValue("id", out id))
            {
                NavigationService.GoBack();
                return;
            }

            // 다음글 버튼
            nextPageButton = new Button()
            {
                Content = "다음글",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            nextPageButton.Click += (o1, e1) =>
            {
                GetNextArticleList();
            };

            origBoard = new DCInsideBoard(id);
            ConnectBoard(origBoard);

            // 왼쪽 상단 갤러리
            PivotMain.Title = App.Current.GalleryList[id].Name + " 갤러리";

            RefreshArticleList();
        }

        void OnBoardCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        Dispatcher.BeginInvoke(() =>
                        {

                            foreach (var item in args.NewItems)
                                ArticleList.Items.Insert(ArticleList.Items.Count - 1, item);
                        });
                        break;
                    }

                case NotifyCollectionChangedAction.Reset:
                    {
                        ArticleList.Items.Clear();
                        if (args.NewItems != null)
                        {
                            foreach (var item in args.NewItems)
                            {
                                ArticleList.Items.Add(item);
                            }
                        }
                        ArticleList.Items.Add(nextPageButton);
                        break;
                    }

            }
        }

        private void ConnectBoard(IBoard board)
        {
            if (this.board != null)
            {
                ((INotifyCollectionChanged)board.Articles).CollectionChanged -= OnBoardCollectionChanged;
            }

            this.board = board;

            ArticleList.Items.Clear();
            foreach (var article in board.Articles)
                ArticleList.Items.Add(article);

            ((INotifyCollectionChanged)board.Articles).CollectionChanged += OnBoardCollectionChanged;
        }
        
        // 하는 행동        

        // 글 목록 리스트 초기화
        private void RefreshArticleList()
        {
            board.ResetArticleList(0);
            GetNextArticleList();
        }

        // 다음 글 목록 얻어오기
        private void GetNextArticleList()
        {
            nextPageButton.IsEnabled = false;
            LoadingArticleListProgressBar.IsIndeterminate = true;

            CancellationTokenSource cts = new CancellationTokenSource();
            board.GetNextArticleList(cts.Token).ContinueWith(prevTask =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    nextPageButton.IsEnabled = true;
                    LoadingArticleListProgressBar.IsIndeterminate = false;
                });                
            });
        }

        // 지금 읽고 있는 글 다시 불러와서 article.Text에 넣기
        private void RefreshArticleText(Article article)
        {
            // 이전 글 안보이게 지우기
            ArticleText.Children.Clear();
            if (article == null) return;

            // 프로그레스 바 켬
            LoadingArticleTextProgressBar.IsIndeterminate = true;

            CancellationTokenSource cts = new CancellationTokenSource();
            board.GetArticleText(article, cts.Token)
            .ContinueWith(prevTask =>
            {
                if (prevTask.IsCanceled || prevTask.Exception != null)
                {
                    ShowErrorMessage("글을 읽어들이는데 실패했습니다. 다시 시도해보세요");
                    return;
                }

                // UI를 건드리기 때문에 Dispatcher로 해야한다.
                ShowArticleText(article);
                Dispatcher.BeginInvoke(() => { LoadingArticleTextProgressBar.IsIndeterminate = false; });
            });
        }

        // 화면에 article의 내용을 보여주기
        private void ShowArticleText(Article article)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ShowArticleText(article));
                return;
            }

            // 일단 기존의 ArticleText를 없앤다
            ArticleText.Children.Clear();

            // 제목
            var title = new TextBlock();
            title.TextWrapping = TextWrapping.Wrap;
            title.Style = Application.Current.Resources["DCViewTextMediumStyle"] as Style;
            title.Text = article.Title;
            title.FontWeight = FontWeights.Bold;
            title.Margin = new Thickness(0, 12, 0, 12);
            ArticleText.Children.Add(title);


            if ((bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] && article.Pictures.Count != 0)
            {
                var button = new Button()
                {
                    Content = "그림 불러오기",
                    FontSize = 14,
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center,
                };

                int index = ArticleText.Children.Count;
                int imageEmbedIndex = index + 1;

                // 버튼을 눌렀을 때 
                button.Click += (o, e) =>
                {
                    button.Content = string.Format("로딩중 {0}/{1}", 0, article.Pictures.Count);
                    button.IsEnabled = false;
                    int failedCount = 0;
                    int count = 0;

                    foreach (Picture pic in article.Pictures)
                    {
                        Uri curUri = pic.Uri;

                        var image = new Image()
                        {
                            Source = new BitmapImage(curUri),
                            Margin = new Thickness(3),
                        };

                        image.Tap += (o1, e1) =>
                        {
                            WebBrowserTask task = new WebBrowserTask();
                            task.Uri = curUri;
                            task.Show();
                        };

                        // 이미지가 열렸을 때.. 
                        image.ImageOpened += delegate(object o1, RoutedEventArgs rea)
                        {
                            count++;

                            if (failedCount != 0)
                                button.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", count, article.Pictures.Count, failedCount);
                            else
                                button.Content = string.Format("불러오기 {0}/{1}", count, article.Pictures.Count, failedCount);

                            if (failedCount == 0 && count == article.Pictures.Count)
                            {
                                // 버튼 제거하기
                                ArticleText.Children.Remove(button);
                                ArticleText.InvalidateArrange();
                            }
                        };

                        // 이미지 로딩에 실패했을 때..
                        image.ImageFailed += (o1, e1) =>
                        {
                            failedCount++;
                            count++;

                            button.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", 0, article.Pictures.Count, failedCount);
                        };

                        ArticleText.Children.Insert(imageEmbedIndex, image);

                        imageEmbedIndex++;
                    }
                };

                ArticleText.Children.Add(button);
            }
            else
            {
                foreach (Picture pic in article.Pictures)
                {
                    var image = new Image()
                    {
                        Source = new BitmapImage(pic.Uri),
                        Margin = new Thickness(3),
                    };

                    image.Tap += (o1, e1) =>
                    {
                        WebBrowserTask task = new WebBrowserTask();
                        task.Uri = pic.Uri;
                        task.Show();
                    };

                    image.ImageOpened += delegate(object o1, RoutedEventArgs rea)
                    {
                        ArticleText.InvalidateArrange();
                    };

                    ArticleText.Children.Add(image);
                }
            }

            var margin = new TextBlock();
            margin.Margin = new Thickness(0, 0, 0, 12);
            ArticleText.Children.Add(margin);

            foreach (var textBlock in HtmlParser.GetTextBlocksFromString(article.Text, tapAction))
                ArticleText.Children.Add(textBlock);

            // 기타 정보
            var status = new TextBlock();
            status.Text = article.Status;
            status.HorizontalAlignment = HorizontalAlignment.Stretch;
            status.TextAlignment = TextAlignment.Right;
            status.Style = Application.Current.Resources["DCViewTextSmallStyle"] as Style;
            status.Margin = new Thickness(0, 0, 0, 12);
            ArticleText.Children.Add(status);

            // 댓글
            foreach (var cmt in article.Comments)
            {
                var cmtName = new TextBlock();
                cmtName.Text = cmt.Name;
                cmtName.Style = Application.Current.Resources["DCViewTextSmallStyle"] as Style;
                cmtName.Margin = new Thickness(0, 12, 0, 3);
                //cmtName.Foreground = App.Current.Resources["PhoneAccentBrush"] as Brush;
                ArticleText.Children.Add(cmtName);

                foreach (var blocks in HtmlParser.GetTextBlocksFromString(cmt.Text, tapAction))
                    ArticleText.Children.Add(blocks);

                // 댓글마다 충전물좀 넣기
                ArticleText.Children.Add(new Rectangle() { Height = 20 });
            }

            // var cookies = WebClientEx.CookieContainer.GetCookies(new Uri("http://gall.dcinside.com"));
            // if (cookies["PHPSESSID"] != null)
            // 댓글 쓰기 버튼 누르기 -> 로그인 -> 로그인 성공 -> 댓글을 남기는 작업 -> 원래 페이지로 돌아와서 페이지 업데이트            
            if (App.Current.LoginInfo.LoginState == LoginInfo.State.LoggedIn)
            {
                var inputScope = new InputScope();
                inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Chat });

                var replyText = new TextBox()
                {
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                    InputScope = inputScope,
                    IsTabStop = false,
                };

                replyText.SizeChanged += (o1, e1) =>
                {
                    if (curReplyTextBox != null)
                        ArticleTextScroll.ScrollToVerticalOffset(ArticleText.ActualHeight);
                };

                replyText.GotFocus += (o1, e1) =>
                {
                    curReplyTextBox = replyText;

                    // 글쓰기 버튼 제거, 전송 버튼 추가
                    UpdateAppBar();
                };

                replyText.LostFocus += (o1, e1) =>
                {       
                    curReplyTextBox = null;
                    UpdateAppBar();
                };

                ArticleText.Children.Add(replyText);

                // 자동으로 늘어나는 replyTex

                // reply_Click                
            }

            ArticleTextScroll.ScrollToVerticalOffset(0);
        }

        // 리플 달기
        private bool CommentSubmit(string text)
        {
            if (curArticle == null) return false;            

            if (text.Trim() == string.Empty)
                return false;

            CancellationTokenSource cts = new CancellationTokenSource();

            board.WriteComment(curArticle, text, cts.Token)
            .ContinueWith(prevTask =>
            {
                if (prevTask.IsCanceled || prevTask.IsFaulted)
                {
                    ShowErrorMessage("댓글달기에 실패했습니다 다시 시도해보세요");
                    return;
                }

                // 글 다시 불러오기
                RefreshArticleText(curArticle);
            });
            return true;
        }

        
        // 이벤트 핸들러
        // 목록 다시 읽기버튼 처리
        private void refreshListIconButton_Click(object sender, EventArgs e)
        {
            RefreshArticleList();
        }

        // 글쓰기 처리
        private void writeIconButton_Click(object sender, EventArgs e)
        {
            // 여기가 에러 
            // NavigationService.Navigate(new Uri(string.Format("/Views/WriteArticle.xaml?id={0}&name={1}&site={2}&pcsite={3}",
            //     archive.ID, NavigationContext.QueryString["name"], archive.Site, archive.PCSite), UriKind.Relative));
        }

        // 검색
        private void searchIconButton_Click(object sender, EventArgs e)
        {
            if (SearchPanel.Visibility == Visibility.Visible)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                return;
            }

            SearchPanel.Visibility = Visibility.Visible;
            SearchTextBox.Focus();            
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdateAppBar();
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {            
            UpdateAppBar();
        }

        private void submitSearchIconButton_Click(object sender, EventArgs e)
        {
            // 내용을 바탕으로 searchBoard 를 만든다
            string id;
            if (!NavigationContext.QueryString.TryGetValue("id", out id))
                return;

            PivotList.Header = "검색";

            DCInsideBoardSearch.SearchType searchType = DCInsideBoardSearch.SearchType.Subject;
            if (SearchTypeSubject.IsChecked ?? false)
                searchType = DCInsideBoardSearch.SearchType.Subject;
            else if (SearchTypeContent.IsChecked ?? false)
                searchType = DCInsideBoardSearch.SearchType.Content;
            else if (SearchTypeName.IsChecked ?? false)
                searchType = DCInsideBoardSearch.SearchType.Name;

            searchBoard = new DCInsideBoardSearch(id, SearchTextBox.Text, searchType);
            ConnectBoard(searchBoard);

            // 검색창 닫기
            SearchPanel.Visibility = Visibility.Collapsed;
            RefreshArticleList();
            this.Focus();
        }

        // 글 목록 웹브라우저에서 보기 메뉴 처리
        private void webViewList_Click(object sender, EventArgs e)
        {
            var wbTask = new WebBrowserTask();
            wbTask.Uri = board.GetBoardUri();
            wbTask.Show();
        }

        // 글 다시 읽기 처리
        private void refreshTextIconButton_Click(object sender, EventArgs e)
        {
            if (curArticle == null) return;
            RefreshArticleText(curArticle);
        }

        // 글을 웹브라우저에서 읽기         
        private void webViewText_Click(object sender, EventArgs e)
        {
            if (curArticle == null) return;

            var wbTask = new WebBrowserTask();
            wbTask.Uri = board.GetArticleUri(curArticle);
            wbTask.Show();
        }

        // 
        void submitReplyIconButton_Click(object sender, EventArgs e)
        {
            if (curReplyTextBox == null)
                return;

            if (CommentSubmit(curReplyTextBox.Text))
            {
                // 일단 제출했으면 
                (sender as ApplicationBarIconButton).IsEnabled = false;
            }
        }

        // 글 목록에서 글을 클릭했을 때
        private void ArticleListItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Article article = (sender as FrameworkElement).Tag as Article;
            if (article == null)
                return;

            curArticle = article;

            // '내용' 탭이 없다면 이제 추가해 준다            
            if (!PivotMain.Items.Contains(PivotArticle))
                PivotMain.Items.Add(PivotArticle);

            // '내용' 탭으로 화면 전환
            PivotMain.SelectedItem = PivotArticle;

            // 글이 없다면
            if (article.Text == null)
                RefreshArticleText(article);
            else
                ShowArticleText(article);
        }

        private void ArticleList_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.Focus();
        }

        private void PivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAppBar();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            searchBoard = null;
            SearchPanel.Visibility = Visibility.Collapsed;
            PivotList.Header = "목록";

            ConnectBoard(origBoard);
            UpdateAppBar();
        }

        private void SearchType_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Focus();
        }      
        
    }
}