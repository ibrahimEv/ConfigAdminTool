using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public class Common
    {
        public static Dictionary<string, int> GetColumnMappings(List<string> columnNamesSql, List<string> columnNamesExcel)
        {
            SpellChecker checker = new SpellChecker();
            checker.AddWordsToDictionary(columnNamesSql);
            Dictionary<string, int> columnMapping = new Dictionary<string, int>();

            for (int i = 0; i < columnNamesExcel.Count; i++)
            {
                if (columnNamesSql.Count > i && columnNamesExcel[i].Trim() == columnNamesSql[i].Trim())
                {
                    columnMapping.Add(columnNamesSql[i].Trim(), i + 1);
                    checker.RemoveWordFromDictionary(columnNamesSql[i]);
                    columnNamesSql[i] = string.Empty;
                }
                else
                {
                    string spellCheck = checker.CheckAndGetSpelling(columnNamesExcel[i]);
                    if (!string.IsNullOrEmpty(spellCheck))
                    {
                        columnMapping.Add(spellCheck, i + 1);
                        checker.RemoveWordFromDictionary(spellCheck);
                        columnNamesSql[columnNamesSql.IndexOf(spellCheck)] = string.Empty;
                    }
                }
            }

            //for (int i = 0; i < columnNamesSql.Count; i++)
            //{
            //    if (columnNamesSql[i].Trim() == columnNamesExcel[i].Trim())
            //    {
            //        columnMapping.Add(columnNamesSql[i].Trim(), i + 1);
            //        checker.RemoveWordFromDictionary(columnNamesSql[i]);
            //        columnNamesSql[i] = string.Empty;
            //    }
            //    else
            //    {
            //        string spellCheck = checker.CheckAndGetSpelling(columnNamesExcel[i]);
            //        if (!string.IsNullOrEmpty(spellCheck))
            //        {
            //            columnMapping.Add(columnNamesSql[i].Trim(), i + 1);
            //            checker.RemoveWordFromDictionary(columnNamesSql[i]);
            //            columnNamesSql[i] = string.Empty;
            //        }
            //    }
            //}

            if (columnNamesSql.Any(cn => !string.IsNullOrEmpty(cn)))
            {
                throw new Exception($"Column could not be matched : {string.Join(",", columnNamesSql)}");
            }

            return columnMapping;
        }
    }
}
