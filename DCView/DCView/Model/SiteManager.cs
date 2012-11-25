using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;

namespace DCView
{
    // 사이트 전체를 관리하는 팩토리
    public class SiteManager
    {
        // favorite과 gallery의 분리
        // Dictionary<string, Gallery> galleries = new Dictionary<string, Gallery>();

        ManualResetEvent loadingComplete = new ManualResetEvent(false);
        List<ISite> sites = new List<ISite>();

        public IEnumerable<IBoard> All
        {
            get 
            {
                foreach (var site in sites)
                    foreach (var board in site.Boards)
                        yield return board;
            }
        }
        
        public SiteManager()
        {
            sites.Add(new DCInsideSite());
            sites.Add(new Clien());

            // 만들어 지자 마자, loading을 시작한다            
            Task.Factory.StartNew(() =>
            {
                try
                {
                    foreach (var site in sites)
                        site.Load();
                }
                finally
                {
                    loadingComplete.Set();
                }
            });
        }

        public IEnumerable<ISite> Sites
        {
            get
            {
                return sites;
            }
        }

        

        public ISite GetSite(string siteID)
        {
            foreach (var site in sites)
            {
                if (site.ID == siteID)
                    return site;
            }

            return null;
        }

        public IBoard GetBoard(string siteID, string boardID, string boardName)
        {
            foreach (var site in sites)
            {
                if (site.ID == siteID)
                    return site.GetBoard(boardID, boardName);
            }

            // board가 없으면 리턴
            return null;
        }

        public void WaitForLoadingComplete(CancellationToken? token)
        {
            // Loading이 끝날때까지는 Search 하지 않고 기다림
            while (true)
            {
                if (loadingComplete.WaitOne(EventWaitHandle.WaitTimeout))
                    break;

                if (token.HasValue && token.Value.IsCancellationRequested)
                    return;
            }
        }        
        
        // 이것도 조금 오래 걸릴 수 있다.
        public void Search(string text, CancellationToken token, ISite site, Action<IBoard> OnSearchGallery)
        {
            foreach (var board in site.Boards)
            {
                if (token.IsCancellationRequested)
                    return;

                if (board.ID.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    board.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    OnSearchGallery(board);
            }
        }
    }
}
