﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NHibernate.FormatSQL.Formatter
{
    public static class Extentions
    {
        public static string MakeEqualSpace(this string @this)
        {
            var regexMatches = Regex.Replace(@this, "\x20{2,20}", " ");
            return regexMatches;
        }

        public static string FirstValueBetweenChars(this string @this, char a, char b, bool keepChars = false)
        {
            int lastIndex = 0;
            return FirstValueBetweenChars(@this, a, b, out lastIndex, keepChars);
        }

        public static string FirstValueBetweenChars(this string @this, char a, char b, out int lastIndex, bool keepChars = false)
        {
            lastIndex = 0;

            int indexOfA = @this.IndexOf(a);
            int indexOfB = @this.IndexOf(b);
            if (indexOfA < 0 || indexOfB < 0)
                return @this;

            if (indexOfA + indexOfB == @this.Length)
                return @this; // basicall send back [] 

            int start = keepChars ? indexOfA + 0 : indexOfA + 1;
            int length = keepChars ? (indexOfB - indexOfA) + 1 : (indexOfB - indexOfA) - 1;
            lastIndex = keepChars ? indexOfB : indexOfB + 1;

            string returnvalue = @this.Substring(start, length);
            return returnvalue;
        }

        public static string[] SplitAndKeep(this string @this, params string[] seperator)
        {
            List<string> returnList = new List<string>();

            string search = string.Empty;
            foreach (var item in seperator)
            {
                search = string.Format(@"{0}\{1}|", search, item);
            }

            search = string.Format("({0})", search.Remove(search.Length - 1, 1));
            var matches = Regex.Matches(@this, search, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                int start = 0;
                string value = string.Empty;
                for (int ix = 0; ix < matches.Count; ix++)
                {
                    value = @this.Substring(start, matches[ix].Index - start);
                    if (!string.IsNullOrWhiteSpace(value))
                        returnList.Add(value);

                    string matchValue = matches[ix].Value;
                    returnList.Add(matchValue);

                    start = matches[ix].Index + 1;
                }

                value = @this.Substring(start);
                if (!string.IsNullOrWhiteSpace(value))
                    returnList.Add(value);
            }

            return returnList.ToArray();
        }

        public static string[] SplitByWord(this string @this, params string[] seperator)
        {
            StringBuilder a = new StringBuilder();
            StringBuilder b = new StringBuilder();
            List<string> returnList = new List<string>();

            string[] splitBySpaces = @this.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < splitBySpaces.Length; ix++)
            {
                b = new StringBuilder();
                string currentWord = splitBySpaces[ix];
                for (int iy = 0; iy < currentWord.Length; iy++)
                {
                    char current = currentWord[iy];
                    a.Append(current);
                    b.Append(current);
                    if (seperator.Contains(b.ToString().Trim()))
                    {
                        string value = a.ToString().Substring(0, a.ToString().Length - b.ToString().Length).Trim();
                        if (!string.IsNullOrEmpty(value))
                        {
                            returnList.Add(value.ToString());
                        }
                        a = new StringBuilder();
                    }
                }
                a.Append(" ");
            }
            if (a.ToString().Trim().Length > 0 && a.Length > 0)
            {
                returnList.Add(a.ToString().Trim());
            }
            return returnList.ToArray();
        }

        public static string EnsureLastCharacterExists(this string @this, char character)
        {
            int indexOfCharacter = @this.IndexOf(character);
            if (indexOfCharacter == @this.Trim().Length-1)
                return @this;
            else
                return string.Format("{0}{1}", @this, character);
        }

        public static string InnerValueAfterLastChar(this string @this, char lastSearchChar, char lastChar) 
        {
            string returnValue = string.Empty;

            try 
            {
                var search = string.Format(@"\{0}", lastSearchChar);
                var lastSearchMatches = Regex.Matches(@this, search, RegexOptions.IgnoreCase);

                if (lastSearchMatches.Count <= 0)
                    return @this;

                var lastMatchIndex = lastSearchMatches[lastSearchMatches.Count - 1].Index;
                var indexOfLastChar = @this.IndexOf(lastChar, lastMatchIndex);

                if (indexOfLastChar > 0)
                    returnValue = @this.Substring(0, indexOfLastChar);
                else
                    return @this;
            }
            catch {
                throw;
            }

            return returnValue;
        }
    }
}