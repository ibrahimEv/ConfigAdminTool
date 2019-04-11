using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConfigToolLibrary2
{
    public class Factory
    {
        public bool Flag = false;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static string KeyGenerator(List<string> splitList, bool isCompositeKey)
        {
            if (!isCompositeKey)
            {
                return splitList[0].Split(new[] { " AS ", " as " }, StringSplitOptions.None)[0];
            }

            return splitList[0].Split(new[] { " AS ", " as " }, StringSplitOptions.None)[0] + "X" +
                   splitList[1].Split(new[] { " AS ", " as " }, StringSplitOptions.None)[0];

        }

        public Dictionary<string, IDictionary<string, object>> GetDynamicObjects(List<string> selectStatements, bool flag01 = false)
        {
            Dictionary<string, IDictionary<string, object>> listOfObjects = new Dictionary<string, IDictionary<string, object>>();
            try
            {
                for (var index = 0; index < selectStatements.Count; index++)
                {
                    var statement = selectStatements[index];
                    string sql = statement;
                    var obj = new ExpandoObject() as IDictionary<string, object>;
                    sql = sql.ReplaceIgnoreCase("SELECT ", " ");
                    sql = sql.ReplaceIgnoreCase("UNION ALL", string.Empty);

                    //Regex to match comma outside single quotes
                    Regex regx = new Regex(',' + "(?=(?:[^']*'[^']*')*(?![^']*'))");
                    string[] lines = regx.Split(sql);

                    //List<string> split = sql.Split(',').ToList();

                    //For primary key//

                    //Regex to match keyword AS outside quotes
                    regx = new Regex("(?<=^([^']|'[^']*')*) AS ", RegexOptions.IgnoreCase);
                    string[] strPropertyValueFirst = regx.Split(lines[0]).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                    var propValue = strPropertyValueFirst[0].Trim().Equals(string.Empty) ? "PrimaryKey" + index : strPropertyValueFirst[0].Trim();
                    obj.Add(strPropertyValueFirst[1].Trim(), $" {propValue} ");
                    ///////////////////

                    var key = KeyGenerator(lines.ToList(), flag01);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        //Regex to match keyword AS outside quotes
                        regx = new Regex("(?<=^([^']|'[^']*')*) AS ", RegexOptions.IgnoreCase);
                        string[] strPropertyValue = regx.Split(lines[i]).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                        //"AS" has no space in start
                        if (strPropertyValue.Length == 1)
                        {
                            int indexOfAs = lines[i].TrimEnd().LastIndexOf("AS ", StringComparison.OrdinalIgnoreCase);
                            string propertyValue = lines[i].Substring(0, indexOfAs);
                            string propertyName = lines[i].Substring(indexOfAs + 3);
                            obj.Add(propertyName.Trim(), $" {propertyValue.Trim()} ");
                        }

                        else if (strPropertyValue.Length == 3)
                            obj.Add(strPropertyValue[2].Trim(), $" {strPropertyValue[0].Trim()} ");
                        else
                            obj.Add(strPropertyValue[1].Trim(), $" {strPropertyValue[0].Trim()} ");
                    }

                    try
                    {
                        listOfObjects.Add(key, obj);
                    }
                    catch (Exception ex)
                    {
                        if (listOfObjects.First().Key.Contains("X"))
                        {
                            logger.Log(LogLevel.Info, $"File Does Not Contain Primary And Unique Key");
                            throw new Exception("File Does Not Contain Primary And Unique Key");
                        }

                        this.Flag = true;
                        listOfObjects.Clear();
                        return GetDynamicObjects(selectStatements, this.Flag);

                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Error creating dynamic object.");
            }
            var x = listOfObjects.Last().Value.Last();
            listOfObjects.Last().Value.Remove(x.Key);
            listOfObjects.Last().Value.Add(x.Key + " ", x.Value);

            return listOfObjects;

        }
    }
}
