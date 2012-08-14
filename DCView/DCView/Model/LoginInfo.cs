using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using MyApps.Common;
using System.Text;
using DCView.Util;

namespace DCView
{
    public class LoginInfo : INotifyPropertyChanged
    {
        public enum State
        {
            NotLogin,
            LoggingIn,
            LoggedIn,
        }

        public string ID
        {
            get { return id; }
            set { id = value; Notify("ID"); }
        }

        public string Password
        {
            get { return password; }
            set { password = value; Notify("Password"); }
        }

        public bool AutoLogin
        {
            get { return autoLogin; }
            set { autoLogin = value; Notify("AutoLogin"); }
        }        

        // State
        public State LoginState
        {
            get { return loginState; }
            private set { loginState = value; Notify("LoginState"); }
        }
            
        public bool Error 
        {
            get { return error; }
            private set { error = value; Notify("Error"); }
        }
        
        // 1. HasError
        public bool CanLogin
        {
            get { return id != null && id.Length != 0 && password != null && password.Length != 0; }
        }

        // 마지막으로 로그인한 시점 (로그인을 했는지 여부를 검사할때 쓰인다)
        public DateTime? LastLogin { get; set; }

        private string id = string.Empty;
        private string password = string.Empty;
        private bool autoLogin = false;
        private State loginState = State.NotLogin;
        private bool error = false;
        public event PropertyChangedEventHandler PropertyChanged;

        // Notify
        private void Notify(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public LoginInfo()
        {
            var isoSettings = IsolatedStorageSettings.ApplicationSettings;

            isoSettings.TryGetValue("dcview.autologin", out autoLogin);

            if (autoLogin)
            {
                isoSettings.TryGetValue("dcview.id", out id);
                isoSettings.TryGetValue("dcview.password", out password);
            }
        }        

        public void Save()
        {
            var isoSettings = IsolatedStorageSettings.ApplicationSettings;
            isoSettings["dcview.autologin"] = autoLogin;

            if (autoLogin)
            {
                isoSettings["dcview.id"] = id;
                isoSettings["dcview.password"] = password;
            }
            else
            {
                isoSettings.Remove("dcview.id");
                isoSettings.Remove("dcview.password");
            }

            isoSettings.Save();
        }

        DCViewWebClient client = null;

        // 로그인 중이라면 취소
        public void Cancel()
        {
            if (client != null)
                client.CancelAsync();
        }

        public void Login()
        {
            if (client != null)
                return;

            client = new DCViewWebClient();

            client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            client.Headers["Referer"] = "http://m.dcinside.com/login.php?r_url=%2F";

            string data = string.Format(
                "user_id={0}&user_pw={1}&r_url=%2F",
                HttpUtility.UrlEncode(ID),
                HttpUtility.UrlEncode(Password));

            client.UploadStringCompleted += LoginCompleted;
            client.UploadStringAsync(new Uri("http://dcid.dcinside.com/join/mobile_login_ok.php", UriKind.Absolute), "POST", data);
            LoginState = State.LoggingIn;            
        }

        private void LoginCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            client = null;

            if (e.Cancelled)
            {
                LoginState = State.NotLogin;
                return;
            }

            if (e.Error != null)
            {
                AutoLogin = false;
                Error = true;
                LoginState = State.NotLogin;
                return;
            }
            
            if (e.Result.IndexOf("parent.location.href='http://m.dcinside.com/'") == -1)            
            {
                LoginState = State.NotLogin;
                Error = true;
                AutoLogin = false;                
                return;
            }

            // 2주동안 지속
            LastLogin = DateTime.Now;
            LoginState = State.LoggedIn;
        }

        public void Logout()
        {
            LastLogin = null;
            LoginState = State.NotLogin;
            DCViewWebClient.ResetCookie(); // 쿠키값 리셋
        }

        public void Delete()
        {
            AutoLogin = false;
            ID = string.Empty;
            Password = string.Empty;
            Logout();

            Save();
        }
    }
}
