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
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;

namespace DCView
{
    public class LoginInfo : INotifyPropertyChanged
    {
        public enum State
        {
            NotLoggedIn,
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

        public bool SaveLoginInfo
        {
            get { return saveLoginInfo; }
            set 
            {
                if (saveLoginInfo == value)
                    return;

                saveLoginInfo = value;
                if (saveLoginInfo == false)
                    AutoLogin = false;                
                Notify("SaveLoginInfo"); 
            }
        }

        public bool AutoLogin
        {
            get { return autoLogin; }
            set 
            {
                if (autoLogin == value) return;
                autoLogin = value; 
                Notify("AutoLogin"); 
            }
        }        

        // State
        public State LoginState
        {
            get { return loginState; }
            private set { loginState = value; Notify("LoginState"); }
        }

        private Dispatcher dispatcher = Deployment.Current.Dispatcher;
        private string id = string.Empty;
        private string password = string.Empty;
        private bool autoLogin = false;
        private bool saveLoginInfo = true;
        private State loginState = State.NotLoggedIn;
        public event PropertyChangedEventHandler PropertyChanged;
        DCInsideAuth auth;

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
            isoSettings.TryGetValue("dcview.savelogininfo", out saveLoginInfo);

            if (saveLoginInfo)
            {
                isoSettings.TryGetValue("dcview.id", out id);
                isoSettings.TryGetValue("dcview.password", out password);
            }

            auth = new DCInsideAuth();
        }        

        public void Save()
        {
            var isoSettings = IsolatedStorageSettings.ApplicationSettings;
            isoSettings["dcview.autologin"] = autoLogin;
            isoSettings["dcview.savelogininfo"] = saveLoginInfo;

            if (saveLoginInfo)
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

        public async void Login(CancellationToken ct)
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(() => Login(ct));
                return;
            }
            
            // 우선 로그인 중인지부터 확인
            if (LoginState != State.NotLoggedIn)
                return;

            LoginState = State.LoggingIn;

            bool bSucceed = await Task.Factory.StartNew(() => auth.Login(id, password, ct));

            if (bSucceed)
            {
                LoginState = State.LoggedIn;
            }
            else
            {
                LoginState = State.NotLoggedIn;                
            }            
        }

        public void Logout()
        {
            if (!dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(() => Logout());
                return;
            }
            
            LoginState = State.NotLoggedIn;
            DCViewWebClient.ResetCookie(); // 쿠키값 리셋
        }
    }
}
