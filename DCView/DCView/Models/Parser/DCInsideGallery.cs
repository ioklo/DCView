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
using System.Collections.Generic;
using MyApps.DCView;

namespace MyApps.Models.Parser
{
    // 게시판이 할 수 있는 일들 모음
    public interface IBoard
    {
        // 보기
        // 게시글 리스트 얻어오기
        public IEnumerable<IList<Article>> GetArticleList();

        // 글 보기, 그림은?
        public bool GetArticleText(Article article);

        // 글의 댓글 얻어오기 (글 보기랑 같이 이뤄질 가능성이 있음)
        public IEnumerable<IList<Comment>> GetComments();

        // 쓰기
        // 글 쓰기, 이미지 첨부 기능
        // 댓글 쓰기
    }

    public class DCGallery : IBoard
    {
        string id;

        public DCGallery(string id)
        {
            this.id = id;
        }        
        

    }
}
