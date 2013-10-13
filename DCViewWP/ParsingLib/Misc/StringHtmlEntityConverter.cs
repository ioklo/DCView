using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace DCView.Misc
{
    public class Scope : IDisposable
    {
        bool bAccept = false;
        Action RollBackAction;

        public Scope(Action action)
        {
            this.RollBackAction = action;
        }

        public void Accept()
        {
            bAccept = true;
        }

        public void Dispose()
        {
            // TODO: finalizer를 통해 들어왔을 때는 아무 일도 일어나지 않아야 한다
            if (!bAccept)
                RollBackAction();
        }
    }

    public class Parser
    {
        private String buffer;

        private int curIndex; // buffer에서의 인덱스

        public Scope Open()
        {
            int baseIndex = curIndex;
            Scope scope = new Scope(() => { curIndex = baseIndex; }); 
            return scope;
        }
        
        public Parser(string str)
        {
            // TODO: 조금씩 가져오는건 나중에 합시다.
            buffer = str;
        }

        public bool ConsumeIf(Predicate<char> Pred, out string str)
        {
            bool bOnce = false;
            int startIndex = curIndex;

            using (Scope scope = Open())
            {
                while(curIndex < buffer.Length)
                {
                    // peek
                    char c = buffer[curIndex];
                    if (!Pred(c)) break;

                    bOnce = true;
                    curIndex++;
                }

                if (bOnce)
                {
                    scope.Accept();
                    str = buffer.Substring(startIndex, curIndex - startIndex);
                    return true;
                }
                
                str = null;
                return false;
            }
        }

        public bool Consume(string str)
        {
            using (Scope scope = Open())
            {
                foreach (char c in str)
                {
                    if (buffer.Length <= curIndex)
                        return false;

                    if (buffer[curIndex] != c)
                        return false;

                    curIndex++;
                }

                scope.Accept();
                return true;
            }
        }

        // 모두 뒤져도 나오지 않았을 경우에
        public bool ConsumeUntil(string str, out string output, bool bConsumeWhenFailed = false)
        {
            int pos = buffer.IndexOf(str, curIndex);

            if (pos != -1)
            {
                output = buffer.Substring(curIndex, pos - curIndex);
                curIndex = pos + str.Length;
                return true;
            }
            
            if (bConsumeWhenFailed)
            {
                curIndex = buffer.Length;
                output = buffer.Substring(curIndex);
                return true;
            }

            output = string.Empty;
            return false;
        }

        // n개만큼 가져와본다, n개가 안될수도 있다
        public String Peek(int n)
        {
            int end = Math.Min(curIndex + n, buffer.Length);
            StringBuilder sb = new StringBuilder();

            // curIndex로부터 n개
            for( int t = curIndex; t < end; t++)
                sb.Append(buffer[t]);

            return sb.ToString();
        }

        public bool Valid
        {
            get { return curIndex < buffer.Length; }
        }
    }


    // 다시 만들어 보는 HtmlLexer
    // HtmlLexer 의 결과물은 -> PlainString 혹은 Tag
    public class HtmlLexer
    {
        // state 모델의 문제는
        // 내가 지금 왜 이 스테이트에 있는지 알 수가 없다. (디버깅이 어려움)
        Parser parser;

        private HtmlLexer(string str)
        {
            parser = new Parser(str);
        }

        private bool ConsumeWhiteSpaces()
        {
            string output;

            return parser.ConsumeIf(c => Char.IsWhiteSpace(c), out output);
        }

        private bool GetEntity(out IHtmlEntity entity)
        {
            // 빈란
            if (ConsumeWhiteSpaces())
            {
                entity = new WhiteSpaces();
                return true;
            }

            // <!-- 으로 시작하는지
            if (parser.Consume("<!--"))
            {
                // --> 가 없어도 모두 consume 하고 끝내면 된다
                string temp;
                parser.ConsumeUntil("-->", out temp, true);
                entity = new Comment();
                return true;
            }

            // Tag
            Tag tag;
            if (ConsumeTag(out tag))
            {
                entity = tag;
                return true;
            }
        
            // PlainString
            PlainString plain;
            if (ConsumePlainString(out plain))
            {
                entity = plain;
                return true;
            }

            entity = null;
            return false;
        }

        private bool ConsumePlainString(out PlainString plain)
        {
            string output;
            if (parser.ConsumeIf(c => !Char.IsWhiteSpace(c) && c != '<', out output))
            {
                plain = new PlainString() { Content = output };
                return true;
            }

            plain = null;
            return false;            
        }

        private bool ConsumeAttribute(out string elem, out string value)
        {
            if (!parser.ConsumeIf(c => Char.IsLetter(c), out elem))
            {
                value = string.Empty;
                return false;
            }

            ConsumeWhiteSpaces();

            if (!parser.Consume("="))
            {
                value = string.Empty;
                return true;
            }

            ConsumeWhiteSpaces();

            if (parser.Consume("\""))
            {
                parser.ConsumeUntil("\"", out value, true);
                return true;
            }
            else if (parser.Consume("'"))
            {
                parser.ConsumeUntil("'", out value, true);
                return true;
            }
            else if (parser.ConsumeIf(c => !Char.IsWhiteSpace(c) && c != '>', out value))
            {
                return true;
            }

            value = string.Empty;
            return false;
        }

        private bool ConsumeTag(out Tag tag)
        {
            tag = null;
            Tag res = new Tag();

            if (!parser.Consume("<")) return false;

            ConsumeWhiteSpaces();

            if (!parser.Consume("/"))
                res.Kind = Tag.TagKind.Open;
            else
                res.Kind = Tag.TagKind.Close;

            ConsumeWhiteSpaces();

            string name;
            if (!parser.ConsumeIf( c => Char.IsLetterOrDigit(c), out name))
                return false;
            
            res.Name = name;

            while (parser.Valid)
            {
                string elem, value;

                // 성공하든 말든 상관없다
                ConsumeWhiteSpaces();

                // elem = "value" 꼴                
                if (parser.Consume(">"))
                {
                    tag = res;
                    return true;
                }

                using (Scope scope = parser.Open())
                {
                    if (parser.Consume("/"))
                    {
                        ConsumeWhiteSpaces();

                        if (parser.Consume(">"))
                        {
                            res.Kind = Tag.TagKind.OpenAndClose;
                            tag = res;
                            scope.Accept();
                            return true;
                        }
                    }
                }

                if (ConsumeAttribute(out elem, out value))
                {
                    res.Attrs[elem.ToLower()] = value;
                    continue;
                }

                break;
            }

            return false;
        }

        static public IEnumerable<IHtmlEntity> Lex(string str)
        {
            HtmlLexer lexer = new HtmlLexer(str);

            IHtmlEntity entity;
            while (lexer.GetEntity(out entity))
                yield return entity;
        }
    }


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
            else if (PassCharacter(reader, '!'))
            {
                // 주석 처리
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
                Debug.WriteLine(stateExecuter);

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

            return new List<IHtmlEntity>();
        }
    }

}
