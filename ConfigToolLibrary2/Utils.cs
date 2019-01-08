using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public class Utils
    {
        private string[] PrevWords;
        private string[] LatWords;
        private  string[] PrevNextWords;
        public static string[] StringSplitter(string line)
        {
            string[] separatingChars = { "SELECT", "AS", ",", "UNION ALL" };
            return line.Split(separatingChars, StringSplitOptions.RemoveEmptyEntries).Select(l=>l.Trim()).ToArray();
        }

        public  bool CompareString(string PrevLine, string PrevNextLine, string LatLine)
        {
            if (!(PrevLine.Contains(Keywords.SELECT) && LatLine.Contains(Keywords.SELECT)))
            {
                return false;
            }
            PrevWords = StringSplitter(PrevLine);
            PrevNextWords = StringSplitter(PrevNextLine);
            LatWords = StringSplitter(LatLine);

            if (!PrevWords[0].Equals(LatWords[0]))
                return false;

            if (PrevWords.Length > 2)
            {
                if (PrevNextWords.Length > 3)
                {
                    if (PrevWords[1].Equals(PrevNextWords[1]))
                    {
                        if (PrevWords[1].Equals(LatWords[1]) && PrevWords[3].Equals(LatWords[3]))
                            return true;
                        return false;
                    }
                }
                if (PrevWords[1].Equals(LatWords[1]))
                    return true;
                return false;
            }
            return false;
        }

        public static string ConvertToString(IDictionary<string, object> latestObj)
        {
            string newLine= Keywords.SELECT;
            foreach (var obj in latestObj)
            {
                newLine = newLine + " " + obj.Value + " AS " + obj.Key+",";
            }

            newLine = newLine.TrimEnd(',');
            newLine = newLine +" "+ Keywords.UNION_ALL;
            return newLine;
        }
    }
}
