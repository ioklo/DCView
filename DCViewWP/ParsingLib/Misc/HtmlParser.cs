using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

namespace DCView.Misc
{
    public static class HtmlParser
    {
        private static Regex urlRegex = new Regex(@"((https?|ftp|gopher|telnet|file|notes|ms-help):((//)|(\\\\))+[\w\d:#@%/;$()~_?\+-=\\\.&]*)");

        public class Splitted
        {
            public string Content { get; set; }
            public bool IsUrl { get; set; }
        }

        static public IEnumerable<Splitted> SplitUrl(string input)
        {
            int cur = 0;

            Match match = urlRegex.Match(input);

            while (match.Success)
            {
                yield return new Splitted() { Content = input.Substring(cur, match.Index - cur), IsUrl = false };

                yield return new Splitted() { Content = match.Value, IsUrl = true };

                cur = match.Index + match.Length;
                match = urlRegex.Match(input, cur);
            }

            if (cur < input.Length)
                yield return new Splitted() { Content = input.Substring(cur), IsUrl = false };
        }

        
        static public string StripTags(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IHtmlEntity entity in HtmlLexer.Lex(input))
            {
                if (entity is PlainString)
                {
                    // 바로 출력하지는 않고 
                    PlainString plainString = (PlainString)entity;
                    sb.Append(plainString.Content);
                }
                else if (entity is WhiteSpaces)
                {
                    if (0 < sb.Length && !char.IsWhiteSpace(sb[sb.Length - 1]))
                        sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        
    }
}
