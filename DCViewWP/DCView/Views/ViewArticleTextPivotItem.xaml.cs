using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CS.Windows.Controls;
using DCView.Board;
using DCView.Lib;
using DCView.Util;
using ImageTools;
using ImageTools.Controls;
using Microsoft.Phone;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;

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

            var deleteArticleIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Data/delete.png", UriKind.Relative),
                Text = "삭제"
            };

            deleteArticleIconButton.Click += deleteArticleIconButton_Click;
            textAppBar.Buttons.Add(deleteArticleIconButton);


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

        private async void deleteArticleIconButton_Click(object sender, EventArgs e)
        {
            if (!article.Board.CanDeleteArticle())
            {
                MessageBox.Show("글 삭제를 지원하지 않습니다");
                return;
            }

            // 삭제하시겠냐고 먼저 물어봐야 한다.
            var confirmResult = MessageBox.Show("이 글을 삭제하시겠습니까?", "확인", MessageBoxButton.OKCancel);
            if (confirmResult != MessageBoxResult.OK) return;

            viewArticlePage.IsEnabled = false;

            var deleteResult = await Task<bool>.Factory.StartNew(() =>
            {
                try
                {
                    return article.Board.DeleteArticle(article.ID);
                }
                catch
                {
                    return false;
                }                
            });

            viewArticlePage.IsEnabled = true;

            if (!deleteResult)
            {
                MessageBox.Show("글 삭제에 실패했습니다");
                return;
            }

            viewArticlePage.DeleteArticleEntry(article.ID);
            viewArticlePage.RemoveArticleTab();
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
                    VirtualizingStackPanel vstackPanel = panel.Tag as VirtualizingStackPanel;

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
            button.Click += async (o, e) =>
            {
                button.IsEnabled = false;
                button.Visibility = Visibility.Collapsed;
                await LoadImagesAsync(panel, imgContainers);
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
                    () => { return article.WriteComment(text); }, cts.Token);

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

        private async Task LoadImagesAsync(ListBox panel, List<Grid> imgContainers)
        {
            foreach(var v in imgContainers)
            {
                Grid grid = v;
                Picture pic = grid.Tag as Picture;
                if (pic == null) continue;

                await LoadImageAsync(panel, grid, pic);
            }
        }

        private List<WriteableBitmap> LoadImage(Stream stream)
        {
            const int maxWidth = 512;
            const int maxHeight = 1024;

            var bitmap = Dispatcher.InvokeAsync(() => PictureDecoder.DecodeJpeg(stream)).Result;

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
                    bitmap = Dispatcher.InvokeAsync(() => PictureDecoder.DecodeJpeg(ms)).Result;
                }
            }

            // Height가 maxHeight인 꽉차는 비트맵의 개수
            int n = bitmap.PixelHeight / maxHeight;

            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();

            int chunk = maxHeight * bitmap.PixelWidth;
            for (int i = 0; i < n; i++)
            {
                var wbmp = Dispatcher.InvokeAsync(() => new WriteableBitmap(bitmap.PixelWidth, maxHeight)).Result;
                Array.Copy(bitmap.Pixels, i * chunk, wbmp.Pixels, 0, chunk);
                bitmaps.Add(wbmp);
            }

            // 남은 비트맵
            int restHeight = bitmap.PixelHeight % maxHeight;
            if (restHeight != 0)
            {
                var wbmp = Dispatcher.InvokeAsync(() => new WriteableBitmap(bitmap.PixelWidth, restHeight)).Result;
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
                status.Text = "[이미지를 불러오는 중...]";
                grid.Children.Add(status);

                using(var handler = new HttpClientHandler() { CookieContainer = WebClientEx.CookieContainer })
                using(var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
                    client.DefaultRequestHeaders.Referrer = article.Uri;

                    var response = await client.GetAsync(HttpUtility.HtmlDecode(pic.Uri));

                    var stream = await response.Content.ReadAsStreamAsync();
                    var contentType = response.Content.Headers.ContentType;
                    string extension = GetExtension(response.Content.Headers);

                    if (contentType.MediaType.Equals("image/gif", StringComparison.CurrentCultureIgnoreCase) || 
                        extension == ".gif")
                    {
                        // grid의 위치를 알아내고
                        int i = panel.Items.IndexOf(grid) + 1;

                        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                        ExtendedImage source = new ExtendedImage();
                        source.SetSource(stream);                        
                        source.LoadingCompleted += (e, o) => { tcs.TrySetResult(true); };
                        source.LoadingFailed += (e, o) => { tcs.TrySetResult(false); };

                        bool bResult = await tcs.Task;
                        if (!bResult)
                        {
                            var tb = new TextBlock();
                            tb.Text = "gif 로딩 실패";
                            tb.Foreground = new SolidColorBrush(Colors.Red);
                            grid.Children.Add(tb);
                        }
                        else
                        {
                            var image = new AnimatedImage();
                            panel.Items.Insert(i, image);

                            image.OnApplyTemplate();
                            image.Source = source;
                            image.Tap += image_Tap;                            
                            image.Tag = pic;
                        }
                    }

                    // 이상하게도 PictureDecoder.DecodeJpeg이 png까지 디코딩을 할 수가 있어서.. 그냥 쓰고 있다
                    else if (contentType.MediaType.Equals("image/jpg", StringComparison.CurrentCultureIgnoreCase) ||
                             contentType.MediaType.Equals("image/jpeg", StringComparison.CurrentCultureIgnoreCase) ||
                             contentType.MediaType.Equals("image/png", StringComparison.CurrentCultureIgnoreCase) || 
                             extension == ".jpg" || extension == ".png")
                    {
                        // 큰 이미지를 잘게 나눈다
                        var wbmps = await Task.Factory.StartNew(() => LoadImage(stream));

                        // grid의 위치를 알아내고
                        int i = panel.Items.IndexOf(grid) + 1;
                        foreach (var wbmp in wbmps)
                        {
                            var image = new Image();
                            image.Source = wbmp;
                            RegisterContextMenu(image, stream);
                            
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
                        RegisterContextMenu(image, stream); 
                        panel.Items.Insert(i, image);
                    }
                }

                grid.Children.Clear();
            }
            catch
            {
                status.Text = "[이미지 불러오기 실패]";
            }
   
        }

        private string GetExtension(HttpContentHeaders headers)
        {
            if (headers.ContentDisposition == null)
            {
                IEnumerable<string> values;

                if (!headers.TryGetValues("Content-Disposition", out values))
                    return string.Empty;

                foreach(var value in values)
                {
                    if (value.Contains(".gif")) return ".gif";
                    else if (value.Contains(".jpg")) return ".jpg";
                    else if (value.Contains(".png")) return ".png";
                }

                return string.Empty;
            }
            

            var fileName = headers.ContentDisposition.FileName;
            if (fileName == null) return string.Empty;
            if (fileName.Length < 4) return string.Empty;

            if ((fileName[0] == '"' && fileName[fileName.Length - 1] == '"') || 
                (fileName[0] == '\'' && fileName[fileName.Length - 1] == '\''))
                fileName = fileName.Substring(1, fileName.Length - 2);

            try
            {
                return System.IO.Path.GetExtension(fileName).ToLower();
            }
            catch
            {
                return string.Empty;
            }
        }

        void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem elem = sender as MenuItem;
            if (elem == null) return;

            Stream stream = elem.Tag as Stream;
            if (stream == null) return;

            stream.Seek(0, SeekOrigin.Begin);

            MediaLibrary library = new MediaLibrary();
            string name = string.Format("dcview_{0}_{1}", article.Board.ID, DateTime.Now.ToString("HHmmss"));
            var picture = library.SavePicture(name, stream);

            MessageBox.Show("사진이 저장되었습니다");
        }

        void RegisterContextMenu(FrameworkElement elem, Stream stream)
        {
            var menu = new ContextMenu();
            var saveMenuItem = new MenuItem() { Header = "사진 저장" };
            saveMenuItem.Tag = stream;
            saveMenuItem.Click += SaveMenuItem_Click;
            menu.Items.Add(saveMenuItem);
            ContextMenuService.SetContextMenu(elem, menu);
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

        private async void ShowArticleText(ArticleData data)
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
            title.Margin = new Thickness(6, 9, 6, 9);
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
                elem.Margin = new Thickness(9);
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

            var cmtStackPanel = new StackPanel();
            foreach (var cmt in data.Comments)
            {
                var commentView = new CommentView();
                commentView.DataContext = new CommentViewModel(cmt);
                
                foreach (var grid in HtmlElementConverter.GetUIElementFromString(cmt.Text, tapAction))
                {
                    grid.Margin = new Thickness(0);
                    commentView.Contents.Children.Add(grid);

                    if (grid is Grid && grid.Tag is Picture)
                        imgContainers.Add((Grid)grid);
                }

                cmtStackPanel.Children.Add(commentView);
            }

            var border = new Border()
            {
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = (Brush)Resources["PhoneSubtleBrush"],
                Margin = new Thickness(0, 12, 0, 0),
            };

            border.Child = cmtStackPanel;
            panel.Items.Add(border);
            

            // * 댓글을 달 수 있으면 ReplyTextBox 넣기
            if (article.CanWriteComment)
            {
                // ReplyTextBox를 새로 만듦
                replyTextBox = CreateReplyTextBox(panel);
                panel.Items.Add(replyTextBox);
            }

            
            bool bPassiveLoading = (bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] && imgContainers.Count != 0;

            // Contents에 panel을 추가.
            Contents.Children.Add(panel);

            // *스크롤을 처음으로 되돌림
            panel.ScrollIntoView(title);

            // * 수동읽기가 설정이 되어있으면 버튼을 활성화
            if (!bPassiveLoading)
            {
                loadImageButton.Visibility = Visibility.Collapsed;
                await LoadImagesAsync(panel, imgContainers);
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
            // 이전 글 안보이게 지우기
            Contents.Children.Clear();
            if (article == null) return;

            // 프로그레스 바 켬
            LoadingArticleTextProgressBar.IsIndeterminate = true;            

            try
            {
                ArticleData data = new ArticleData();

                bool result = await Task.Factory.StartNew( () => 
                {
                    string text = null;
                    if (!article.GetText(out text)) return false;

                    // ArticleData 생성                                        

                    // TODO: 현재는 첫번째 이후 코멘트를 가져올 방법이 없다 
                    data.Text = text;
                    data.CommentLister = article.GetCommentLister();

                    IEnumerable<IComment> newComments;
                    bool bEnded = data.CommentLister.Next(out newComments);

                    data.Comments.AddRange(newComments);

                    // Cache에 집어넣기
                    cachedData.Add(article, data);
                    return true;
                
                });

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

        private void VirtualizingStackPanel_Loaded(object sender, RoutedEventArgs e)
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
        }
    }
}
