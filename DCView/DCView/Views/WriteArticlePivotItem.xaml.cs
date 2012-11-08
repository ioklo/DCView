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

        // 글쓰기 제출
        private async void Submit()
        {
            if (FormTitle.Text.Trim().Length == 0 || FormText.Text.Trim().Length == 0)
            {
                MessageBox.Show("제목/내용이 없습니다");
                return;
            }

            List<AttachmentStream> attachStream = new List<AttachmentStream>();

            // 짤 추가된것들 목록을 얻어온다
            foreach (var child in ImagesPanel.Children)
            {
                Button button = child as Button;
                if (button == null) continue;

                Image image = button.Content as Image;
                if (image == null) continue;

                PhotoResult result = image.Tag as PhotoResult;
                if (result == null) continue;

                string name = result.OriginalFileName;
                int last = result.OriginalFileName.LastIndexOf('\\');
                if (last != -1)
                    name = result.OriginalFileName.Substring(last + 1);

                attachStream.Add(new AttachmentStream() { Stream = result.ChosenPhoto, Filename = name });
            }

            
            submitButton.IsEnabled = false;
            WriteProgressBar.IsIndeterminate = true;

            try
            {               
                CancellationTokenSource cts = new CancellationTokenSource();

                string title = Title;
                string text = Text;

                bool result = await Task.Factory.StartNew ( () =>                    
                {
                    return board.WriteArticle(title, text, attachStream, cts.Token);
                }, cts.Token);

                // 성공했으면 
                if (result)
                {
                    // 글쓰기 창을 닫고..
                    viewArticlePage.RemoveWriteForm();                            

                    // 글 목록을 reload
                    viewArticlePage.RefreshArticleList();                        
                }                    
                else
                {
                    MessageBox.Show("글쓰기에 실패했습니다. 다시 시도해 보세요");
                }
            }
            catch
            {
                MessageBox.Show("글쓰기에 실패했습니다. 다시 시도해 보세요");            
            }
            finally
            {
                submitButton.IsEnabled = true;
                WriteProgressBar.IsIndeterminate = false;
            }                
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
                button.Height = 128;
                button.Width = 128;
                button.Padding = new Thickness(0);
                button.Click += ModifyImage;
                button.BorderThickness = new Thickness(1);

                ContextMenu menu = new ContextMenu();
                MenuItem removeMenuItem = new MenuItem();
                removeMenuItem.Header = "제거";
                removeMenuItem.Click += new RoutedEventHandler(removeMenuItem_Click);
                removeMenuItem.Tag = button;

                menu.Items.Add(removeMenuItem);

                ContextMenuService.SetContextMenu(button, menu);

                Image image = new Image();
                image.Source = new BitmapImage(new Uri(e1.OriginalFileName));
                image.Stretch = Stretch.UniformToFill;
                image.Tag = e1;
                button.Content = image;

                ImagesPanel.Children.Insert(ImagesPanel.Children.Count - 1, button);

                if (ImagesPanel.Children.Count >= 6)
                    AddImageButton.IsEnabled = false;
            };

            photoChooser.Show();
        }

        void removeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (elem == null) return;

            var uiElem = elem.Tag as UIElement;
            if (uiElem == null) return;

            ImagesPanel.Children.Remove(uiElem);
            if (ImagesPanel.Children.Count < 6)
                AddImageButton.IsEnabled = true;
        }

        void INotifyActivated.OnActivated()
        {
            viewArticlePage.ApplicationBar = appBar;
        }

        void submitButton_Click(object sender, EventArgs e)
        {
            if (App.Current.LoginInfo.LoginState != LoginInfo.State.LoggedIn)
            {
                viewArticlePage.ShowLoginDialog();
                return;
            }

            this.Focus();
            Submit();
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            viewArticlePage.RemoveWriteForm();
        }

        

    }
}
