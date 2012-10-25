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
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using DCView.Util;
using MyApps.Common;
using System.IO;

namespace DCView
{
    public partial class ViewArticleTextPivotItem : PivotItem, INotifyActivated
    {
        public class ArticleData
        {
            public string Text { get; set; }
            public ILister<IComment> CommentLister { get; set; }
            public List<IComment> Comments { get; private set; }

            public ArticleData()
            {
                Comments = new List<IComment>();
            }
        }

        MRUCache<IArticle, ArticleData> cachedData = new MRUCache<IArticle, ArticleData>(128);

        ApplicationBar textAppBar = null;  // 텍스트에서의 앱바
        ApplicationBar replyAppBar = null; // 리플 달때의 앱바
        
        IArticle article = null;
        ViewArticle viewArticlePage;

        Grid loadImageGrid = null;
        Button loadImageButton = null;

        CommentedTextBox replyTextBox = null;

        Action<Uri> tapAction = (Action<Uri>)(uri =>
        {
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = uri;
            task.Show();
        });

        public ViewArticleTextPivotItem(ViewArticle viewArticlePage)
        {
            InitializeComponent();
            InitializeAppBar();
            InitializeLoadImageButton();

            this.Header = "내용";
            this.viewArticlePage = viewArticlePage;            
        }

        private void InitializeAppBar()
        {
            textAppBar = new ApplicationBar();
            textAppBar.IsMenuEnabled = true;
            textAppBar.IsVisible = true;

            var refreshTextIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Data/appbar.refresh.rest.png", UriKind.Relative),
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
                IconUri = new Uri("/Data/appbar.check.rest.png", UriKind.Relative),
                Text = "보내기"
            };
            submitReplyIconButton.Click += submitReplyIconButton_Click;
            replyAppBar.Buttons.Add(submitReplyIconButton);
        }

