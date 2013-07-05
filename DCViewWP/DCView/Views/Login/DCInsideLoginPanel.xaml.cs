using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace DCView
{
    public partial class DCInsideLoginPanel : UserControl
    {
        // Credential은 이것들의 상태를 관리한다 

        DCInsideCredential credential;
        ViewArticle parent;

        public DCInsideLoginPanel(DCInsideCredential credential, ViewArticle parent)
        {
            InitializeComponent();
            this.credential = credential;
            this.parent = parent;

            // Status가 바뀔때 컨트롤의 동작들도 바뀐다

            // 컨트롤 값 채우기
            LoginIDTextBox.Text = credential.MemberID;
            LoginPWTextBox.Password = credential.MemberPassword;
            SaveLoginInfoCheckBox.IsChecked = credential.SaveLoginInfo;
            AutoLoginCheckBox.IsChecked = credential.AutoLogin;            

            credential.OnStatusChanged += OnStatusChanged;
            UpdateLoginSubmitButton();
        }

        private void UpdateLoginSubmitButton()
        {
            if (credential.Status == DCInsideCredential.LoginStatus.MemberConnecting)
                LoginSubmitButton.Content = "로그인 취소"; // 취소 버튼 

            else if (credential.Status == DCInsideCredential.LoginStatus.MemberLogin)
                LoginSubmitButton.Content = "로그아웃";

            else
                LoginSubmitButton.Content = "로그인";   
        }

        private void OnStatusChanged(object sender, EventArgs e)
        {
            UpdateLoginSubmitButton();
        }

        // 로그인 버튼을 눌렀을 때만 저장한다 
        // 닫기 버튼을 눌렀다 -> 감추는 건데 별로 필요는.. 
        // 저쪽 

        CancellationTokenSource loginCancelTokenSource;

        // 로그인 버튼을 눌렀을 때
        private async void LoginSubmitButton_Click(object sender, RoutedEventArgs e)
        {
            switch (credential.Status)
            {
                // 로그인 중이라면 취소,
                case DCInsideCredential.LoginStatus.MemberConnecting:
                    if (loginCancelTokenSource != null)
                    {
                        loginCancelTokenSource.Cancel();
                        loginCancelTokenSource = null;
                    }
                    break;

                case DCInsideCredential.LoginStatus.MemberNoLogin:                
                    {
                        loginCancelTokenSource = new CancellationTokenSource();

                        bool result = await credential.Login(
                            LoginIDTextBox.Text,
                            LoginPWTextBox.Password,
                            SaveLoginInfoCheckBox.IsChecked,
                            AutoLoginCheckBox.IsChecked, loginCancelTokenSource.Token);

                        if (result)
                        {
                            parent.HideLoginDialog();

                            // 로긴 성공했으면 credential 정보도 저장
                        }
                        
                        break;
                    }

                case DCInsideCredential.LoginStatus.MemberLogin:
                    {
                        credential.Logout();
                    }
                    break;
            }
        }

        private void Close()
        {
            parent.HideLoginDialog();
        }

        private void CloseCredentialPanelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
