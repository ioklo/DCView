using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Threading;

namespace DCView
{
    public class DCInsideArticle : IArticle
    {
        private DCInsideBoard board;

        // 기본 정보
        public string ID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }

        // 글 리스트만 봤을 때 얻을 수 있는 정보, 나중에 구체적인 정보가 오면 쓸모 없어진다 
        public bool HasImage { get; set;  }
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }

        public string CommentUserID { get; set; } // 댓글 요청 보낼때 쓸 user_no

        public List<DCInsideComment> CachedComments { get; set; }
        public List<Picture> Pictures { get; set; }

        public Uri Uri
        {
            get { return board.GetArticleUri(this); }
        }

        public bool CanWriteComment { get { return true; } }

        public DCInsideArticle(DCInsideBoard board)
        {
            this.board = board;
        }

        public bool GetText(CancellationToken ct, out string text)
        {
            return board.GetArticleText(this, ct, out text);
        }

        public bool WriteComment(string text, CancellationToken ct)
        {
            return board.WriteComment(this, text, ct);
        }

        public MemberStatus MemberStatus { get { return MemberStatus.Fix; } }

        class CommentLister : ILister<IComment>
        {
            bool bOnce = false;
            List<DCInsideComment> comments;

            public CommentLister(List<DCInsideComment> comments)
            {
                this.comments = comments;
            }

            IEnumerable<IComment> GetEnum()
            {
                foreach (var comment in comments)
                    yield return comment;                
            }

            public bool Next(CancellationToken ct, out IEnumerable<IComment> elems)
            {
                elems = null;
                if (bOnce) return false; // 끝

                elems = GetEnum();
                bOnce = true;
                return true;
            }
        }

        public ILister<IComment> GetCommentLister()
        {
            return new CommentLister(CachedComments);
        }
    }
}
