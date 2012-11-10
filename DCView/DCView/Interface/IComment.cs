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

namespace DCView
{
    public interface IComment
    {
        int Level { get; }   // 댓글의 레벨, 0부터 시작
        string Name { get; }
        string Text { get; }
    }
}
