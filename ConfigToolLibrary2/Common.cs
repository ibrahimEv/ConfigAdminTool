using NLog;
using System.Collections.Generic;
using System.Linq;

namespace ConfigToolLibrary2
{
    public class Common
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static Dictionary<string, int> GetColumnMappings(List<string> columnNamesSqlWithType, List<string> columnNamesExcel)
        {
            List<string> columnNamesSql = columnNamesSqlWithType.Select(col => col.Substring(0, col.IndexOf(':'))).ToList();
            SpellChecker checker = new SpellChecker();
            checker.AddWordsToDictionary(columnNamesSqlWithType);
            Dictionary<string, int> columnMapping = new Dictionary<string, int>();

            for (int i = 0; i < columnNamesExcel.Count; i++)
            {
                if (columnNamesSqlWithType.Count > i && columnNamesExcel[i].Trim() == columnNamesSql[i].Trim())
                {
                    logger.Log(LogLevel.Debug, $"Excel column : {columnNamesExcel[i]} , sql column : {columnNamesSql[i]}");
                    columnMapping.Add(columnNamesSqlWithType[i].Trim(), i + 1);
                    checker.RemoveWordFromDictionary(columnNamesSql[i]);
                    columnNamesSqlWithType[i] = string.Empty;
                }
                else
                {
                    string spellCheck = checker.CheckAndGetSpelling(columnNamesExcel[i]);
                    if (!string.IsNullOrEmpty(spellCheck))
                    {
                        logger.Log(LogLevel.Debug, $"Excel column : {columnNamesExcel[i]} , sql column : {spellCheck}");
                        string colWithType = columnNamesSqlWithType.Single(col => col.StartsWith(spellCheck));
                        columnMapping.Add(colWithType, i + 1);
                        checker.RemoveWordFromDictionary(spellCheck);
                        columnNamesSqlWithType[columnNamesSqlWithType.IndexOf(colWithType)] = string.Empty;
                    }
                }
            }

            if (columnNamesSqlWithType.Any(cn => !string.IsNullOrEmpty(cn)))
            {
                logger.Log(LogLevel.Error, $"Error in column mapping, Column that could not be matched : {string.Join(",", columnNamesSqlWithType)}");
            }

            return columnMapping;
        }
    }

    public static class Constants
    {
        public const int UserAdminDataRepsitoryId = 135317263;
        public const int IdentifiDataRepsitoryId = 50869152;
        public const int TestRepsitoryId = 159133156;
    }
}
