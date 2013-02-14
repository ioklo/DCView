using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using DCView.Lib;
using DCView.Util;

namespace DCView
{
    public static class DCRegexManager
    {
        // DCInside
        public static Regex WriteCode { get; private set; }
        public static Regex WriteMobileKey{ get; private set; }
        public static Regex WriteFLData { get; private set; }
        public static Regex WriteOFLData { get; private set; }

        public static Regex ListArticleNumber { get; private set; }
        public static Regex ListArticleData { get; private set; }

        public static Regex SearchListArticleNumber { get; private set; }
        public static Regex SearchListArticleData { get; private set; }

        public static Regex TextImage { get; private set; }
        public static Regex TextStart { get; private set; }
        public static Regex TextDIV { get; private set; }
        public static Regex TextCommentUserID { get; private set; }       

        public static Regex CommentStart { get; private set; }
        public static Regex CommentName { get; private set; }
        public static Regex CommentText { get; private set; }

        private static IsolatedStorageSettings setting = IsolatedStorageSettings.ApplicationSettings;

        private static Regex GetRegex(string key, string def)
        {
            string regex;
            if (setting.TryGetValue(key, out regex))
                return new Regex(regex, RegexOptions.IgnoreCase);

            return new Regex(def, RegexOptions.IgnoreCase);
        }

        static DCRegexManager()
        {
            Load();
        }

        static bool IsResourceVersionLatest(out int resVer)
        {
            // ResourceVersion을 연다.
            var sri = Application.GetResourceStream(new Uri("Data/pattern_version.txt", UriKind.Relative));

            Debug.Assert(sri != null);

            var reader = new StreamReader(sri.Stream);
            resVer = int.Parse(reader.ReadLine());

            // 로컬 버전은 세팅에서 읽어들일 수 있다.
            var settings = IsolatedStorageSettings.ApplicationSettings;
            int localVer;
            if (!settings.TryGetValue("dcview.pattern_version", out localVer))
            {
                // 그런거 저장 안되어 있으면 무조건 리소스 버전이 최신버전
                return true;
            }

            return resVer > localVer;
        }

        static void Load()
        {
            // 리소스에 있는 패턴 버전과 로컬의 패턴 버전 비교해서 리소스가 최신이면 덮어 쓴다
            int resVer;
            if (IsResourceVersionLatest(out resVer))
            {
                DCView.Lib.StorageUtil.CopyResourceToStorage("Data/pattern_dc.txt", "/pattern_dc.txt", true);

                var settings = IsolatedStorageSettings.ApplicationSettings;
                settings.Add("dcview.pattern_version", resVer);
                settings.Save();
            }

            // Iso Setting 이용하지 않고
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var reader = new StreamReader(storage.OpenFile("/pattern_dc.txt", FileMode.Open), Encoding.UTF8))
            {
                Dictionary<string, Regex> dict = new Dictionary<string, Regex>();

                // 버전 읽을 필요가 있나..
                string ver = reader.ReadLine();
                while (true)
                {
                    string key = reader.ReadLine();
                    if (key == null) break;

                    string regexStr = reader.ReadLine();
                    if (regexStr == null) break;
                    Regex regex = new Regex(regexStr.Trim());

                    dict[key.Trim()] = regex;
                }

                WriteCode = dict["dc.write.code"];
                WriteMobileKey = dict["dc.write.mobileKey"];
                WriteFLData = dict["dc.write.flData"];
                WriteOFLData = dict["dc.write.oflData"];

                ListArticleNumber = dict["dc.list.articleNumber"];
                ListArticleData = dict["dc.list.articleData"];

                SearchListArticleNumber = dict["dc.searchlist.articleNumber"];
                SearchListArticleData = dict["dc.searchlist.articleData"];

                TextImage = dict["dc.text.textImage"];
                TextStart = dict["dc.text.textStart"];
                TextDIV = dict["dc.text.textDIV"];
                TextCommentUserID = dict["dc.text.commentUserID"];

                CommentStart = dict["dc.comment.start"];
                CommentName = dict["dc.comment.name"];
                CommentText = dict["dc.comment.text"];
            }
        }


        public static void Reset()
        {
            // 로컬 버전 정보를 없앤다 -> 로딩시에 항상 리소스쪽 정보가 최신으로 인식되고 강제 복사된다
            var settings = IsolatedStorageSettings.ApplicationSettings;
            settings.Remove("dcview.pattern_version");
            settings.Save();

            Load();
        }

        public static string Update()
        {
            try
            {
                var settings = IsolatedStorageSettings.ApplicationSettings;
                DCViewWebClient client = new DCViewWebClient();

                // 버전 확인
                var verString = client.DownloadStringAsyncTask(new Uri(
                    string.Format("http://ioklo.byus.net/dcview/pattern_version.txt?nocache={0}", DateTime.Now.Ticks), UriKind.Absolute), new CancellationTokenSource().Token).GetResult();

                var reader = new StringReader(verString);
                int updateVer = int.Parse(reader.ReadLine());
                int localVer;

                if (!settings.TryGetValue("dcview.pattern_version", out localVer))
                    return "실패 - 패턴 버전을 가져올 수 없습니다";

                // 현재 버전이 더 높으면 업데이트 안함
                if (updateVer <= localVer)
                    return "지금 패턴이 최신입니다";
                
                client = new DCViewWebClient();
                var result = client.DownloadStringAsyncTask(new Uri(
                    string.Format("http://ioklo.byus.net/dcview/pattern_dc.txt?nocache={0}", DateTime.Now.Ticks), UriKind.Absolute), new CancellationTokenSource().Token).GetResult();

                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                using (var writer = new StreamWriter(storage.OpenFile("/pattern_dc.txt", FileMode.Create), Encoding.UTF8))
                {
                    writer.Write(result);
                }

                // 업데이트가 잘 끝나면 버전 기록
                settings["dcview.pattern_version"] = updateVer;
                settings.Save();

                // 로딩
                Load();
                return "패턴이 업데이트 되었습니다";
            }
            catch
            {
                Reset();
                return "실패 - 패턴을 처음 상태로 되돌립니다";
            }
        }
    }
}