        private CommentedTextBox CreateReplyTextBox()
        {
            var inputScope = new InputScope();
            inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Chat });

            var textBox = new CommentedTextBox();

            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            textBox.InputScope = inputScope;
            textBox.Comment = "댓글";
            textBox.Margin = new Thickness(-15, 0, -15, 0);

            textBox.SizeChanged += (o1, e1) =>
            {
                if (FocusManager.GetFocusedElement() == textBox)
                    ArticleTextScroll.ScrollToVerticalOffset(ArticleText.ActualHeight);
            };

            textBox.GotFocus += (o1, e1) => { UpdateAppBar(); };
            textBox.LostFocus += (o1, e1) => { UpdateAppBar(); };

            return textBox;
        }

        void InitializeLoadImageButton()
        {
            loadImageGrid = new Grid();
            loadImageButton = new Button();
            loadImageButton.Content = "그림 불러오기";
            loadImageButton.FontSize = 14;
            loadImageButton.BorderThickness = new Thickness(1);
            loadImageButton.HorizontalAlignment = HorizontalAlignment.Center;
            loadImageButton.Click += new RoutedEventHandler(loadImageButton_Click);
            loadImageGrid.Children.Add(loadImageButton);
        }


        private void UpdateAppBar()
        {
            if (FocusManager.GetFocusedElement() == replyTextBox)
                viewArticlePage.ApplicationBar = replyAppBar;
            else
                viewArticlePage.ApplicationBar = textAppBar;
        }

        // 글 다시 읽기 처리
        private void refreshTextIconButton_Click(object sender, EventArgs e)
        {
            if (article == null) return;

            GetAndShowArticleText();
        }

        // 
        void submitReplyIconButton_Click(object sender, EventArgs e)
        {
            // 제출하기 전에 로그인 확인 부터
            if (App.Current.LoginInfo.LoginState != LoginInfo.State.LoggedIn)
            {
                viewArticlePage.ShowLoginDialog();
                return;
            }

            string text = replyTextBox.Text;
            CommentSubmit(text, sender as ApplicationBarIconButton);
            this.Focus();
        }

        // 리플 달기
        private bool CommentSubmit(string text, ApplicationBarIconButton submitButton)
        {
            if (article == null) return false;

            if (text.Trim() == string.Empty)
            {
                MessageBox.Show("댓글 내용을 입력해주세요");
                return false;
            }

            submitButton.IsEnabled = false; 

            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Factory.StartNew( () =>
            {
                try
                {
                    if (!article.WriteComment(text, cts.Token))
                    {
                        viewArticlePage.ShowErrorMessage("댓글달기에 실패했습니다 다시 시도해보세요");
                        Dispatcher.BeginInvoke(() => { submitButton.IsEnabled = true; });
                        return;
                    }

                    // 성공 했으면
                    Dispatcher.BeginInvoke(() => 
                    {
                        GetAndShowArticleText(); 
                        submitButton.IsEnabled = true;                         
                    });
                }
                catch
                {
                    viewArticlePage.ShowErrorMessage("댓글달기에 실패했습니다 다시 시도해보세요");
                    Dispatcher.BeginInvoke(() => { submitButton.IsEnabled = true; });
                    return;                    
                }
            });

            return true;
        }

        void LoadImageAsync(Image image, Picture pic)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    HttpWebRequest request = WebRequest.CreateHttp(pic.Uri);
                    request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
                    request.Headers["Referer"] = pic.Referer;
                    request.CookieContainer = WebClientEx.CookieContainer;
                    HttpWebResponse response = (HttpWebResponse)Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null).GetResult();


                    MemoryStream stream = new MemoryStream();
                    response.GetResponseStream().CopyTo(stream);                    
                    
                    Dispatcher.BeginInvoke(() => 
                    {
                        BitmapSource source = new BitmapImage();
                        source.SetSource(stream);
                        image.Source = source; 
                    });
                }
                catch
                {

                }
            });
        }

        private void ShowArticleText(ArticleData data)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ShowArticleText(data));
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
                loadImageButton.Content = "그림 불러오기";
                loadImageButton.IsEnabled = true;                

                ArticleText.Children.Add(loadImageGrid);                
            }
            else
            {
                foreach (Picture pic in article.Pictures)
                {   
                    var image = new Image()
                    {
                        // Source = new BitmapImage(pic.Uri),
                        Margin = new Thickness(3),
                    };

                    LoadImageAsync(image, pic);

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

            foreach (var textBlock in HtmlElementConverter.GetTextBlocksFromString(data.Text, tapAction))
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
            foreach (var cmt in data.Comments)
            {
                var cmtName = new TextBlock();
                cmtName.Text = cmt.Name;
                cmtName.Style = Application.Current.Resources["DCViewTextSmallStyle"] as Style;
                cmtName.Margin = new Thickness(0, 12, 0, 3);
                //cmtName.Foreground = App.Current.Resources["PhoneAccentBrush"] as Brush;
                ArticleText.Children.Add(cmtName);

                foreach (var blocks in HtmlElementConverter.GetTextBlocksFromString(cmt.Text, tapAction))
                    ArticleText.Children.Add(blocks);

                // 댓글마다 충전물좀 넣기
                ArticleText.Children.Add(new Rectangle() { Height = 20 });
            }

            // ReplyTextBox를 새로 만듦
            replyTextBox = CreateReplyTextBox();
            ArticleText.Children.Add(replyTextBox);

            ArticleTextScroll.ScrollToVerticalOffset(0);
        }

        // 글을 웹브라우저에서 읽기         
        private void webViewText_Click(object sender, EventArgs e)
        {
            if (article == null) return;

            var wbTask = new WebBrowserTask();
            wbTask.Uri = article.Uri;
            wbTask.Show();
        }

        // 지금 읽고 있는 글 다시 불러와서 article.Text에 넣기
        private void GetAndShowArticleText()
        {
            ArticleText.Children.Clear(); // 이전 글 안보이게 지우기
            if (article == null) return;

            // 프로그레스 바 켬
            LoadingArticleTextProgressBar.IsIndeterminate = true;
            CancellationTokenSource cts = new CancellationTokenSource();

            Task.Factory.StartNew( () =>
            {
                try
                {
                    string text;                                        
                    if (!article.GetText(cts.Token, out text))
                    {
                        viewArticlePage.ShowErrorMessage("글을 읽어들이는데 실패했습니다. 다시 시도해보세요");
                        Dispatcher.BeginInvoke(() => { LoadingArticleTextProgressBar.IsIndeterminate = false; });
                        return;
                    }

                    // 이것을 기반으로 ArticleData 생성                    
                    ArticleData data = new ArticleData();

                    // TODO: 현재는 첫번째 이후 코멘트를 가져올 방법이 없다 
                    data.Text = text;
                    data.CommentLister = article.GetCommentLister();
                    IEnumerable<IComment> newComments;
                    bool bEnded = data.CommentLister.Next(cts.Token, out newComments);
                    data.Comments.AddRange(newComments);

                    // Cache에 집어넣기
                    cachedData.Add(article, data);

                    // UI를 건드리기 때문에 Dispatcher로 해야한다.
                    ShowArticleText(data);
                    Dispatcher.BeginInvoke(() => { LoadingArticleTextProgressBar.IsIndeterminate = false; });
                }
                catch
                {
                    viewArticlePage.ShowErrorMessage("글을 읽어들이는데 실패했습니다. 다시 시도해보세요");
                    Dispatcher.BeginInvoke(() => { LoadingArticleTextProgressBar.IsIndeterminate = false; });
                }
            });
        }

        public void SetArticle(IArticle article)
        {
            this.article = article;

            ArticleData data;
            if (!cachedData.TryGetValue(article, out data))
            {
                // 글이 없다면
                GetAndShowArticleText();
            }          
            else
                ShowArticleText(data);
        }

        // 버튼을 눌렀을 때 
        private void loadImageButton_Click(object sender, RoutedEventArgs e)
        {
            loadImageButton.Content = string.Format("로딩중 {0}/{1}", 0, article.Pictures.Count);
            loadImageButton.IsEnabled = false;

            int insertIndex = ArticleText.Children.IndexOf(loadImageGrid) + 1;

            List<Image> failedImage = new List<Image>();
            int openedImageCount = 0;

            foreach (Picture pic in article.Pictures)
            {
                Uri curUri = pic.Uri;

                var image = new Image()
                {
                    Margin = new Thickness(3),
                };

                LoadImageAsync(image, pic);

                image.Tap += (o1, e1) =>
                {
                    WebBrowserTask task = new WebBrowserTask();
                    task.Uri = curUri;
                    task.Show();
                };

                // 이미지가 열렸을 때.. 
                image.ImageOpened += delegate(object o1, RoutedEventArgs rea)
                {
                    openedImageCount++;

                    if (failedImage.Count != 0)
                        loadImageButton.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", openedImageCount, article.Pictures.Count, failedImage.Count);
                    else
                        loadImageButton.Content = string.Format("불러오기 {0}/{1}", openedImageCount, article.Pictures.Count);

                    if (failedImage.Count == 0 && openedImageCount == article.Pictures.Count)
                    {
                        // 버튼 제거하기
                        ArticleText.Children.Remove(loadImageGrid);
                        ArticleText.InvalidateArrange();
                    }
                };

                // 이미지 로딩에 실패했을 때..
                image.ImageFailed += (o1, e1) =>
                {
                    if (failedImage.IndexOf(image) == -1)
                        failedImage.Add(image);
                    
                    loadImageButton.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", openedImageCount, article.Pictures.Count, failedImage.Count);
                };

                ArticleText.Children.Insert(insertIndex, image);
                insertIndex++;
            }            
        }

        void INotifyActivated.OnActivated()
        {
            UpdateAppBar();
        }

        
    }
}
