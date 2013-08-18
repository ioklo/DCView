using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;
using System.Text;
using DCView.Misc;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;

namespace DCView.Util
{
    public class HtmlElementConverter
    {
        private static StringHtmlEntityConverter stringHtmlConverter = new StringHtmlEntityConverter();

        static public IEnumerable<FrameworkElement> GetUIElementFromString(string input, Action<Uri> tapAction)
        {
            StringBuilder curPlainString = new StringBuilder();

            int pDepth = 0;

            foreach (IHtmlEntity entity in stringHtmlConverter.Convert(input))
            {
                if (entity is PlainString)
                {
                    // 바로 출력하지는 않고 
                    PlainString plainString = (PlainString)entity;
                    curPlainString.Append(plainString.Content);
                }
                else if (entity is Tag)
                {
                    Tag tag = (Tag)entity;

                    if (tag.Name.Equals("br"))
                    {
                        curPlainString.AppendLine();
                        continue;
                    }

                    // p는 0에서 1일때는 반응 안함
                    // 1에서 0으로 내려올때 

                    if (tag.Name == "p")
                    {
                        if (tag.Kind == Tag.TagKind.Open)
                            pDepth++;
                        else if (tag.Kind == Tag.TagKind.Close)
                            pDepth--;

                        if ((tag.Kind == Tag.TagKind.Open && pDepth > 1) ||
                            (tag.Kind == Tag.TagKind.Close && pDepth == 0) ||
                            (tag.Kind == Tag.TagKind.OpenAndClose))
                        {
                            foreach (var obj in MakeTextBlocks(curPlainString.ToString(), tapAction))
                                yield return obj;
                            curPlainString.Clear();
                            continue;
                        }
                    }

                    if (tag.Name == "div")
                    {
                        if (curPlainString.Length != 0)
                        {
                            foreach (var obj in MakeTextBlocks(curPlainString.ToString(), tapAction))
                            {
                                yield return obj;
                            }
                            curPlainString.Clear();
                        }
                        continue;
                    }

                    // 이미지 처리.. 몰라 
                    if (tag.Name.Equals("img", StringComparison.CurrentCultureIgnoreCase))
                    {
                        string url;
                        if (tag.Attrs.TryGetValue("src", out url))                        
                        {
                            var pic = new Picture(url, "");
                            var grid = new Grid();
                            grid.Tag = pic;

                            yield return grid;
                        }                        
                    }
                }
            }

            if (curPlainString.Length != 0)
                foreach (var obj in MakeTextBlocks(curPlainString.ToString(), tapAction))
                    yield return obj;

            // 1. <br> <br />은 \n으로 바꿈
            // 2. <p> </p>는 하나의 paragraph로 처리
            // 3. <div>도 마찬가지..
            // 4. <img src=는 image로 바꿈;
            // 5. <a>

            //string text = Regex.Replace(input, "\\s+", " ");
            //text = Regex.Replace(text, "(<br[^>]*>)|(<br[^/]*/>)", "\n", RegexOptions.IgnoreCase);

            //// p를 만나면 거기서 끊는다
            //foreach (var par in Regex.Split(text, "(<p[^>]*>)|(<div[^>]*>)", RegexOptions.IgnoreCase))
            //{               

            //    yield return textBlock;
            //}                    
        }



        static private IEnumerable<FrameworkElement> MakeTextBlocks(string input, Action<Uri> tapUrl)
        {
            input = HttpUtility.HtmlDecode(input);

            foreach (var s in HtmlParser.SplitUrl(input))
            {
                var splitted = s;

                // url임 
                if (splitted.IsUrl)
                {
                    var textBlock = new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 1.2,
                        Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style,
                        Margin = new Thickness(0, 3, 0, 3),
                        // Foreground = new SolidColorBrush(Colors.Blue),
                    };

                    var underline = new Underline();
                    underline.Inlines.Add(new Run() { Text = splitted.Content });
                    textBlock.Inlines.Add(underline);

                    Uri uri = new Uri(splitted.Content, UriKind.Absolute);

                    textBlock.Tap += (o1, e1) =>
                    {
                        tapUrl(uri);
                    };

                    yield return textBlock;
                }
                else
                {
                    // 지금까지 내용을 전부 flush 
                    var textBlock = new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 1.2,
                        Style = Application.Current.Resources["DCViewTextNormalStyle"] as Style,
                        Margin = new Thickness(0, 3, 0, 3),
                        Text = splitted.Content,
                    };

                    yield return textBlock;
                }
            }
        }


    }
}
