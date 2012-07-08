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
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using MyApps.Common;
using System.IO.IsolatedStorage;
using System.Windows.Data;

namespace MyApps.DCView
{
    // 글 목록을 보고 읽는 곳
    public partial class ViewArticle : PhoneApplicationPage
    {
        // TEMP
        //class ArticleTextAdapter
        //{
        //    public class ItemCollector
        //    {
        //        ViewArticle va = null;

        //        public ItemCollector(ViewArticle article)
        //        {
        //            va = article;
        //        }

        //        public int Count { get { return va.ArticleTextPanel.Children.Count; } }

        //        public void Clear()
        //        {
        //            va.ArticleTextPanel.Children.Clear();
        //        }
        //        public void Remove(UIElement obj)
        //        {
        //            va.ArticleTextPanel.Children.Remove(obj);
        //        }
        //        public void Add(UIElement obj)
        //        {
        //            va.ArticleTextPanel.Children.Add(obj);
        //        }

        //        public void Insert(int i, UIElement obj)
        //        {
        //            va.ArticleTextPanel.Children.Insert(i, obj);
        //        }
        //    }

        //    public ItemCollector Items { get; private set; }

        //    public void InvalidateArrange()
        //    {
        //        // va.ArticleTextPanel.InvalidateArrange();
        //    }

        //    public void ScrollIntoView(UIElement element)
        //    {
        //    }


        //    public ArticleTextAdapter(ViewArticle article)
        //    {
        //        Items = new ItemCollector(article);
        //    }
        //}
        //ArticleTextAdapter ArticleText = null;

        // TEMP
        

        // 데이터.. articles
        // Tombstoning 당하면 사라져야 정상인걸까? => 그렇다.. 아무것도 없는 처음 상태로 되돌린다
        ArticleManager archive = null;
        LoginInfo loginInfo = LoginInfo.Instance;

        // appBar        
        ApplicationBarIconButton appbarWrite;
        ApplicationBarIconButton appbarRefresh;
        ApplicationBarIconButton appbarCommentSubmit;

        // 댓글쓰기 관련
        TextBox curReplyTextBox = null;

        private void InitializeAppbar()
        {
            // AppBar
            ApplicationBar appBar = new ApplicationBar();
            appBar.IsMenuEnabled = true;
            appBar.IsVisible = true;
            
            appbarWrite = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.edit.rest.png", UriKind.Relative),
                Text = "글쓰기"
            };
            appbarWrite.Click += appbarWrite_Click;

            appbarRefresh = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.refresh.rest.png", UriKind.Relative),
                Text = "새로고침"
            };
            appbarRefresh.Click += appbarRefresh_Click;

