using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DCView
{
    public class CommentViewModel
    {
        public IComment Comment { get; private set; }

        public CommentViewModel(IComment comment)
        {
            Comment = comment;
        }

        public Thickness MarginByLevel
        {
            get
            {
                return new Thickness(Comment.Level * 20, 10, 0, 10);
            }
        }

        public string Name
        {
            get { return Comment.Name; }
        }

        public Brush MemberStatusBrush
        {
            get
            {
                switch (Comment.MemberStatus)
                {
                    case MemberStatus.Anonymous:
                        return new SolidColorBrush(Colors.Transparent);

                    case MemberStatus.Default:
                        return new SolidColorBrush(Colors.LightGray);

                    case MemberStatus.Fix:
                        return new SolidColorBrush(Colors.Yellow);
                }

                return null;
            }
        }

        public Visibility MemberStatusVisibility
        {
            get
            {
                if (Comment.MemberStatus == MemberStatus.Anonymous)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

    }
}
