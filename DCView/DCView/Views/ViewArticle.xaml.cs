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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows.Data;
using System.Collections.Specialized;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using MyApps.Common;
using MyApps.Common.HtmlParser;
using DCView.Util;

namespace DCView
{
    // 글 목록을 보고 읽는 곳
    public partial class ViewArticle : PhoneApplicationPage
    {
        bool bInitialized = false;

        ViewArticleListPivotItem viewArticleListPivotItem = null;
        ViewArticleListPivotItem viewSearchArticleListPivotItem = null; // 검색 결과
        ViewArticleTextPivotItem viewArticleTextPivotItem = null;
        WriteArticlePivotItem writeArticlePivotItem = null;

        Thickness pivotMargin = new Thickness(6, 14, 6, 0);

        // 공통 함수
        public void ShowErrorMessage(string msg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => ShowErrorMessage(msg));
                return;
            }

            MessageBox.Show(msg);
        }                

        
        // 생성자
        public ViewArticle()
        {
            InitializeComponent();            
        }

        // 오버라이드 함수들
        // 백키를 누르면 바로 이전것으로 돌아가지 않고, 목록에 있을때만 뒤로 되돌아간다
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (MainPivot.SelectedIndex != 0)
            {
                MainPivot.SelectedIndex = 0;
                e.Cancel = true;
            }
        }

        // 페이지에서 나가려고 할 때
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        // 이 페이지로 들어오려고 할 때
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            Initialize();
        }

        private void Initialize()
        {            
            if (bInitialized) return;
            
            string id = NavigationContext.QueryString["id"];

            viewArticleListPivotItem = new ViewArticleListPivotItem(this, new DCInsideBoard(id));
            
            MainPivot.Title = App.Current.GalleryList[id].Name + " 갤러리"; // 왼쪽 상단 갤러리 이름 적기
            MainPivot.Items.Clear();
            MainPivot.Items.Add(viewArticleListPivotItem);

            viewArticleListPivotItem.RefreshArticleList(); // 리스트 갱신
            
            bInitialized = true;
        }
        
        // 하는 행동
        private void PivotMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                UpdatePivotItem(item);
            }
        }

        private void UpdatePivotItem(object item)
        {
            INotifyActivated obj = item as INotifyActivated;
            if (obj == null) return;

            obj.OnActivated();
        }

        public void Search(IBoard board, string text, SearchType searchType)
        {
            // 새로운 searchBoard 를 만든다
            viewSearchArticleListPivotItem = new ViewSearchArticleListPivotItem(this, board, text, searchType);

            // 목록창은 항상 0번에
            MainPivot.Items[0] = viewSearchArticleListPivotItem;
            MainPivot.SelectedIndex = 0;
            UpdatePivotItem(viewSearchArticleListPivotItem);
            
            viewSearchArticleListPivotItem.RefreshArticleList();
            this.Focus();
        }

        public void SelectArticle(IArticle article)
        {
            if (viewArticleTextPivotItem == null)
                viewArticleTextPivotItem = new ViewArticleTextPivotItem(this);


            // '내용' 탭이 없다면 이제 추가해 준다            
            if (!MainPivot.Items.Contains(viewArticleTextPivotItem))
                MainPivot.Items.Insert(1, viewArticleTextPivotItem);

            // '내용' 탭으로 화면 전환
            MainPivot.SelectedItem = viewArticleTextPivotItem;
            viewArticleTextPivotItem.SetArticle(article);
        }

        public void ShowWriteForm()
        {
            // 글쓰기 폼 보이기
            if (writeArticlePivotItem == null)
                writeArticlePivotItem = new WriteArticlePivotItem(this);

            if (!MainPivot.Items.Contains(writeArticlePivotItem))
                MainPivot.Items.Add(writeArticlePivotItem);

            MainPivot.SelectedItem = writeArticlePivotItem;
        }

        public void RemoveWriteForm()
        {
            if (writeArticlePivotItem == null)
                return;

            MainPivot.Items.Remove(writeArticlePivotItem);
            MainPivot.SelectedIndex = 0;
        }

        public void ShowArticleList()
        {            
            MainPivot.Items[0] = viewArticleListPivotItem;
            MainPivot.SelectedIndex = 0;

            UpdatePivotItem(viewArticleListPivotItem);            
        }
    }
}