            appbarCommentSubmit = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/appbar.check.rest.png", UriKind.Relative),
                Text = "전송"
            };            
            appbarCommentSubmit.Click += new EventHandler(appbarCommentSubmit_Click);

            var menuItem1 = new ApplicationBarMenuItem();
            menuItem1.Text = "웹브라우저로 보기";
            menuItem1.Click += appbarWebbrowser_Click;
            appBar.MenuItems.Add(menuItem1);

            this.ApplicationBar = appBar;
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
            ApplicationBar.Buttons.Clear();

            if (LoginInfo.Instance.LoginState == LoginInfo.State.LoggedIn)
                ApplicationBar.Buttons.Add(appbarWrite);

            ApplicationBar.Buttons.Add(appbarRefresh);

            // 초기화 되면 archive가 없어진다
            if (archive != null)
            {
                return;
            }
       
            // archive 설정
            string id, name, site, pcsite;

            if (!NavigationContext.QueryString.TryGetValue("id", out id) ||
                !NavigationContext.QueryString.TryGetValue("name", out name) ||
                !NavigationContext.QueryString.TryGetValue("site", out site) ||
                !NavigationContext.QueryString.TryGetValue("pcsite", out pcsite)
                )
            {
                NavigationService.GoBack();
                return;
            }

            // 다음글 버튼
            var NextPage = new Button()
            {
                Content = "다음글",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            NextPage.SetBinding(Button.IsEnabledProperty, new Binding("IsLoadingArticleList") { Converter = new NegateBoolConverter() });
            NextPage.Click += NextPage_Click;
            
            archive = new ArticleManager(id, site, pcsite);
            archive.ArticleAdded += (article) => { ArticleList.Items.Insert(ArticleList.Items.Count - 1, article); };
            archive.ArticleCleared += () => 
            {
                ArticleList.Items.Clear();
                ArticleList.Items.Add(NextPage);
            };
            
            archive.Refresh();

            DataContext = archive;

            // 왼쪽 상단 갤러리
            PivotMain.Title = name + " 갤러리";
        }
        
        // 하는 행동
        private void Refresh()
        {
            // 글 목록을 지우고.. 
            ArticleList.Items.Clear();
            archive.Refresh();            
        }

        private void RefreshArticleText(Article article)
        {
            // 이전 글 안보이게 지우기
            ArticleText.Children.Clear();

            archive.GetArticleText(article,
                    () => { UpdateArticleText(article); },
                    () => { MessageBox.Show("글을 읽어들이는데 실패했습니다. 다시 시도해보세요"); });
        }
        private void UpdateArticleText(Article article)
        {
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


            if ((bool)IsolatedStorageSettings.ApplicationSettings["DCView.passive_loadimg"] && article.Images.Count != 0)
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
                    button.Content = string.Format("로딩중 {0}/{1}", 0, article.Images.Count);
                    button.IsEnabled = false;
                    int failedCount = 0;
                    int count = 0;
                    
                    foreach (Uri uri in article.Images)
                    {
                        Uri curUri = uri;

                        var image = new Image()
                        {
                            Source = new BitmapImage(uri),
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

                            if( failedCount != 0 )
                                button.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", count, article.Images.Count, failedCount);
                            else
                                button.Content = string.Format("불러오기 {0}/{1}", count, article.Images.Count, failedCount);

                            if (failedCount == 0 && count == article.Images.Count)
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

                            button.Content = string.Format("불러오기 {0}/{1}, 실패 {2}", 0, article.Images.Count, failedCount);
                        };

                        ArticleText.Children.Insert(imageEmbedIndex, image);

                        imageEmbedIndex++;
                    }
                };

                ArticleText.Children.Add(button);
            }
            else 
            {
                foreach (Uri uri in article.Images)
                {
                    var image = new Image()
                    {
                        Source = new BitmapImage(uri),
                        Margin = new Thickness(3),
                    };

                    image.Tap += (o1, e1) =>
                    {
                        WebBrowserTask task = new WebBrowserTask();
                        task.Uri = uri;
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

            foreach (var textBlock in GetTextBlocksFromString(article.Text))
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
                cmtName.Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style;
                cmtName.FontWeight = FontWeights.Bold;
                cmtName.Margin = new Thickness(0, 12, 0, 3);
                ArticleText.Children.Add(cmtName);

                /*
                var cmtText = new TextBlock();
                cmtText.Text = cmt.Text;
                cmtText.TextWrapping = TextWrapping.Wrap;
                cmtText.Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style;
                cmtText.Margin = new Thickness(0, 3, 0, 24);
                */

                foreach(var blocks in MakeTextBlocks(cmt.Text))
                    ArticleText.Children.Add(blocks);
            }

            // var cookies = WebClientEx.CookieContainer.GetCookies(new Uri("http://gall.dcinside.com"));
            // if (cookies["PHPSESSID"] != null)
            // 댓글 쓰기 버튼 누르기 -> 로그인 -> 로그인 성공 -> 댓글을 남기는 작업 -> 원래 페이지로 돌아와서 페이지 업데이트            
            if (loginInfo.LoginState == LoginInfo.State.LoggedIn)
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
                        if (LoginInfo.Instance.LoginState == LoginInfo.State.LoggedIn)
                            ApplicationBar.Buttons.Remove(appbarWrite);

                        if( !ApplicationBar.Buttons.Contains(appbarCommentSubmit))
                            ApplicationBar.Buttons.Add(appbarCommentSubmit);
                    };

                replyText.LostFocus += (o1, e1) =>
                    {
                        curReplyTextBox = null;
                        // 글쓰기 버튼 추가, 전송 버튼 제거
                        if (LoginInfo.Instance.LoginState == LoginInfo.State.LoggedIn)
                            if( !ApplicationBar.Buttons.Contains(appbarWrite))
                            ApplicationBar.Buttons.Insert(0, appbarWrite);

                        ApplicationBar.Buttons.Remove(appbarCommentSubmit);
                    };

                ArticleText.Children.Add(replyText);
               
                // 자동으로 늘어나는 replyTex

                // reply_Click                
            }

            ArticleTextScroll.ScrollToVerticalOffset(0);
        }

        private static StringHtmlEntityConverter stringHtmlConverter = new StringHtmlEntityConverter();
        private static Regex urlRegex = new Regex(@"((https?|ftp|gopher|telnet|file|notes|ms-help):((//)|(\\\\))+[\w\d:#@%/;$()~_?\+-=\\\.&]*)");

        class Splitted
        {
            public string Content {get; set;}
            public bool IsUrl {get; set;}
        }

        private IEnumerable<Splitted> SplitUrl(string input)
        {
            int cur = 0;

            Match match = urlRegex.Match(input);

            while (match.Success)
            {
                yield return new Splitted() { Content = input.Substring(cur, match.Index - cur), IsUrl = false };

                yield return new Splitted() { Content = match.Value, IsUrl = true };

                cur = match.Index + match.Length;
                match = urlRegex.Match(input, cur);
            }

            if (cur < input.Length)
                yield return new Splitted() { Content = input.Substring(cur), IsUrl = false };
        }

        private IEnumerable<FrameworkElement> MakeTextBlocks(string input)
        {
            input = HttpUtility.HtmlDecode(input);

            foreach (var s in SplitUrl(input))
            {
                var splitted = s;

                // url임 
                if (splitted.IsUrl)
                {
                    var textBlock = new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 1.2,
                        Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style,
                        Margin = new Thickness(0, 3, 0, 3),                        
                        Foreground = new SolidColorBrush(Colors.Blue),                        
                    };

                    var underline = new Underline();
                    underline.Inlines.Add(new Run() { Text = splitted.Content });
                    textBlock.Inlines.Add(underline);

                    textBlock.Tap += (o1, e1) =>
                        {
                            WebBrowserTask task = new WebBrowserTask()
                            {
                                Uri = new Uri(splitted.Content, UriKind.Absolute)
                            };

                            task.Show();
                        };

                    yield return textBlock;                    
                }
                else
                {
                    // 지금까지 내용을 전부 flush 
                    var textBlock = new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 1.2,
                        Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style,
                        Margin = new Thickness(0, 3, 0, 3),
                        Text = splitted.Content,
                    };

                    yield return textBlock;
                }
            }
        }
        
        private IEnumerable<UIElement> GetTextBlocksFromString(string input)
        {
            StringBuilder curPlainString = new StringBuilder();

            int pDepth = 0;            

            foreach (IHtmlEntity entity in stringHtmlConverter.Convert(input))
            {
                if (entity is PlainString)
                {
                    // 바로 출력하지는 않고 
                    PlainString plainString = (PlainString)entity;
                    curPlainString.Append(plainString.Content);
                }

                if (entity is Tag)
                {
                    Tag tag = (Tag)entity;

                    if (tag.Name.Equals("br"))
                    {
                        curPlainString.AppendLine();
                        continue;
                    }

                    // p는 0에서 1일때는 반응 안함
                    // 1에서 0으로 내려올때 

                    if (tag.Name == "p")
                    {
                        if (tag.Kind == Common.Tag.TagKind.Open)
                            pDepth++;
                        else if (tag.Kind == Common.Tag.TagKind.Close)
                            pDepth--;

                        if ((tag.Kind == Common.Tag.TagKind.Open && pDepth > 1) ||
                                (tag.Kind == Common.Tag.TagKind.Close && pDepth == 0) ||
                                (tag.Kind == Common.Tag.TagKind.OpenAndClose))
                        {
                            foreach (var obj in MakeTextBlocks(curPlainString.ToString()))
                                yield return obj;
                            curPlainString.Clear();
                            continue;
                        }                                
                    }
                    
                    if (tag.Name == "div")
                    {
                        if (curPlainString.Length != 0)
                        {
                            foreach (var obj in MakeTextBlocks(curPlainString.ToString()))
                                yield return obj;
                            curPlainString.Clear();                            
                        }
                        continue;
                    }
                    
                    // 이미지 처리.. 몰라 ㅋ
                    if (tag.Name.Equals("img", StringComparison.CurrentCultureIgnoreCase))
                    {
                    }
                }                
            }

            if( curPlainString.Length != 0)
                foreach (var obj in MakeTextBlocks(curPlainString.ToString()))
                    yield return obj;            

            // 1. <br> <br />은 \n으로 바꿈
            // 2. <p> </p>는 하나의 paragraph로 처리
            // 3. <div>도 마찬가지..
            // 4. <img src=는 image로 바꿈;
            // 5. <a>

            //string text = Regex.Replace(input, "\\s+", " ");
            //text = Regex.Replace(text, "(<br[^>]*>)|(<br[^/]*/>)", "\n", RegexOptions.IgnoreCase);

            //// p를 만나면 거기서 끊는다
            //foreach (var par in Regex.Split(text, "(<p[^>]*>)|(<div[^>]*>)", RegexOptions.IgnoreCase))
            //{               

            //    yield return textBlock;
            //}                    
        }

        // 이벤트 핸들러        
        private void ArticleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Article article = ArticleList.SelectedItem as Article;
            if (article == null) return;

            if (!PivotMain.Items.Contains(PivotArticle))
                PivotMain.Items.Add(PivotArticle);

            // 글이 없다면
            if (article.Text == null)
                RefreshArticleText(article);
            else
                UpdateArticleText(article);            
                
            PivotMain.SelectedItem = PivotArticle;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            archive.GetNextArticles();            
        }

        // 답장 버튼 클릭
        private bool CommentSubmit(string text)
        {
            Article article = ArticleList.SelectedItem as Article;
            if (article == null) return false;

            if (text.Trim() == string.Empty)
                return false;

            archive.Reply(article, text,
                () => { RefreshArticleText(article); },
                () => { MessageBox.Show("댓글달기에 실패했습니다 다시 시도해보세요"); });

            return true;
        }

        private void appbarRefresh_Click(object sender, EventArgs e)
        {
            if (PivotMain.SelectedItem == PivotList)
                Refresh();
            else
            {
                Article article = ArticleList.SelectedItem as Article;
                if (article == null) return;

                RefreshArticleText(article);
            }

        }
        private void appbarWebbrowser_Click(object sender, EventArgs e)
        {
            var wbTask = new WebBrowserTask();

            if (PivotMain.SelectedItem == PivotList)
                wbTask.Uri = new Uri(string.Format("http://{0}/list.php?id={1}", archive.PCSite, archive.ID), UriKind.Absolute);
            else
            {
                Article article = ArticleList.SelectedItem as Article;
                if (article == null) return;

                wbTask.Uri = new Uri(string.Format("http://{0}/list.php?id={1}&no={2}", archive.PCSite, archive.ID, article.ID), UriKind.Absolute);
            }

            wbTask.Show();
        }

        private void appbarWrite_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri(string.Format("/Views/WriteArticle.xaml?id={0}&name={1}&site={2}&pcsite={3}",
                archive.ID, NavigationContext.QueryString["name"], archive.Site, archive.PCSite), UriKind.Relative));
        }

        void appbarCommentSubmit_Click(object sender, EventArgs e)
        {
            if (curReplyTextBox == null)
                return;

            if (CommentSubmit(curReplyTextBox.Text))
            {
                // 일단 제출했으면 
                appbarCommentSubmit.IsEnabled = false;
            }
        }

        private void PivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PivotMain.SelectedItem == PivotList)
                ArticleList.SelectedItem = null;
        }

        
        private void ArticleList_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            this.Focus();
        }
        
    }
}