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

namespace MyApps.DCView
{
    public partial class WriteArticle : PhoneApplicationPage
    {
        // Object
        ArticleWriter writer = null;
        ApplicationBarIconButton writeButton = null;

        public WriteArticle()
        {
            InitializeComponent();

            var appBar = new ApplicationBar()
            {
                IsVisible = true,
                IsMenuEnabled = false
            };

            writeButton = new ApplicationBarIconButton(new Uri("/appbar.check.rest.png", UriKind.Relative));
            writeButton.Text = "전송";
            writeButton.Click += Submit_Click;
            appBar.Buttons.Add(writeButton);

            ApplicationBar = appBar;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (writer != null)
            {
                writer.PropertyChanged -= writer_PropertyChanged;
                State["title"] = writer.Title;
                State["text"] = writer.Text;
            }
        }

        // 페이지로 들어올 때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {            
            base.OnNavigatedTo(e);
            
            if (writer != null)
            {
                writer.PropertyChanged += writer_PropertyChanged;
                return;
            }

            string id;
            if( !NavigationContext.QueryString.TryGetValue("id", out id))
            {                
                return;
            }

            writer = new ArticleWriter(id);
            writer.Refresh();
            writer.PropertyChanged += writer_PropertyChanged;            

            if (State.ContainsKey("title"))
                writer.Title = State["title"] as string;

            if (State.ContainsKey("text"))
                writer.Text= State["text"] as string;

            DataContext = writer;            
        }

        void writer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "WriterState")
            {
                writeButton.IsEnabled = (writer.WriterState == ArticleWriter.State.Ready);

                if (writer.WriterState == ArticleWriter.State.SendCompleted)
                {
                    NavigationService.Navigate(new Uri(
                        string.Format("/Views/ViewArticle.xaml?id={0}&name={1}&site={2}&pcsite={3}",
                        NavigationContext.QueryString["id"],
                        NavigationContext.QueryString["name"],
                        NavigationContext.QueryString["site"],
                        NavigationContext.QueryString["pcsite"]), UriKind.Relative));
                }
            }
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            if (WriteTitle.Text.Trim().Length == 0 || WriteText.Text.Trim().Length == 0)
            {
                MessageBox.Show("제목과 본문에 내용을 입력해야 글을 올릴 수 있습니다");
                return;
            }

            writer.Send();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            WriteTitle.Focus();
        }
    }
}