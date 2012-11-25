using System;
using System.Net;
using System.Windows;

namespace DCView
{
    public interface IComment
    {
        int Level { get; }   // 댓글의 레벨, 0부터 시작
        string Name { get; }
        string Text { get; }
    }
}
