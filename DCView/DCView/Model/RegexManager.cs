using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        static void Load()
        {
            DCView.Lib.StorageUtil.CopyResourceToStorage("Data/pattern_dc.txt", "/pattern_dc.txt");

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
            DCView.Lib.StorageUtil.CopyResourceToStorage("Data/pattern_dc.txt", "/pattern_dc.txt", true);
            Load();
        }

        public static bool Update()
        {
            try
            {
                DCViewWebClient client = new DCViewWebClient();
                var result = client.DownloadStringAsyncTask(new Uri(
                    string.Format("http://ioklo.byus.net/dcview/pattern_dc.txt?nocache={0}", DateTime.Now.Ticks), UriKind.Absolute), new CancellationTokenSource().Token).GetResult();

                var storage = IsolatedStorageFile.GetUserStoreForApplication();
                using (var writer = new StreamWriter(storage.OpenFile("/pattern_dc.txt", FileMode.Create), Encoding.UTF8))
                {
                    writer.Write(result);
                }

                Load();
                return true;
            }
            catch
            {
                Reset();
                return false;
            }
        }
    }
}
