using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public class UtilClass
    {
        public List<string> ContainsSelect { get; set; }
        public List<string> BeforeSelect { get; set; }
        public List<string> AfterSelect { get; set; }
        public List<string> WithoutSelect { get; set; }
        public string InitialPartOfLine;

        public UtilClass()
        {
            ContainsSelect = new List<string>();
            WithoutSelect = new List<string>();
        }

        public List<string> GetSelectStatements(List<string> AllLines)
        {
            foreach (var line in AllLines)
            {
                var Words = line.Split(',',' ');
                if (Words[0].Equals(Keywords.SELECT))
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
                newLine = newLine + " " + obj.Value + " AS " + obj.Key + ",";
            }

            newLine = newLine.TrimEnd(',');
            newLine = newLine + " " + Keywords.UNION_ALL;
            return newLine;
        }

        public static List<string> StringSplitter(string line)
        {
            string[] separatingChars = { "SELECT", "AS", "UNION", "ALL", " ", "," };
            return line.Split(separatingChars, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
