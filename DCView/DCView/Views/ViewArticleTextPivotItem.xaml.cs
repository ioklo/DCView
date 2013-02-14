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
using Microsoft.Phone;

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

            var wifiSetting = new ApplicationBarMenuItem();
            wifiSetting.Text = "와이파이 설정";
            wifiSetting.Click += wifiSetting_Click;
            textAppBar.MenuItems.Add(wifiSetting);

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

        private void wifiSetting_Click(object sender, EventArgs e)
        {
            ConnectionSettingsTask task = new ConnectionSettingsTask();
            task.ConnectionSettingsType = ConnectionSettingsType.WiFi;
            task.Show();
        }

        private WatermarkTextBox CreateReplyTextBox(ListBox panel)
        {
            var inputScope = new InputScope();
            inputScope.Names.Add(new InputScopeName() { NameValue = InputScopeNameValue.Chat });

            var textBox = new WatermarkTextBox();

            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            textBox.InputScope = inputScope;
            textBox.WatermarkText = "댓글";
            textBox.Margin = new Thickness(-10, 0, -10, 0);

            textBox.SizeChanged += (o1, e1) =>
            {
                if (FocusManager.GetFocusedElement() == textBox)
                {                    
                    VirtualizingStackPanel vstackPanel = (VirtualizingStackPanel)GetChildren(panel).FirstOrDefault(dobj => dobj is VirtualizingStackPanel);
                    if(vstackPanel != null)
                        vstackPanel.SetVerticalOffset(vstackPanel.ExtentHeight - vstackPanel.ViewportHeight);
                }
            };

            textBox.GotFocus += (o1, e1) => UpdateAppBar();
            textBox.LostFocus += (o1, e1) => UpdateAppBar();

            return textBox;
        }

        private FrameworkElement CreateLoadImageButton(ListBox panel, List<Grid> imgContainers)
        {            
            var button = new Button();
            button.Content = "그림 불러오기";
            button.FontSize = 14;
            button.BorderThickness = new Thickness(1);
            button.HorizontalAlignment = HorizontalAlignment.Center;
            button.Click += (o, e) =>
            {
                button.IsEnabled = false;
                button.Visibility = Visibility.Collapsed;
                LoadImagesAsync(panel, imgContainers);
            };

            var grid = new Grid();
            grid.Children.Add(button);

            return grid;
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

        private async void LoadImagesAsync(ListBox panel, List<Grid> imgContainers)
        {
            List<Task> loadingTasks = new List<Task>();

            foreach(var v in imgContainers)
            {
                Grid grid = v;
                Picture pic = grid.Tag as Picture;
                if (pic == null) continue;

                var task = LoadImageAsync(panel, grid, pic);
                loadingTasks.Add(task);
            }

            await Task.Factory.StartNew(() => Task.WaitAll(loadingTasks.ToArray()));
        }

        private List<WriteableBitmap> LoadImage(Stream stream)
        {
            const int maxWidth = 512;
            const int maxHeight = 1024;

            var bitmap = Dispatcher.InvokeAsync(() => PictureDecoder.DecodeJpeg(stream)).GetResult();

            // 메모리를 적게 쓰기 위해서 다운 사이징
            // 1. width 최대치를 maxWidth로 고정
            if (bitmap.PixelWidth > maxWidth)
            {
                float ratio = (float)maxWidth / bitmap.PixelWidth;

                int downWidth = (int)(bitmap.PixelWidth * ratio);
                int downHeight = (int)(bitmap.PixelHeight * ratio);

                using (MemoryStream ms = new MemoryStream())
                {
                    // SaveJpeg만 리사이즈 기능을 갖고 있나보다..
                    bitmap.SaveJpeg(ms, downWidth, downHeight, 0, 90);
                    ms.Seek(0L, SeekOrigin.Begin);
                    bitmap = Dispatcher.InvokeAsync(() => PictureDecoder.DecodeJpeg(ms)).GetResult();
                }
            }

            // Height가 maxHeight인 꽉차는 비트맵의 개수
            int n = bitmap.PixelHeight / maxHeight;

            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();

            int chunk = maxHeight * bitmap.PixelWidth;
            for (int i = 0; i < n; i++)
            {
                var wbmp = Dispatcher.InvokeAsync(() => new WriteableBitmap(bitmap.PixelWidth, maxHeight)).GetResult();
                Array.Copy(bitmap.Pixels, i * chunk, wbmp.Pixels, 0, chunk);
                bitmaps.Add(wbmp);
            }

            // 남은 비트맵
            int restHeight = bitmap.PixelHeight % maxHeight;
            if (restHeight != 0)
            {
                var wbmp = Dispatcher.InvokeAsync(() => new WriteableBitmap(bitmap.PixelWidth, restHeight)).GetResult();
                Array.Copy(bitmap.Pixels, n * chunk, wbmp.Pixels, 0, bitmap.PixelWidth * restHeight);
                bitmaps.Add(wbmp);
            }

            return bitmaps;            
        }

        // 이미지 로딩 부분 UI 스레드에서 이루어짐
        private async Task LoadImageAsync(ListBox panel, Grid grid, Picture pic)
        {
            // Status를 위한 텍스트 블럭 삽입
            var status = new TextBlock();

            try
            {
                status.Text = "[이미지를 불러오는 중..]";
                grid.Children.Add(status);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(HttpUtility.HtmlDecode(pic.Uri));
                request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
                request.Method = "GET";

                // 빈 스트링인 경우 요청이 예외가 난다
                if (pic.Referer != string.Empty)
                    request.Headers["Referer"] = pic.Referer;
                request.CookieContainer = WebClientEx.CookieContainer;
                
                HttpWebResponse response = (HttpWebResponse)await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null);

                MemoryStream stream = new MemoryStream((int)response.ContentLength);
                await response.GetResponseStream().CopyToAsync(stream);
                               
                stream.Seek(0, SeekOrigin.Begin);

                if (response.ContentType.Equals("image/gif", StringComparison.CurrentCultureIgnoreCase))
                {
                    // grid의 위치를 알아내고
                    int i = panel.Items.IndexOf(grid) + 1;

                    ExtendedImage source = new ExtendedImage();
                    source.SetSource(stream);

                    var image = new AnimatedImage();
                    image.Source = source;
                    image.Tap += image_Tap;
                    image.Tag = pic;
                    image.LoadingFailed += (o, e) =>
                    {
                        var tb = new TextBlock();
                        tb.Text = "로딩 실패";
                        tb.Foreground = new SolidColorBrush(Colors.Red);
                        grid.Children.Add(tb);
                    };
                    panel.Items.Insert(i, image);
                }

                // 이상하게도 PictureDecoder.DecodeJpeg이 png까지 디코딩을 할 수가 있어서.. 그냥 쓰고 있다
                else if (response.ContentType.Equals("image/jpg", StringComparison.CurrentCultureIgnoreCase) ||
                         response.ContentType.Equals("image/jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                         response.ContentType.Equals("image/png", StringComparison.CurrentCultureIgnoreCase))
                {
                    // 큰 이미지를 잘게 나눈다
                    var wbmps = await Task.Factory.StartNew(() => LoadImage(stream));

                    // grid의 위치를 알아내고
                    int i = panel.Items.IndexOf(grid) + 1;
                    foreach (var wbmp in wbmps)
                    {
                        var image = new Image();
                        image.Source = wbmp;
                        image.Tag = pic;
                        image.Tap += image_Tap;
                        panel.Items.Insert(i, image);
                        i++;
                    }
                    // panel.Items.RemoveAt(i);
                }
                else
                {
                    var source = new BitmapImage();
                    source.SetSource(stream);

                    int i = panel.Items.IndexOf(grid) + 1;

                    var image = new Image();
                    image.Source = source;
                    image.Tag = pic;
                    image.Tap += image_Tap;
                    panel.Items.Insert(i, image);
                }

                grid.Children.Clear();
            }
            catch
            {
                status.Text = "[이미지 불러오기 실패]";
            }
   
        }

        
        void image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var image = sender as FrameworkElement;
            if (image == null) return;

            var pic = image.Tag as Picture;
            if (pic == null) return;
 	        
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = new Uri(pic.BrowserUri, UriKind.Absolute);
            task.Show();
        }

        IEnumerable<DependencyObject> GetChildren(DependencyObject obj)
        {
            int count = VisualTreeHelper.GetChildrenCount(obj);
            for(int t = 0; t < count; t++)
            {
                var child = VisualTreeHelper.GetChild(obj, t);
                yield return child;
            }

            for(int t = 0; t < count; t++)
            {
                var child = VisualTreeHelper.GetChild(obj, t);
                foreach (var res in GetChildren(child))
                    yield return res;
            }
        }

        private void ShowArticleText(ArticleData data)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ShowArticleText(data));
                return;
            }

            Contents.Children.Clear();

            // VirtualizingStackPanel을 쓰는 리스트 박스
            var panel = new ListBox();
            panel.ItemContainerStyle = Resources["ListBoxItemStyle"] as Style;
            panel.ItemsPanel = Resources["ListBoxItemsPanelTemplate"] as ItemsPanelTemplate;
            panel.ManipulationCompleted += (o, e) => this.Focus();
            
            // * 제목 title
            var title = new TextBlock();
            title.TextWrapping = TextWrapping.Wrap;
            title.Style = Application.Current.Resources["DCViewTextMediumStyle"] as Style;
            title.Text = HttpUtility.HtmlDecode(article.Title);
            title.FontWeight = FontWeights.Bold;
            title.Margin = new Thickness(0, 12, 0, 12);
            panel.Items.Add(title);

            // * 그림 불러오기 버튼
            List<Grid> imgContainers = new List<Grid>();
            var loadImageButton = CreateLoadImageButton(panel, imgContainers);
            panel.Items.Add(loadImageButton);

            // 그림 들어갈 자리에 Grid 하나씩 넣기..
            foreach (Picture pic in article.Pictures)
            {
                var grid = new Grid();
                grid.Tag = pic;
                grid.Margin = new Thickness(0, 3, 0, 0);

                imgContainers.Add(grid);
                panel.Items.Add(grid);
            }

            // * 12픽셀을 띄기 위해서 마진이 12인 텍스트 블럭 삽입
            var margin = new TextBlock();
            margin.Margin = new Thickness(0, 0, 0, 12);
            panel.Items.Add(margin);

            // * 본문
            foreach (var elem in HtmlElementConverter.GetUIElementFromString(data.Text, tapAction))
            {
                panel.Items.Add(elem);

                if (elem is Grid && elem.Tag is Picture)
                    imgContainers.Add((Grid)elem);
            }

            // * 글쓴이 정보
            var status = new ArticleStatusView();
            status.DataContext = new ArticleViewModel(article);
            status.HorizontalAlignment = HorizontalAlignment.Right;
            panel.Items.Add(status);
            
            // * 댓글
            foreach (var cmt in data.Comments)
            {
                var commentView = new CommentView();
                commentView.DataContext = new CommentViewModel(cmt);
                
                foreach (var grid in HtmlElementConverter.GetUIElementFromString(cmt.Text, tapAction))
                {
                    commentView.Contents.Children.Add(grid);

                    if (grid is Grid && grid.Tag is Picture)
                        imgContainers.Add((Grid)grid);
                }

                panel.Items.Add(commentView);
            }

            // * 댓글을 달 수 있으면 ReplyTextBox 넣기
            if (article.CanWriteComment)
            {
                // ReplyTextBox를 새로 만듦
                replyTextBox = CreateReplyTextBox(panel);
                panel.Items.Add(replyTextBox);
            }

            
            bool bPassiveLoading = (bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] && imgContainers.Count != 0;

            // * 수동읽기가 설정이 되어있으면 버튼을 활성화
            if (!bPassiveLoading)
            {
                loadImageButton.Visibility = Visibility.Collapsed;
                LoadImagesAsync(panel, imgContainers);
            }

            // Contents에 panel을 추가.
            Contents.Children.Add(panel);

            // *스크롤을 처음으로 되돌림
            panel.ScrollIntoView(title);
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
            // 이전 글 안보이게 지우기
            Contents.Children.Clear();
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

        /*private void VirtualizingStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            // ItemsPanelTemplate 인 VirtualizingStackPanel을 얻기 위해서
            DependencyObject dep = sender as DependencyObject;
            while (dep != null)
            {
                if (dep is ListBox)
                {
                    ListBox box = dep as ListBox;
                    box.Tag = sender;
                    return;
                }

                dep = VisualTreeHelper.GetParent(dep);
            }
        }*/
    }
}
