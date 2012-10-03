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
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DCView
{
    public partial class WriteArticlePivotItem : PivotItem, INotifyActivated
    {
        ViewArticle viewArticlePage;        
        ApplicationBar appBar;
        ApplicationBarIconButton submitButton;

        public string Title { get { return FormTitle.Text; } }
        public string Text { get { return FormText.Text; } }

        bool bTitleModified = false;
        bool bTextModified = false;
        IBoard board = null;

        public WriteArticlePivotItem(ViewArticle page, IBoard board)
        {
            InitializeComponent();
            InitializeApplicationBar();

            this.DataContext = this;
            this.viewArticlePage = page;
            this.board = board;
        }

        private void InitializeApplicationBar()
        {
            submitButton = new ApplicationBarIconButton();
            submitButton.Text = "올리기";
            submitButton.IconUri = new Uri("/Data/appbar.check.rest.png", UriKind.Relative);
            submitButton.Click += new EventHandler(submitButton_Click);

            var cancelButton = new ApplicationBarIconButton();
            cancelButton.Text = "취소";
            cancelButton.IconUri = new Uri("/Data/appbar.stop.rest.png", UriKind.Relative);
            cancelButton.Click += new EventHandler(cancelButton_Click);

            appBar = new ApplicationBar();
            appBar.Buttons.Add(submitButton);
            appBar.Buttons.Add(cancelButton);
        }

        private void Submit()
        {
            if (!bTitleModified || !bTextModified || FormTitle.Text.Trim().Length == 0 || FormText.Text.Trim().Length == 0)
            {
                MessageBox.Show("제목/내용이 없습니다");
                return;
            }

            List<AttachmentStream> attachStream = new List<AttachmentStream>();

            // 짤 추가된것들 목록을 얻어온다
            foreach (Button button in ImagesPanel.Children)
            {
                if (button == null) continue;

                Image image = button.Content as Image;
                if (image == null) continue;

                PhotoResult result = image.Tag as PhotoResult;
                if (result == null) continue;

                attachStream.Add(new AttachmentStream() { Stream = result.ChosenPhoto, Filename = new FileInfo(result.OriginalFileName).Name });
            }

            CancellationTokenSource cts = new CancellationTokenSource();

            string title = Title;
            string text = Text;
            submitButton.IsEnabled = false;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // 성공했으면 
                    if (board.WriteArticle(title, text, attachStream, cts.Token))
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            // 글쓰기 창을 닫고..
                            viewArticlePage.RemoveWriteForm();

                            // 글 목록을 reload
                            viewArticlePage.RefreshArticleList();
                        });
                        return;
                    }
                    
                }
                catch
                {
                                        
                }

                viewArticlePage.ShowErrorMessage("글쓰기에 실패했습니다. 다시 시도해 보세요");
                Dispatcher.BeginInvoke(() => { submitButton.IsEnabled = true; });
                

            }, cts.Token);            
        }        

        private void ModifyImage(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            PhotoChooserTask photoChooser = new PhotoChooserTask();
            photoChooser.Completed += (o, e1) =>
            {
                if (e1.Error != null || e1.TaskResult == TaskResult.Cancel)
                    return;
                
                Image image = new Image();
                image.Source = new BitmapImage(new Uri(e1.OriginalFileName));
                image.Stretch = Stretch.UniformToFill;
                image.Tag = e1;
                button.Content = image;
            };
            photoChooser.Show();
        }


        private void AddImage(object sender, RoutedEventArgs e)
        {
            PhotoChooserTask photoChooser = new PhotoChooserTask();
            photoChooser.ShowCamera = true;
            photoChooser.Completed += (o, e1) =>
            {
                if (e1.Error != null || e1.TaskResult == TaskResult.Cancel)
                    return;

                Button button = new Button();
                button.Height = 100;
                button.Width = 100;
                button.Padding = new Thickness(0);
                button.Margin = new Thickness(10);
                button.Click += ModifyImage;

                ContextMenu menu = new ContextMenu();
                menu.Items.Add(new MenuItem() { Header = "제거" });

                ContextMenuService.SetContextMenu(button, menu);

                Image image = new Image();
                image.Source = new BitmapImage(new Uri(e1.OriginalFileName));
                image.Stretch = Stretch.UniformToFill;
                image.Tag = e1;
                button.Content = image;

                ImagesPanel.Children.Insert(ImagesPanel.Children.Count - 1, button);
            };

            photoChooser.Show();
        }

        void INotifyActivated.OnActivated()
        {
            viewArticlePage.ApplicationBar = appBar;
        }

        private void FormTitle_GotFocus(object sender, RoutedEventArgs e)
        {
            if (bTitleModified) return;
            FormTitle.Text = "";
            bTitleModified = true;
        }

        private void FormTitle_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormTitle.Text.Length == 0)
            {
                bTitleModified = false;
                FormTitle.Text = "제목";
            }
        }

        private void FormText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FormText.Text.Length == 0)
            {
                bTextModified = false;
                FormText.Text = "내용";
            }
        }       

        
        private void FormText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (bTextModified) return;
            FormText.Text = "";
            bTextModified = true;
        }

        void submitButton_Click(object sender, EventArgs e)
        {
            if (App.Current.LoginInfo.LoginState != LoginInfo.State.LoggedIn)
            {
                viewArticlePage.ShowLoginDialog();
                return;
            }

            Submit();
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            viewArticlePage.RemoveWriteForm();
        }

        

    }
}
