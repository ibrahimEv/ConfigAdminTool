using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace ConfigToolLibrary2
{
    public class Common
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static Dictionary<string, int> GetColumnMappings(List<string> columnNamesSqlWithType, List<string> columnNamesExcel)
        {
            List<string> columnNamesSql = columnNamesSqlWithType.Select(col => col.Substring(0, col.IndexOf(':'))).ToList();
            SpellChecker checker = new SpellChecker();
            checker.AddWordsToDictionary(columnNamesSql);
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

        public static List<FileDetailLocal> GetAllSqlFilesFromDir(string dirPath)
        {
            List<FileDetailLocal> sqlFileDetails = Directory.GetFiles(dirPath, "*.sql", SearchOption.AllDirectories).Select(x => new FileDetailLocal()
            {
                Name = Path.GetFileName(x),
                Path = x
            }).ToList();
            return sqlFileDetails;
        }

        public static string GetRegistryKeyValue(string registryKey)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Config Admin POC\\Config Admin Automation"))
            {
                if (key != null)
                {
                    var value = key.GetValue(registryKey);
                    return value != null ? value.ToString() : string.Empty;
                }
            }
            return string.Empty;
        }

        public static void SetRegistryKeyValue(string registryKey, string value)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Wow6432Node\\Config Admin POC\\Config Admin Automation", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
            {
                if (key != null)
                {
                    key.SetValue(registryKey, value);
                }
                else throw new Exception("Registry path does not exists : Software\\Wow6432Node\\Config Admin POC\\Config Admin Automation");
            }
        }
    }

    public static class Constants
    {
        public const int UserAdminDataRepsitoryId = 135317263;
        public const int IdentifiDataRepsitoryId = 50869152;
        public const int UM2_0DataRepsitoryId = 143331528;
        public const int TestRepsitoryId = 159133156;
        public const string ReplaceCharsForComma = "#@$%";
    }

    public static class StringExtension
    {
        public static string ReplaceIgnoreCase(this string str, string oldValue, string newValue)
        {
            return Regex.Replace(str, oldValue, newValue, RegexOptions.IgnoreCase);
        }
    }
}
