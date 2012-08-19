using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyApps.Common.HtmlParser
{
    public interface IHtmlEntity
    {
    }

    // 태그.. 
    public class Tag : IHtmlEntity
    {
        public enum TagKind
        {
            Open, Close, OpenAndClose
        }

        public string Name { get; set; }
        public IDictionary<string, string> Attrs { get; private set; }
        public TagKind Kind { get; set; }

        public Tag()
        {
            Attrs = new Dictionary<string, string>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (this.Kind == TagKind.Close)
                sb.AppendFormat("</{0}", this.Name);
            else
                sb.AppendFormat("<{0}", this.Name);

            foreach (var pair in this.Attrs)
            {
                sb.AppendFormat(" {0}=\"{1}\"", pair.Key, pair.Value);
            }

            if (this.Kind == TagKind.OpenAndClose)
                sb.AppendFormat(" />");
            else
                sb.AppendFormat(">");

            return sb.ToString();
        }
    }

    // Html에서 평문을 의미합니다
    public class PlainString : IHtmlEntity
    {
        public string Content { get; set; }
    }

}
