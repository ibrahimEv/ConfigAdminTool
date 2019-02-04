using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using System.Runtime.Serialization.Formatters.Binary;
using NLog;

namespace ConfigToolLibrary2
{
    public class MergeFile
    {
        private List<string> FinalFile { get; set; }
        private List<string> NewUpdatedSelects { get; set; }
        private List<string> ContainsSelect { get; set; }
        private List<string> NewAddedSelects { get; set; }
        private List<string> NewSqlFile { get; set; }
        private UtilClass Util { get; set; }
        private int PrimaryKey { get; set; }
        private Dictionary<string, IDictionary<string, object>> NewAddedObjects { get; set; }
        private Dictionary<string, IDictionary<string, object>> DefaultObject { get; set; }
        private Factory Factory { get; set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public MergeFile()
        {
            Util = new UtilClass();
            ContainsSelect = new List<string>();
            NewSqlFile = new List<string>();
            NewUpdatedSelects = new List<string>();
            NewAddedSelects = new List<string>();
            NewAddedObjects = new Dictionary<string, IDictionary<string, object>>();
            FinalFile = new List<string>();
            DefaultObject = new Dictionary<string, IDictionary<string, object>>
            {
                { "Default", new Dictionary<string, object>() }
            };
            Factory = new Factory();
        }

        public List<string> Merge(List<string> oldSqlFile, List<string> newFileChanges)
        {
            this.Reset();
            ContainsSelect = Util.GetSelectStatements(oldSqlFile);
            var oldSelectStatementsObjects = Factory.GetDynamicObjects(ContainsSelect);
            var newSelectStatementsObjects = Factory.GetDynamicObjects(newFileChanges, Factory.Flag);
            if (!oldSelectStatementsObjects.First().Key.Contains("X") && newSelectStatementsObjects.First().Key.Contains("X"))
            {
                logger.Log(LogLevel.Info, $"Excel File Contain Duplicate Key");
                throw new Exception("Excel File Contain Duplicate Key");
            }
            this.MakeDefaultObject(oldSelectStatementsObjects);
            this.GetPrimaryKey();
            NewAddedObjects = this.GetDeepCopy(newSelectStatementsObjects, NewAddedObjects);
            this.UpdateObjects(newSelectStatementsObjects, oldSelectStatementsObjects);
            this.AddNewObjects(oldSelectStatementsObjects);
            this.MakeCorrectionOfLines(oldSelectStatementsObjects);
            return this.CombineLines();
        }

        public List<string> CombineLines()
        {
            for (int i = 0; i < Util.WithoutSelect.Count; i++)
            {

                if (Util.WithoutSelect[i].Contains(Keywords.INSERT_INTO))
                {
                    FinalFile.Add(Util.WithoutSelect[i]);
                    FinalFile.Add("");
                    i++;
                    foreach (var updatedSelect in NewUpdatedSelects)
                    {
                        FinalFile.Add(updatedSelect);
                    }
                    FinalFile.Add("");
                    foreach (var addedSelect in NewAddedSelects)
                    {
                        FinalFile.Add(addedSelect);
                    }

                }
                FinalFile.Add(Util.WithoutSelect[i]);
            }

            var t = FinalFile.Select(x => x.Replace(Constants.ReplaceCharsForComma, ",")).ToList();
            return FinalFile.Select(x => x.Replace(Constants.ReplaceCharsForComma, ",")).ToList();
        }


        public void MakeDefaultObject(Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            var values = DefaultObject["Default"];
            foreach (var defaultValue in oldSelectStatementsObjects.Last().Value)
            {
                values.Add(defaultValue.Key, defaultValue.Value);
            }
        }

        public void GetPrimaryKey()
        {
            this.PrimaryKey = Convert.ToInt32(DefaultObject.First().Value.First().Value);
        }

        public Dictionary<string, IDictionary<string, object>> GetDeepCopy(Dictionary<string, IDictionary<string, object>> newSelectStatementsObjects,
            Dictionary<string, IDictionary<string, object>> newCollection)
        {
            foreach (var newSelect in newSelectStatementsObjects)
            {
                newCollection.Add(newSelect.Key, new Dictionary<string, object>());
                foreach (var xx in newSelect.Value)
                {
                    newCollection[newSelect.Key].Add(xx.Key, xx.Value);
                }

            }

            return newCollection;
        }

        /* public void UpdateObjects(Dictionary<string, IDictionary<string, object>> newSelectStatementsObjects,
             Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
         {
             var duplicateOldObj = new Dictionary<string, IDictionary<string, object>>();
             duplicateOldObj = this.GetDeepCopy(oldSelectStatementsObjects, duplicateOldObj);
             var comparer = StringComparison.OrdinalIgnoreCase;

             foreach (var newObject in newSelectStatementsObjects)
             {
                 foreach (var oldObject in duplicateOldObj)
                 {
                     if (newObject.Key == oldObject.Key)
                     {
                         var val = oldSelectStatementsObjects[newObject.Key];
                         foreach (var newValue in newObject.Value)
                         {
                             foreach (var oldObj in oldObject.Value)
                             {
                                 if (String.Equals(newValue.Key.Trim(), oldObj.Key.Trim(), comparer))
                                 {
                                     val[oldObj.Key] = newValue.Value;
                                 }

                             }

                         }

                         NewAddedObjects.Remove(oldObject.Key);
                     }
                 }
             }

         }*/
        public void UpdateObjects(Dictionary<string, IDictionary<string, object>> newSelectStatementsObjects,
            Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            var comparer = StringComparison.OrdinalIgnoreCase;

            foreach (var newObject in newSelectStatementsObjects)
            {
                foreach (var oldObject in oldSelectStatementsObjects)
                {
                    if (newObject.Key.Trim() == oldObject.Key.Trim())
                    {
                        var val = oldSelectStatementsObjects[oldObject.Key];
                        foreach (var newValue in newObject.Value)
                        {
                            foreach (var oldObj in oldObject.Value.ToList())
                            {
                                if (String.Equals(newValue.Key.Trim(), oldObj.Key.Trim(), comparer))
                                {
                                    val[oldObj.Key] = newValue.Value;
                                }

                            }

                        }

                        NewAddedObjects.Remove(newObject.Key);
                    }
                }
            }

        }

        public void AddNewObjects(Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            var duplicateObj = new Dictionary<string, IDictionary<string, object>>();
            duplicateObj = this.GetDeepCopy(DefaultObject, duplicateObj);
            var comparer = StringComparison.OrdinalIgnoreCase;
            foreach (var newAddedObject in NewAddedObjects)
            {
                foreach (var obj in newAddedObject.Value)
                {
                    if (obj.Value.ToString().Contains("PrimaryKey"))
                    {
                        foreach (var val in duplicateObj.First().Value)
                        {
                            if (String.Equals(val.Key.Trim(), obj.Key.Trim(), comparer))
                            {
                                DefaultObject.First().Value[val.Key] = $" {++PrimaryKey} ";
                            }

                        }
                    }
                    else
                    {
                        foreach (var val in duplicateObj.First().Value)
                        {
                            if (String.Equals(val.Key.Trim(), obj.Key.Trim(), comparer))
                            {
                                DefaultObject.First().Value[val.Key] = obj.Value;
                            }

                        }
                    }
                }
                if (!(oldSelectStatementsObjects.ContainsKey(DefaultObject.First().Key)))
                {
                    NewAddedSelects.Add(UtilClass.ConvertToString(DefaultObject.First().Value));
                }

            }

        }

        public void MakeCorrectionOfLines(Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            foreach (var oldSelectObject in oldSelectStatementsObjects)
            {
                NewUpdatedSelects.Add(UtilClass.ConvertToString(oldSelectObject.Value));
            }

            if (NewAddedSelects.Count > 0)
            {
                ContainsSelect[ContainsSelect.Count - 1] = ContainsSelect[ContainsSelect.Count - 1] + Keywords.UNION_ALL;
                NewAddedSelects[NewAddedSelects.Count - 1] = NewAddedSelects[NewAddedSelects.Count - 1].Replace(Keywords.UNION_ALL, "");
            }
            else
            {
                NewUpdatedSelects[NewUpdatedSelects.Count - 1] = NewUpdatedSelects[NewUpdatedSelects.Count - 1].Replace(Keywords.UNION_ALL, "");
            }

        }

        public void Reset()
        {
            this.ContainsSelect.Clear();
            this.Factory.Cnt = 0;
            this.Factory.Flag = false;
            this.FinalFile.Clear();
            this.NewAddedSelects.Clear();
            this.NewUpdatedSelects.Clear();
            this.PrimaryKey = 0;
            this.Util.ContainsSelect.Clear();
            //this.Util.AfterSelect.Clear();
            //this.Util.BeforeSelect.Clear();
            this.Util.WithoutSelect.Clear();
            this.ClearAll(this.DefaultObject);
            this.ClearAll(this.NewAddedObjects);
            this.NewAddedObjects.Clear();
        }

        public void ClearAll(Dictionary<string,IDictionary<string,object>> dictionaryObjs)
        {
            foreach (var obj in dictionaryObjs)
            {
                obj.Value.Clear();
            }
        }


    }

}
