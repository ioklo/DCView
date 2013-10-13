using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using DCView.Board;
using DCView.Misc;

namespace DCView
{
    // 사이트 전체를 관리하는 팩토리
    public class SiteManager
    {   
        // 이것도 조금 오래 걸릴 수 있다.
        public async void Search(string text, CancellationToken token, ISite site, Action<IBoard> OnSearchGallery)
        {
            foreach (var board in await site.GetBoards())
            {
                if (token.IsCancellationRequested)
                    return;

                if (board.ID.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    board.Name.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    OnSearchGallery(board);
            }
        }

        public Tuple<ICredential, UserControl> GetCredential(string siteID, ViewArticle page)
        {
            ISite site = Factory.GetSite(siteID);

            if (site is DCInsideSite)
            {
                return Tuple.Create<ICredential, UserControl>(site.Credential, new DCInsideLoginPanel((DCInsideCredential)site.Credential, page));
            }
            else
                return Tuple.Create<ICredential, UserControl>(null, null);

        }
    }
}
