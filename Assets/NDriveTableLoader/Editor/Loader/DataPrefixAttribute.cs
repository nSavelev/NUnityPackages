using System;
using System.Text.RegularExpressions;

namespace GoogleTableLoader
{
    public class DataPrefixAttribute : Attribute
    {
        public string DataRegexp { get; }
        private Regex _regex;

        public DataPrefixAttribute(string dataRegexp)
        {
            DataRegexp = dataRegexp;
            _regex = new Regex(dataRegexp, RegexOptions.Compiled);
        }

        public bool IsStaisfied(string text)
        {
            return _regex.IsMatch(text);
        }
    }
}