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
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MyApps.Common
{
    public class StringHtmlEntityConverter
    {
        // 어떤 스테이트 실행기는.. 
        // 실행의 결과는.. => 결과물과, 다음 스테이트가 뭔지도 알려줘야 한다.
        public delegate bool StateExecuter<T>(StringReader reader, T data, out StateExecuter<T> nextExecuter);

        // WhiteSpace가 끝날때까지 reader 진행시키기
        public bool PassThroughWhiteSpaces(StringReader reader)
        {
            int c;
            while ((c = reader.Peek()) != -1)
            {
                if (!char.IsWhiteSpace((char)c))
                    return true;

                // 소비
                reader.Read();
            }

            return true;
        }

        // 다음 문자가 c인지 확인
        public bool PassCharacter(StringReader reader, char expectedChar)
        {
            int i = reader.Peek();

            if (i == -1) return false;
            char c = (char)i;

            if (c != expectedChar) return false;

            reader.Read();
            return true;
        }

        // 문자 c가 나올때까지 reader 진행시키기 
        public bool PassThroughCharacter(StringReader reader, char expectedChar)
        {
            int i;
            while ((i = reader.Read()) != -1)
            {
                char c = (char)i;

                if (c == expectedChar)
                    return true;
            }

            return false;
        }

        // 지금 위치에서부터 단어를 얻는다
        // 1. 단어를 못얻을 경우.. 포인터 안 증가 => null
        // 2. 단어를 얻을 경우 => str
        public string GetWord(StringReader reader)
        {
            StringBuilder builder = new StringBuilder();

            int i;
            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (!Char.IsLetter(c))
                    break;

                builder.Append(c);
                reader.Read();
            }

            // 끝에 도달하거나 Letter가 아니면 이리로 온다

            // 단어가 없었다면
            string str = builder.ToString();
            if (str.Length == 0) return null;

            return str;
        }

        // AttrValue에 특화된 가져오기
        // 1. 끝에 도달했는데 읽어들였으면 => null
        // 2. 
        public string GetAttrValue(StringReader reader)
        {
            int i = reader.Peek();
            if (i == -1) return string.Empty;

            char c = (char)i;

            if (c == '\'' || c == '\"')
            {
                // 한 글자 소비
                reader.Read();
                char quoteMark = c;

                StringBuilder sb = new StringBuilder();

                while ((i = reader.Read()) != -1)
                {
                    c = (char)i;

                    if (c == quoteMark)
                        return sb.ToString();

                    sb.Append(c);
                }

                // 마크 없이 그냥 끝에 도달해도 인정
                return sb.ToString();
            }
            else
            {
                StringBuilder sb = new StringBuilder();

                while ((i = reader.Peek()) != -1)
                {
                    c = (char)i;

                    // letter(a-zA-Z) digits(0-9) hyphen(-), period(.), underscore(_), colon(:)                    
                    if (!(char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == '_' || c == ':'))
                        break;

                    sb.Append(c);
                    reader.Read(); // 한글자 소비 
                }

                return sb.ToString();
            }
        }

        public bool GetTag(StringReader reader, List<IHtmlEntity> entities, out StateExecuter<List<IHtmlEntity>> nextState)
        {
            Tag tag = new Tag();
            nextState = null;

            // 이건 말도 안되는거니까 놔둔다
            if (!PassCharacter(reader, '<'))
                return false;

            PassThroughWhiteSpaces(reader);

            if (PassCharacter(reader, '/'))
            {
                // Closing Tag
                tag.Kind = Tag.TagKind.Close;
                PassThroughWhiteSpaces(reader);
            }
            else
            {
                tag.Kind = Tag.TagKind.Open;
            }

            string name = GetWord(reader);

            if (name == null)
                return false;

            tag.Name = name.ToLower();

            PassThroughWhiteSpaces(reader);

            // 이번이 > 문자열이라면 종료
            // 끝에 도달하지 않을 경우
            while (reader.Peek() != -1)
            {
                PassThroughWhiteSpaces(reader);

                // Closing Tag
                if (PassCharacter(reader, '/'))
                {
                    PassThroughWhiteSpaces(reader);

                    if (!PassCharacter(reader, '>'))
                        continue;

                    tag.Kind = Tag.TagKind.OpenAndClose;
                    break;
                }

                if (PassCharacter(reader, '>'))
                    break;

                // 키를 가져온다
                string key = GetWord(reader);

                if (key == null)
                {
                    PassThroughCharacter(reader, '>');
                    break;
                }

                PassThroughWhiteSpaces(reader);

                // 그 다음엔 =이 나와야 한다. 안 나왔다면
                if (!PassCharacter(reader, '='))
                {
                    tag.Attrs[key.ToLower()] = string.Empty;
                    continue;
                }

                PassThroughWhiteSpaces(reader);

                string value = GetAttrValue(reader);

                // value가 null이 나오는 경우는 없다
                tag.Attrs[key.ToLower()] = value;
            }

            entities.Add(tag);
            nextState = GetPlainString;
            return true;
        }

        public bool GetPlainString(StringReader reader, List<IHtmlEntity> entities, out StateExecuter<List<IHtmlEntity>> nextState)
        {
            StringBuilder sb = new StringBuilder();

            // 중복 스페이스 처리
            bool bSpace = false;

            int i;

            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (bSpace)
                {
                    if (char.IsWhiteSpace(c))
                    {
                        // 소비
                        reader.Read();
                        continue;
                    }

                    bSpace = false;
                }
                else
                {
                    if (char.IsWhiteSpace(c))
                        bSpace = true;
                }

                // 태그의 시작이 나타나면 소비하지 않고 종료
                if (c == '<')
                {
                    if (sb.Length != 0)
                        entities.Add(new PlainString() { Content = sb.ToString() });

                    nextState = GetTag;
                    return true;
                }

                sb.Append(c);
                reader.Read();
            }

            // 끝까지 오면
            if (sb.Length != 0)
                entities.Add(new PlainString() { Content = sb.ToString() });

            nextState = null;
            return true;
        }

        public bool Get<T>(StringReader reader, T data, StateExecuter<T> initState)
        {
            StateExecuter<T> stateExecuter = initState;

            // 종료 스테이트가 될 때까지 돌린다
            while (stateExecuter != null)
            {
                StateExecuter<T> nextExecuter;

                if (!stateExecuter(reader, data, out nextExecuter))
                {
                    // 에러상태 다 쓸모 없어짐
                    return false;
                }

                stateExecuter = nextExecuter;
            }

            return true;
        }

        public List<IHtmlEntity> Convert(string html)
        {
            StringReader reader = new StringReader(html);

            List<IHtmlEntity> entities = new List<IHtmlEntity>();
            if (Get<List<IHtmlEntity>>(reader, entities, GetPlainString))
                return entities;

            return null;
        }
    }

}
