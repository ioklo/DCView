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
using MyApps.Common;
using System.Text;
using System.Text.RegularExpressions;
using DCView.Util;

namespace DCView
{
    public class ArticleWriter : INotifyPropertyChanged
    {
        public enum State
        {
            Starting,
            Ready,
            Sending,
            SendCompleted,
            Error, // 에러 
        }        

        public event PropertyChangedEventHandler PropertyChanged;

        // 게시판 아이디
        public string ID { get; private set; }
        public string Title 
        {
            get{ return title;}
            set
            {
                title = value;
                Notify("Title");
            }
        }

        public string Text
        {
            get{ return text;}
            set
            {
                text = value;
                Notify("Text");
            }
        }

        public State WriterState
        {
            get { return state; }
            set
            {
                state = value;
                Notify("WriterState");
            }
        }

        private State state;
        private string code;
        private string mobileKey;
        private string title;
        private string text;

        public void Notify(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        
        public ArticleWriter(string id)
        {
            ID = id;
            this.state = State.Starting;
        }

        public void Refresh()
        {
            DCViewWebClient client = new DCViewWebClient();
            client.Encoding = Encoding.UTF8;            
            client.Headers["Referer"] = string.Format("http://m.dcinside.com/list.php?id={0}", ID);

            client.DownloadStringCompleted += (o, e) =>
            {
                if (e.Cancelled || e.Error != null)
                {
                    WriterState = State.Error;
                    return;
                }

                Regex codeRegex = new Regex("<input type=\"hidden\" name=\"code\" value=\"([^\"]*)\"");
                Regex mobileKeyRegex = new Regex("<input type=\"hidden\" name=\"mobile_key\" id=\"mobile_key\" value=\"([^\"]*)\"");

                StringEngine se = new StringEngine(e.Result);
                Match match;

                if (!se.Next(codeRegex, out match))
                {
                    WriterState = State.Error;
                    return;
                }

                code = match.Groups[1].Value;

                if (!se.Next(mobileKeyRegex, out match))
                {
                    WriterState = State.Error;
                    return;
                }

                mobileKey = match.Groups[1].Value;

                WriterState = State.Ready;
            };            

            client.DownloadStringAsync(new Uri("http://m.dcinside.com/write.php?id=windowsphone&mode=write", UriKind.Absolute));
            
        }

        public void Send()
        {
            if (App.Current.LoginInfo.LoginState != LoginInfo.State.LoggedIn)
                return;

            string data = string.Format(
                "subject={0}&memo={1}&user_id={2}&mode=write&id={3}&code={4}&mobile_key={5}",
                HttpUtility.UrlEncode(Title),
                HttpUtility.UrlEncode(Text),
                HttpUtility.UrlEncode(App.Current.LoginInfo.ID),
                HttpUtility.UrlEncode(ID),
                code,
                mobileKey);

            DCViewWebClient client = new DCViewWebClient();
            client.Encoding = Encoding.UTF8;
            client.Headers["Content-Type"] = "application/x-www-form-urlencoded";
            client.Headers["Referer"] = string.Format("http://m.dcinside.com/write.php?id={0}&mode=write", ID);

            client.UploadStringCompleted += (o, e) =>
            {
                if (e.Cancelled || e.Error != null)
                {
                    WriterState = State.Error;
                    return;
                }

                WriterState = State.SendCompleted;
            };

            WriterState = State.Sending;
            client.UploadStringAsync(new Uri("http://upload.dcinside.com/g_write.php", UriKind.Absolute), "POST", data);                       
        }

        // State 
        // Write할 정보를 얻어내는 부분 

        // 1. Starting (시작)
        // 2. Ready (준비완료)
        // 3. Sending
        // 4. Sent        



        
    }
}
