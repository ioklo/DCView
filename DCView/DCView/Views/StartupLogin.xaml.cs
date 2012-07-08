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
using System.Windows.Navigation;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using MyApps.Common;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace MyApps.DCView
{
    public partial class StartupLogin : PhoneApplicationPage
    {
        public StartupLogin()
        {
            InitializeComponent();            
        }

        // 이 페이지로 들어왔으면 로그인을 요청하는 것
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            DataContext = LoginInfo.Instance;

            // 바인딩 설정
            LoginInfo.Instance.PropertyChanged += new PropertyChangedEventHandler(LoginInfoPropertyChanged);

            if (LoginInfo.Instance.AutoLogin && LoginInfo.Instance.CanLogin)
                LoginInfo.Instance.Login();
        }

        // 페이지에서 나갈 때         
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // 바인딩 설정
            LoginInfo.Instance.PropertyChanged -= new PropertyChangedEventHandler(LoginInfoPropertyChanged);
            
            // 세팅을 저장한다
            LoginInfo.Instance.Save();
        }

        private void LoginInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Error")
            {
                MessageBox.Show("로그인 에러");
            }
            else if (e.PropertyName == "LoginState")
            {
                switch (LoginInfo.Instance.LoginState)
                {
                    case LoginInfo.State.NotLogin:
                        SubmitButton.Content = "로그인";
                        SubmitButton.IsEnabled = true;
                        DiscardButton.IsEnabled = true;
                        ProgressBar.IsIndeterminate = false;
                        break;

                    case LoginInfo.State.LoggingIn:
                        SubmitButton.Content = "로그인 중";
                        SubmitButton.IsEnabled = false;
                        DiscardButton.IsEnabled = false;
                        ProgressBar.IsIndeterminate = true;
                        break;

                    case LoginInfo.State.LoggedIn:
                        SubmitButton.Content = "로그아웃";
                        SubmitButton.IsEnabled = true;
                        DiscardButton.IsEnabled = false;
                        ProgressBar.IsIndeterminate = false;

                        NavigationService.Navigate(new Uri("/Views/SelectGallery.xaml", UriKind.Relative));
                        break;
                }
            }
        }

        // 로그인 버튼을 눌렀을 때
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            switch (LoginInfo.Instance.LoginState)
            {
                case LoginInfo.State.NotLogin:
                    LoginInfo.Instance.Login();
                    break;

                case LoginInfo.State.LoggingIn:
                    LoginInfo.Instance.Cancel();
                    break;

                case LoginInfo.State.LoggedIn:
                    LoginInfo.Instance.Logout();
                    break;
            }
        }

        // 비회원으로 들어가기
        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Views/SelectGallery.xaml", UriKind.Relative));
        }
    }
}