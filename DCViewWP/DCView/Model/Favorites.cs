using System;
using System.Net;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.IO;
using System.Threading;

namespace DCView
{
    // 
    public class Favorites
    {
        public class Entry
        {
            public string SiteID { get; set; }
            public string BoardID { get; set; }
            public string DisplayName { get; set; }
        }

        ObservableCollection<Entry> favorites = new ObservableCollection<Entry>();
        bool bFavoriteModified = false;

        public ObservableCollection<Entry> All
        {
            get
            {
                return favorites;
            }
        }

        public Favorites()
        {
            Load();
        }

        private void ConvertV2ToV3()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();

            if (storage.FileExists("/DCView_favorites3.txt")) return;

            // 이전 데이터 마이그레이션
            if (storage.FileExists("/DCView_favorites2.txt"))
            {
                using (var writer = new StreamWriter(storage.CreateFile("/DCView_favorites3.txt")))
                using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites2.txt", FileMode.Open)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string name = line;

                        line = reader.ReadLine();
                        if (line == null) break;

                        string id = line;

                        writer.WriteLine("dcinside");
                        writer.WriteLine(id);
                        writer.WriteLine(name);
                    }
                }
            }
            else
            {
                using (var stream = new StreamWriter(storage.CreateFile("/DCView_favorites3.txt")))
                {
                    stream.WriteLine("dcinside");                    
                    stream.WriteLine("windowsphone");
                    stream.WriteLine("윈도우폰");
                }
            }
        }

        public void Load()
        {
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForApplication();
            ConvertV2ToV3(); // 버전 2를 버전3으로 

            // favorite 리스트를 얻어온다..
            using (var reader = new StreamReader(storage.OpenFile("/DCView_favorites3.txt", FileMode.Open)))
            {
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string siteID = line;
                    string boardID = reader.ReadLine();
                    if (boardID == null) break;

                    string boardName = reader.ReadLine();
                    if (boardName == null) break;

                    favorites.Add(new Entry() { SiteID = siteID, BoardID = boardID, DisplayName = boardName });
                }
            }
        }

        public void Add(string siteID, string boardID, string boardName)
        {
            // 이미 있는지 검색한다
            foreach (var entry in favorites)
                if (entry.SiteID == siteID && entry.BoardID == boardID)
                    return;

            bFavoriteModified = true;
            favorites.Add(new Entry() { SiteID = siteID, BoardID = boardID, DisplayName = boardName });
        }

        public void Remove(Entry entry)
        {
            bFavoriteModified = true;
            favorites.Remove(entry);
        }

        public void Save()
        {
            if (!bFavoriteModified) return;

            // favorite 저장
            var storage = IsolatedStorageFile.GetUserStoreForApplication();
            using (var writer = new StreamWriter(storage.OpenFile("/DCView_favorites3.txt", FileMode.Create)))
            {
                foreach (var entry in favorites)
                {
                    writer.WriteLine(entry.SiteID);
                    writer.WriteLine(entry.BoardID);
                    writer.WriteLine(entry.DisplayName);
                }
            }
        }
    }
}
