using NLog;
using System.Collections.Generic;
using System.Linq;

namespace ConfigToolLibrary2
{
    public class Common
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static Dictionary<string, int> GetColumnMappings(List<string> columnNamesSql, List<string> columnNamesExcel)
        {
            SpellChecker checker = new SpellChecker();
            checker.AddWordsToDictionary(columnNamesSql);
            Dictionary<string, int> columnMapping = new Dictionary<string, int>();

            for (int i = 0; i < columnNamesExcel.Count; i++)
            {
                if (columnNamesSql.Count > i && columnNamesExcel[i].Trim() == columnNamesSql[i].Trim())
                {
                    logger.Log(LogLevel.Debug, $"Excel column : {columnNamesExcel[i]} , sql column : {columnNamesSql[i]}");
                    columnMapping.Add(columnNamesSql[i].Trim(), i + 1);
                    checker.RemoveWordFromDictionary(columnNamesSql[i]);
                    columnNamesSql[i] = string.Empty;
                }
                else
                {
                    string spellCheck = checker.CheckAndGetSpelling(columnNamesExcel[i]);
                    if (!string.IsNullOrEmpty(spellCheck))
                    {
                        logger.Log(LogLevel.Debug, $"Excel column : {columnNamesExcel[i]} , sql column : {spellCheck}");
                        columnMapping.Add(spellCheck, i + 1);
                        checker.RemoveWordFromDictionary(spellCheck);
                        columnNamesSql[columnNamesSql.IndexOf(spellCheck)] = string.Empty;
                    }
                }
            }

            if (columnNamesSql.Any(cn => !string.IsNullOrEmpty(cn)))
            {
                logger.Log(LogLevel.Error, $"Error in column mapping, Column that could not be matched : {string.Join(",", columnNamesSql)}");
            }

            return columnMapping;
        }
    }

    public static class Constants
    {
        public const int UserAdminDataRepsitoryId = 135317263;
        public const int IdentifiDataRepsitoryId = 50869152;
    }
}
