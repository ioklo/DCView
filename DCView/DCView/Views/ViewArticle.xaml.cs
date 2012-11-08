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
using DCView.Util;

namespace DCView
{
    // 글 목록을 보고 읽는 곳
    public partial class ViewArticle : PhoneApplicationPage
    {
        bool bInitialized = false;

        ViewArticleListPivotItem viewArticleListPivotItem = null;
        ViewArticleListPivotItem viewSearchArticleListPivotItem = null; // 검색 결과
        ViewArticleTextPivotItem viewArticleTextPivotItem = null;
        WriteArticlePivotItem writeArticlePivotItem = null;

        Thickness pivotMargin = new Thickness(6, 14, 6, 0);
        
        // 생성자
        public ViewArticle()
        {
            InitializeComponent();
        }

        // 오버라이드 함수들
        // 백키를 누르면 바로 이전것으로 돌아가지 않고, 목록에 있을때만 뒤로 되돌아간다
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (MainPivot.SelectedIndex != 0)
            {
                MainPivot.SelectedIndex = 0;
                e.Cancel = true;
            }
        }

        // 페이지에서 나가려고 할 때
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // LoginInfo 바인딩 제거
            App.Current.LoginInfo.PropertyChanged -= LoginInfo_PropertyChanged;
        }

        void EnableLoginForm(bool bEnabled)
        {
            StackPanel panel = LoginForm;
            foreach(var child in panel.Children)
            {
                Control ctrl = child as Control;
                if (ctrl == null) continue;
                ctrl.IsEnabled = bEnabled;   
            }
        }

        void LoginInfo_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LoginInfo info = sender as LoginInfo;
            if (info == null) return;

            switch(e.PropertyName)
            {
                case "LoginState":

                    switch(info.LoginState)
                    {
                        case LoginInfo.State.LoggedIn:
                            LoginStatus.Text = info.ID + " 로그인";
                            EnableLoginForm(false);
                            LoginSubmitButton.Content = "로그아웃";
                            HideLoginDialog();
                            break;

                        case LoginInfo.State.LoggingIn:
                            LoginStatus.Text = "로그인 중...";
                            EnableLoginForm(false);
                            LoginSubmitButton.Content = "로그인 취소";
                            break;

                        case LoginInfo.State.NotLoggedIn:
                            LoginStatus.Text = "비회원";
                            EnableLoginForm(true);
                            LoginSubmitButton.Content = "로그인";
                            break;
                    }
                    break;
            }
        }

        // 이 페이지로 들어오려고 할 때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Initialize();
            LoginForm.DataContext = App.Current.LoginInfo;
            LoginInfo_PropertyChanged(App.Current.LoginInfo, new PropertyChangedEventArgs("LoginState"));

            App.Current.LoginInfo.PropertyChanged += LoginInfo_PropertyChanged;
        }

        private void Initialize()
        {            
            if (bInitialized) return;
            
            string siteID = NavigationContext.QueryString["siteID"];
            string boardID = NavigationContext.QueryString["boardID"];
            string boardName = NavigationContext.QueryString["boardName"];

            IBoard board = App.Current.SiteManager.GetBoard(siteID, boardID, boardName);

            viewArticleListPivotItem = new ViewArticleListPivotItem(this, board);
            
            GalleryTitle.Text = boardName + " 갤러리"; // 왼쪽 상단 갤러리 이름 적기
            MainPivot.Items.Clear();
            MainPivot.Items.Add(viewArticleListPivotItem);

            viewArticleListPivotItem.RefreshArticleList(); // 리스트 갱신
            
            bInitialized = true;
        }
        
        // 하는 행동
        private void PivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                UpdatePivotItem(item);
            }
        }

        private void UpdatePivotItem(object item)
        {
            INotifyActivated obj = item as INotifyActivated;
            if (obj == null) return;

            obj.OnActivated();
        }

        public void Search(IBoard board, string text, SearchType searchType)
        {
            // 새로운 searchBoard 를 만든다
            viewSearchArticleListPivotItem = new ViewSearchArticleListPivotItem(this, board, text, searchType);

            // 목록창은 항상 0번에
            MainPivot.Items[0] = viewSearchArticleListPivotItem;
            MainPivot.SelectedIndex = 0;
            UpdatePivotItem(viewSearchArticleListPivotItem);
            
            viewSearchArticleListPivotItem.RefreshArticleList();
            this.Focus();
        }

        public void SelectArticle(IArticle article)
        {
            if (viewArticleTextPivotItem == null)
                viewArticleTextPivotItem = new ViewArticleTextPivotItem(this);


            // '내용' 탭이 없다면 이제 추가해 준다            
            if (!MainPivot.Items.Contains(viewArticleTextPivotItem))
                MainPivot.Items.Insert(1, viewArticleTextPivotItem);

            // '내용' 탭으로 화면 전환
            MainPivot.SelectedItem = viewArticleTextPivotItem;
            viewArticleTextPivotItem.SetArticle(article);
        }

        public void ShowWriteForm(IBoard board)
        {
            // 글쓰기 폼 보이기
            if (!MainPivot.Items.Contains(writeArticlePivotItem))
            {
                writeArticlePivotItem = new WriteArticlePivotItem(this, board);
                MainPivot.Items.Add(writeArticlePivotItem);
            }

            MainPivot.SelectedItem = writeArticlePivotItem;
        }

        public void RemoveWriteForm()
        {
            if (writeArticlePivotItem == null)
                return;

            MainPivot.Items.Remove(writeArticlePivotItem);
            MainPivot.SelectedIndex = 0;

            writeArticlePivotItem = null;
        }

        public void ShowArticleList()
        {            
            MainPivot.Items[0] = viewArticleListPivotItem;
            MainPivot.SelectedIndex = 0;

            UpdatePivotItem(viewArticleListPivotItem);            
        }

        public void RefreshArticleList()
        {
            viewArticleListPivotItem.RefreshArticleList(); // 리스트 갱신
        }

        public void ShowLoginDialog()
        {
            MainPivot.IsEnabled = false;
            ApplicationBar.IsMenuEnabled = false;

            LoginPanel.Visibility = Visibility.Visible;
        }

        public void HideLoginDialog()
        {
            MainPivot.IsEnabled = true;
            ApplicationBar.IsMenuEnabled = true;

            LoginPanel.Visibility = Visibility.Collapsed;
        }

        private void LoginStatus_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowLoginDialog();            
        }

        private void LoginCloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideLoginDialog();
        }

        CancellationTokenSource loginCancelTokenSource;

        private void LoginSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var loginInfo = App.Current.LoginInfo;

            switch (loginInfo.LoginState)
            {
                // 로그인 중이라면 취소,
                case LoginInfo.State.LoggingIn:
                    if (loginCancelTokenSource != null)
                    {
                        loginCancelTokenSource.Cancel();
                        loginCancelTokenSource = null;
                    }
                    break;

                case LoginInfo.State.NotLoggedIn:
                    {
                        loginCancelTokenSource = new CancellationTokenSource();
                        loginInfo.Login(loginCancelTokenSource.Token);
                        break;
                    }

                case LoginInfo.State.LoggedIn:
                    {
                        loginInfo.Logout();
                    }
                    break;
            }
        }


    }
}