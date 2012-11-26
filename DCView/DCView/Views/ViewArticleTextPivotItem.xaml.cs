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
using DCView.Lib;
using System.IO;
using ImageTools.Controls;
using ImageTools;
using CS.Windows.Controls;

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

        WatermarkTextBox replyTextBox = null;

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

        private WatermarkTextBox CreateReplyTextBox()
        {
            var inputScope = new InputScope();
            inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Chat });

            var textBox = new WatermarkTextBox();

            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            textBox.InputScope = inputScope;
            textBox.WatermarkText = "댓글";
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

        private Button CreateLoadImageButton(List<Tuple<Grid, Picture>> imgContainers)
        {
            var grid = new Grid();            

            var button = new Button();
            button.Content = "그림 불러오기";
            button.FontSize = 14;
            button.BorderThickness = new Thickness(1);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.Click += (o, e) =>
            {
                button.IsEnabled = false;
                button.Visibility = Visibility.Collapsed;
                LoadImagesAsync(imgContainers);
            };
            return button;            
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
            this.Focus();
            CommentSubmit(text, sender as ApplicationBarIconButton);
        }

        // 리플 달기
        private async void CommentSubmit(string text, ApplicationBarIconButton submitButton)
        {
            if (article == null) return;

            if (text.Trim() == string.Empty)
            {
                MessageBox.Show("댓글 내용을 입력해주세요");
                return;
            }

            submitButton.IsEnabled = false; 

            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                bool succ = await Task.Factory.StartNew(
                    () => { return article.WriteComment(text, cts.Token); }, cts.Token);

                if (!succ)
                {
                    MessageBox.Show("댓글달기에 실패했습니다 다시 시도해보세요");
                    Dispatcher.BeginInvoke(() => { submitButton.IsEnabled = true; });
                    return;
                }

                // 성공 했으면
                GetAndShowArticleText();
            }
            catch
            {
                MessageBox.Show("댓글달기에 실패했습니다 다시 시도해보세요");                
            }

            submitButton.IsEnabled = true;
        }

        private async void LoadImagesAsync(List<Tuple<Grid, Picture>> imgContainers)
        {
            await Task.Factory.StartNew( () => 
            {
                List<Task> loadingTasks = new List<Task>();

                foreach(var tuple in imgContainers)
                {
                    loadingTasks.Add(LoadImageAsync(tuple.Item1, tuple.Item2));
                }

                Task.WaitAll(loadingTasks.ToArray());
            });
        }

        // 이미지 로딩 부분
        private async Task LoadImageAsync(Grid grid, Picture pic)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pic.Uri);
                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
                request.Method = "GET";
                request.Headers["Referer"] = pic.Referer;
                request.CookieContainer = WebClientEx.CookieContainer;
                
                HttpWebResponse response = (HttpWebResponse)await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null);

                MemoryStream stream = new MemoryStream((int)response.ContentLength);
                await response.GetResponseStream().CopyToAsync(stream);
                               
                stream.Seek(0, SeekOrigin.Begin);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (response.ContentType.Equals("image/gif", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ExtendedImage source = new ExtendedImage();
                        source.SetSource(stream);

                        var image = new AnimatedImage();
                        image.Margin = new Thickness(3);
                        image.Source = source;
                        image.Tap += image_Tap;
                        image.LoadingFailed += (o, e) =>
                        {
                            var tb = new TextBlock();
                            tb.Text = "로딩 실패";
                            tb.Foreground = new SolidColorBrush(Colors.Red);
                            grid.Children.Add(tb);
                        };
                        grid.Children.Add(image);
                    }
                    else
                    {
                        var source = new BitmapImage();
                        source.SetSource(stream);

                        var image = new Image();
                        image.Margin = new Thickness(3);
                        image.Source = source;
                        image.Tag = pic;
                        image.Tap += image_Tap;
                        image.ImageFailed += (o, e) =>
                        {
                            var tb = new TextBlock();
                            tb.Text = "로딩 실패";
                            tb.Foreground = new SolidColorBrush(Colors.Red);
                            grid.Children.Add(tb);
                        };
                        grid.Children.Add(image);
                    }
                });
            }
            catch
            {

            }            
        }

        
        void image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var image = sender as Image;
            if (image == null) return;

            var pic = image.Tag as Picture;
            if (pic == null) return;
 	        
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(pic.Uri, UriKind.Absolute);
            task.Show();
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

            List<Tuple<Grid, Picture>> imgContainers = new List<Tuple<Grid, Picture>>();

            
            Button loadImgBtn = CreateLoadImageButton(imgContainers);
            Grid loadImggrid = new Grid();
            loadImggrid.Children.Add(loadImgBtn);
            ArticleText.Children.Add(loadImggrid);

            // 그림 들어갈 자리에 Grid 하나씩 넣기..
            foreach (Picture p in article.Pictures)
            {
                Picture pic = p;
                var grid = new Grid();

                imgContainers.Add(Tuple.Create(grid, pic));
                ArticleText.Children.Add(grid);
            }

            var margin = new TextBlock();
            margin.Margin = new Thickness(0, 0, 0, 12);
            ArticleText.Children.Add(margin);

            foreach (var tuple in HtmlElementConverter.GetUIElementFromString(data.Text, tapAction))
            {
                ArticleText.Children.Add(tuple.Item1);

                if (tuple.Item2 != null)
                    imgContainers.Add(Tuple.Create((Grid)tuple.Item1, tuple.Item2));
            }

            // 기타 정보
            var status = new TextBlock();
            status.Text = article.Status;
            status.HorizontalAlignment = HorizontalAlignment.Stretch;
            status.TextAlignment = TextAlignment.Right;
            status.Style = Application.Current.Resources["DCViewTextSmallStyle"] as Style;
            status.Margin = new Thickness(0, 0, 0, 0);
            ArticleText.Children.Add(status);

            // 댓글
            foreach (var cmt in data.Comments)
            {
                var rect = new System.Windows.Shapes.Rectangle();
                rect.HorizontalAlignment = HorizontalAlignment.Stretch;
                rect.VerticalAlignment = VerticalAlignment.Stretch;
                rect.Fill = (Brush)App.Current.Resources["PhoneAccentBrush"];
                rect.SetValue(Grid.ColumnProperty, 0);

                var cmtPanel = new StackPanel();
                cmtPanel.Margin = new Thickness(6, 0, 0, 0);
                cmtPanel.SetValue(Grid.ColumnProperty, 1);

                foreach (var tuple in HtmlElementConverter.GetUIElementFromString(cmt.Text, tapAction))
                {
                    cmtPanel.Children.Add(tuple.Item1);

                    if (tuple.Item2 != null)
                        imgContainers.Add(Tuple.Create((Grid)tuple.Item1, tuple.Item2));
                }

                var cmtName = new TextBlock();
                cmtName.Text = cmt.Name;
                cmtName.Style = Application.Current.Resources["DCViewTextSmallStyle"] as Style;
                cmtName.Margin = new Thickness(0, 3, 0, 3);
                cmtPanel.Children.Add(cmtName);
                

                var cmtGrid = new Grid();
                
                cmtGrid.Margin = new Thickness(cmt.Level * 20, 10, 0, 10);
                cmtGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5) });
                cmtGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                cmtGrid.Children.Add(rect);
                cmtGrid.Children.Add(cmtPanel);
                
                ArticleText.Children.Add(cmtGrid);
            }

            if (article.CanWriteComment)
            {
                // ReplyTextBox를 새로 만듦
                replyTextBox = CreateReplyTextBox();
                ArticleText.Children.Add(replyTextBox);
            }

            ArticleTextScroll.ScrollToVerticalOffset(0);

            bool bPassiveLoading = (bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] && imgContainers.Count != 0;

            if (!bPassiveLoading)
            {
                loadImgBtn.Visibility = Visibility.Collapsed;
                LoadImagesAsync(imgContainers);
            }
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
        private async void GetAndShowArticleText()
        {
            ArticleText.Children.Clear(); // 이전 글 안보이게 지우기
            if (article == null) return;

            // 프로그레스 바 켬
            LoadingArticleTextProgressBar.IsIndeterminate = true;            

            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();                
                ArticleData data = new ArticleData();

                bool result = await Task.Factory.StartNew( () => 
                {
                    string text = null;
                    if (!article.GetText(cts.Token, out text)) return false;

                    // ArticleData 생성                                        

                    // TODO: 현재는 첫번째 이후 코멘트를 가져올 방법이 없다 
                    data.Text = text;
                    data.CommentLister = article.GetCommentLister();

                    IEnumerable<IComment> newComments;
                    bool bEnded = data.CommentLister.Next(cts.Token, out newComments);

                    data.Comments.AddRange(newComments);

                    // Cache에 집어넣기
                    cachedData.Add(article, data);
                    return true;
                
                }, cts.Token);

                if (!result)
                {
                    MessageBox.Show("글을 읽어들이는데 실패했습니다. 다시 시도해보세요");                    
                    return;
                }
                
                ShowArticleText(data);
            }
            catch
            {
                MessageBox.Show("글을 읽어들이는데 실패했습니다. 다시 시도해보세요");
                
            }
            finally
            {
                LoadingArticleTextProgressBar.IsIndeterminate = false;
            }
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

        void INotifyActivated.OnActivated()
        {
            UpdateAppBar();
        }

        
    }
}
