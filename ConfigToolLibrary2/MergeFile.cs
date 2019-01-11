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

namespace ConfigToolLibrary2
{
    public class MergeFile
    {
        public List<string> FinalFile { get; set; }
        public List<string> NewUpdatedSelects { get; set; }
        public List<string> ContainsSelect { get; set; }
        public List<string> NewAddedSelects { get; set; }
        public List<string> NewSqlFile { get; set; }
        public UtilClass Util { get; set; }
        public int PrimaryKey { get; set; }
        public Dictionary<string, IDictionary<string, object>> NewAddedObjects { get; set; }
        public Dictionary<string, IDictionary<string, object>> DefaultObject { get; set; }
        public Factory Factory { get; set; }

        public MergeFile()
        {
            Util = new UtilClass();
            NewSqlFile = new List<string>();
            NewUpdatedSelects = new List<string>();
            NewAddedSelects = new List<string>();
            NewAddedObjects = new Dictionary<string, IDictionary<string, object>>();
            FinalFile = new List<string>();
            DefaultObject = new Dictionary<string, IDictionary<string, object>>();
            DefaultObject.Add("Default", new Dictionary<string, object>());
            Factory = new Factory();
        }

        public List<string> Merge(List<string> oldSqlFile, List<string> newFileChanges)
        {
            ContainsSelect = Util.GetSelectStatements(oldSqlFile);
            var oldSelectStatementsObjects = Factory.GetDynamicObjects(ContainsSelect);
            var newSelectStatementsObjects = Factory.GetDynamicObjects(newFileChanges,  Factory.Flag);
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

            return FinalFile;//.Select(x=>x.Replace('#',',')).ToList();
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

        public void UpdateObjects(Dictionary<string, IDictionary<string, object>> newSelectStatementsObjects,
            Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            foreach (var newObject in newSelectStatementsObjects)
            {
                foreach (var oldObject in oldSelectStatementsObjects)
                {
                    if (newObject.Key == oldObject.Key)
                    {
                        foreach (var newValue in newObject.Value)
                        {
                            oldObject.Value[newValue.Key] = newValue.Value;
                        }

                        NewAddedObjects.Remove(oldObject.Key);
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
                            if (String.Equals(val.Key, obj.Key, comparer))
                            {
                                DefaultObject.First().Value[val.Key] = $" {++PrimaryKey} ";
                            }

                        }
                    }
                    else
                    {
                        foreach (var val in duplicateObj.First().Value)
                        {
                            if (String.Equals(val.Key, obj.Key, comparer))
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


    }

}
