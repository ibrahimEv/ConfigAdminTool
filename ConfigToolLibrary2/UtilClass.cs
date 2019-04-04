using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfigToolLibrary2
{
    public class UtilClass
    {
        public List<string> ContainsSelect { get; set; }
        public List<string> WithoutSelect { get; set; }
        public string InitialPartOfLine;

        public UtilClass()
        {
            ContainsSelect = new List<string>();
            WithoutSelect = new List<string>();
        }

        public List<string> GetSelectStatements(List<string> allLines)
        {
            foreach (var line in allLines)
            {
                var words = line.Split(',', ' ');
                if (words[0].Equals(Keywords.SELECT, StringComparison.OrdinalIgnoreCase))
                {
                    ContainsSelect.Add(line);
                }
                else
                {
                    WithoutSelect.Add(line);
                }
            }
            return ContainsSelect;
        }

        public static string ConvertToString(IDictionary<string, object> latestObj)
        {
            string newLine = Keywords.SELECT;
            foreach (var obj in latestObj)
            {
                newLine += $" {obj.Value.ToString().Trim()} AS {obj.Key.Trim()},";
            }

            newLine = newLine.TrimEnd(',');
            newLine += $" {Keywords.UNION_ALL}";
            return newLine;
        }

        public static List<string> StringSplitter(string line)
        {
            string[] separatingChars = { "SELECT", "AS", ",", "UNION ALL" };
            return line.Split(separatingChars, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }

        public static string RemoveComments(string code)
        {
            var re = @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/";
            return Regex.Replace(code, re, "$1");
        }
    }
}
