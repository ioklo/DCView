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
using Microsoft.Live;
using Microsoft.Live.Controls;
using System.Windows.Media.Imaging;

namespace MyApps.Common
{
    public partial class SelectSkydriveFile : PhoneApplicationPage
    {
        public class FileEntry
        {   
            IDictionary<string, object> data;

            public FileEntry(IDictionary<string, object> data)
            {
                this.data = data;
            }

            public string ID
            {
                get { return data["id"] as string; }
            }

            public string Name
            {
                get { return data["name"] as string; }
            }

            public string DisplayName
            {
                get { return Name; }
            }

            public bool IsFolder
            {
                get
                {
                    string type = data["type"] as string;
                    return type == "folder" || type == "album";
                }
            }

            public string ImageSource
            {
                get
                {
                    if (IsFolder)
                        return "/Common;component/icons/folder.png";
                    else
                        return "/Common;component/icons/document.png";
                }
            }

        }

        public SelectSkydriveFile()
        {
            InitializeComponent();
        }

        // 디렉토리를 보여준다
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);            
        }

        void GetFileList(string path)
        {
            ProgressBar.IsIndeterminate = true;

            LiveConnectClient client1 = new LiveConnectClient(LiveSignInButton.Session);
            client1.GetCompleted += (o1, e1) =>
            {
                if (e1.Error != null)
                {
                    MessageBox.Show("폴더 읽기에 실패했습니다");
                    ProgressBar.IsIndeterminate = false;
                    return;
                }
                
                // 파일의 기본 정보 읽어오기
                string parentID = e1.Result["parent_id"] as string;

                // 실제 파일 리스트 얻어오기
                LiveConnectClient client2 = new LiveConnectClient(LiveSignInButton.Session);
                client2.GetCompleted += (o2, e2) =>
                {
                    if (e2.Error != null)
                    {
                        MessageBox.Show("폴더 읽기에 실패했습니다");
                        ProgressBar.IsIndeterminate = false;
                        return;
                    }

                    IList<object> data = (IList<object>)e2.Result["data"];

                    FileList.Items.Clear();
                    if( parentID != null )
                    {
                        var btn = new Button()
                        {
                            Content = "상위 폴더"
                        };
                        btn.Click += (o3, e3) => { GetFileList(parentID); };
                        FileList.Items.Add(btn);
                    }

                    foreach (object entryObj in data)
                    {
                        var entry = entryObj as IDictionary<string, object>;
                        FileList.Items.Add(new FileEntry(entry));
                    }
                    ProgressBar.IsIndeterminate = false;
                };

                client2.GetAsync(path + "/files");
            };
            client1.GetAsync(path);
        }

        private void LiveSignInButton_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                GetFileList("me/skydrive");
            }
        }

        private void Item_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel sp = sender as StackPanel;
            FileEntry fe = sp.Tag as FileEntry;

            if (fe.IsFolder)
                GetFileList(fe.ID);           

        }
    }
}