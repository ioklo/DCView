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
using DCView.Util;

namespace DCView.Util
{
    public class CommentedTextBox : TextBox
    {     
        bool bModified = false;  // 코멘트가 Text에 달려있는 상태

        public static readonly DependencyProperty CommentProperty;

        public string Comment
        {
            get
            {
                return (string)GetValue(CommentProperty);
            }
            set
            {
                SetValue(CommentProperty, value);
            }
        }

        public new string Text
        {
            get
            {
                if (!bModified)
                    return "";
                else
                    return base.Text;
            }

            set
            {
                base.Text = value;
            }
        }

        static CommentedTextBox()
        {
            CommentProperty = DependencyProperty.Register("Comment", typeof(string), typeof(CommentedTextBox), new PropertyMetadata(""));            
        }

        public CommentedTextBox()
        {
            this.RegisterForNotification("Text", this, TextPropertyChangedCallback);
        }

        void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string text = e.NewValue as string;
            if (text == null) return;

            // 비 편집 상태이고, 주석을 보여주는 상태였는데 text가 바뀐거라면
            if (!bModified && text.Length != 0)
            {
                // 주석 보여주기 끄기
                bModified = true;
            }
        }

        // 편집 상태에서는 bModified가 항상 true이다
        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            if (!bModified)
            {
                Text = "";
                bModified = true;
            }
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (Text.Length == 0)
            {
                Text = Comment;
                bModified = false;
            }
        }

    }
}
