using System;
using System.Net;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using DCView.Adapter;

namespace DCView.Board
{
    // 지금 Credential
    // 2. 로그인해서 그 정보를 사용하려고 한다
    // 2-0. 로그인 상태가 아니고 로그인 중도 아니다
    // 2-1. 지금 로그인 중이라 정보를 사용할 수 없다
    // 2-2. 로그인이 완료되어서 정보를 사용할 수 있다
    public class DCInsideCredential : ICredential
    {
        public enum LoginStatus
        {            
            MemberNoLogin,     // 로그인 안한 상태
            MemberConnecting,  // 연결 중
            MemberLogin,       // 로그인 완료
        }
        
        public string MemberID { get; set; }
        public string MemberPassword { get; set; }
        public bool SaveLoginInfo { get; private set; }
        public bool AutoLogin { get; private set; }

        private LoginStatus status;
        public LoginStatus Status
        {
            get { return status; }       
            private set
            {
                if (status!= value)
                {
                    status = value;
                    if (OnStatusChanged != null)
                        OnStatusChanged(this, EventArgs.Empty);
                }
            }
        }

        public DCInsideCredential()
        {
            MemberID = string.Empty;
            MemberPassword = string.Empty;
            AutoLogin = false;
            SaveLoginInfo = false;

            var settings = AdapterFactory.Instance.Settings; // IsolatedStorageSettings.ApplicationSettings;

            bool autoLogin, saveLoginInfo;

            settings.TryGetValue("dcview.autologin", out autoLogin);
            AutoLogin = autoLogin;

            settings.TryGetValue("dcview.savelogininfo", out saveLoginInfo);
            SaveLoginInfo = saveLoginInfo;
            
            if (saveLoginInfo)
            {
                string id, password;

                if (settings.TryGetValue("dcview.id", out id))
                    MemberID = id;

                if (settings.TryGetValue("dcview.password", out password))
                    MemberPassword = password;
            }

            Status = LoginStatus.MemberNoLogin;

            // 자동 로그인이라면 프로그램이 시작할때 로그인을 시작한다
            if (autoLogin)
            {                
                // Task에서 워닝 어떻게
                Task<bool> bResult = DoLogin();
            }
        }

        // 로그인 하기
        // Status 
        public string StatusText
        {
            get
            {
                switch(Status)
                {
                    case LoginStatus.MemberNoLogin:
                        return "로그인되지 않음";

                    case LoginStatus.MemberConnecting:
                        return MemberID + "으로 로그인 중";

                    case LoginStatus.MemberLogin:
                        return MemberID + "으로 로그인";
                }

                return string.Empty;
            }
        }

        private async Task<bool> DoLogin()
        {
            Status = LoginStatus.MemberConnecting;

            var client = AdapterFactory.Instance.CreateWebClient();

            client.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            client.SetHeader("Referer", "http://m.dcinside.com/login.php?r_url=%2F");

            string data = string.Format(
                "user_id={0}&user_pw={1}&r_url=%2F",
                AdapterFactory.Instance.UrlEncode(MemberID),
                AdapterFactory.Instance.UrlEncode(MemberPassword));

            try
            {
                string result = await client.UploadStringAsyncTask(new Uri("http://dcid.dcinside.com/join/mobile_login_ok.php", UriKind.Absolute), "POST", data);
                var cookies = AdapterFactory.Instance.CookieContainer.GetCookies(new Uri("http://gall.dcinside.com"));

                if (result.IndexOf("parent.location.href='http://m.dcinside.com/'") == -1)
                {
                    Status = LoginStatus.MemberNoLogin;
                    return false;
                }
                else
                {
                    Status = LoginStatus.MemberLogin;
                    return true;
                }
            }
            catch
            {
                Status = LoginStatus.MemberNoLogin;
            }

            return false;
        }
        public async Task<bool> Login(string id, string password, bool? saveLoginInfo, bool? autoLogin)
        {
            MemberID = id;
            MemberPassword = password;
            SaveLoginInfo = saveLoginInfo ?? false;
            AutoLogin = autoLogin ?? false;

            // 세팅 저장
            bool result = await DoLogin();

            if (result)
            {
                var settings = AdapterFactory.Instance.Settings; // IsolatedStorageSettings.ApplicationSettings;
                settings["dcview.autologin"] = autoLogin;
                settings["dcview.savelogininfo"] = saveLoginInfo;

                if (SaveLoginInfo)
                {
                    settings["dcview.id"] = id;
                    settings["dcview.password"] = password;
                }
                else
                {
                    settings.Remove("dcview.id");
                    settings.Remove("dcview.password");
                }
                settings.Save();
            }

            return result;
        }
        public void Logout()
        {
            Status = LoginStatus.MemberNoLogin;
            AdapterFactory.Instance.ResetCookie(); // 쿠키값 리셋
        }

        public event EventHandler OnStatusChanged;
    }
}
