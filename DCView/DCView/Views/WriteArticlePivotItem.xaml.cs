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

namespace DCView
{
    public partial class WriteArticlePivotItem : PivotItem, INotifyActivated
    {
        ViewArticle viewArticlePage;        
        ApplicationBar appBar;

        public string Title { get { return FormTitle.Text; } }
        public string Text { get { return FormText.Text; } }

        bool bTitleModified = false;
        bool bTextModified = false;

        public WriteArticlePivotItem(ViewArticle page)
        {
            InitializeComponent();
            InitializeApplicationBar();

            this.DataContext = this;
            this.viewArticlePage = page;
        }

        private void InitializeApplicationBar()
        {
            var submitButton = new ApplicationBarIconButton();
            submitButton.Text = "올리기";
            submitButton.IconUri = new Uri("/appbar.check.rest.png", UriKind.Relative);
            submitButton.Click += new EventHandler(submitButton_Click);

            var cancelButton = new ApplicationBarIconButton();
            cancelButton.Text = "취소";
            cancelButton.IconUri = new Uri("/appbar.stop.rest.png", UriKind.Relative);
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
            Submit();
        }

        void cancelButton_Click(object sender, EventArgs e)
        {
            viewArticlePage.RemoveWriteForm();
        }

        

    }
}
