﻿using System;
using System.Net;
using System.Windows;
using System.Text.RegularExpressions;

namespace DCView.Lib
{
    public class StringEngine
    {
        private int cur = 0;
        public string data;
        public int Cursor { get { return cur; } }

        public StringEngine(string str)
        {
            data = str;
        }

        // 원하는 곳까지 갑니다.
        public bool Next(Regex regex, out Match match)
        {
            match = regex.Match(data, cur);

            if (!match.Success)
                return false;

            cur = match.Index + match.Length;

            return true;
        }

        Regex lineRegex = new Regex("^(.*)$", RegexOptions.Multiline);

        public bool GetNextLine(out string line)
        {
            Match match = lineRegex.Match(data, cur);
            
            if (!match.Success)
            {
                line = string.Empty;
                return false;
            }

            cur = match.Index + match.Length + 1;
            line = match.Groups[1].Value;
            return true;
        }
    }
        
}
