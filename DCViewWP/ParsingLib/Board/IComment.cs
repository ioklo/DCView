using System;
using System.Net;
using System.Windows;

namespace DCView.Board
{
    public interface IComment
    {
        int Level { get; }   // 댓글의 레벨, 0부터 시작
        string Name { get; }
        MemberStatus MemberStatus { get; }
        string Text { get; }
    }
}
