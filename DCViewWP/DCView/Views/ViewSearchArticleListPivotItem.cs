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
using Microsoft.Phone.Shell;
using DCView.Board;

namespace DCView
{
    public partial class ViewSearchArticleListPivotItem : ViewArticleListPivotItem
    {
        private IBoard board;
        private string searchText;
        private SearchType searchType;

        public ViewSearchArticleListPivotItem(ViewArticle page, IBoard board, string text, SearchType searchType)
            : base(page, board)
        {
            // InitializeComponent();

            this.board = board;
            this.searchText = text;
            this.searchType = searchType;
            this.Header = "검색";

            // appBar에 전체 목록 보기
            var listIconButton = new ApplicationBarIconButton()
            {
                IconUri = new Uri("/Data/appbar.gotoslide.rest.png", UriKind.Relative),
                Text = "전체보기"
            };
            listIconButton.Click += listIconButton_Click;
            appBar.Buttons.Add(listIconButton);
        }

        protected override ILister<IArticle> GetLister()
        {
            return board.GetSearchLister(searchText, searchType);
        }


        protected new void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            base.SearchButton_Click(sender, e);
        }

        protected new void ArticleList_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            base.ArticleList_ManipulationCompleted(sender, e);
        }

        protected new void ArticleListItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            base.ArticleListItem_Tap(sender, e);
        }

        protected new void SearchType_Click(object sender, RoutedEventArgs e)
        {
            base.SearchType_Click(sender, e);
        }

        void listIconButton_Click(object sender, EventArgs e)
        {
            viewArticlePage.ShowArticleList();
        }
    }
}
