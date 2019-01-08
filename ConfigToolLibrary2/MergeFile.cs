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
        public List<string> DuplicateLatFile { get; set; }
        public List<string> NewFileChanges { get; set; }
        public List<string> FinalFile { get; set; }
        public List<string> NewUpdated { get; set; }
        public List<string> ContainsSelect { get; set; }
        public List<string> NewAdded { get; set; }
        public List<string> NewSqlFile { get; set; }
        public UtilClass Util { get; set; }
        public int PrimaryKey { get; set; }
        public Dictionary<string, IDictionary<string, object>> Duplicate { get; set; }
        public Dictionary<string, IDictionary<string, object>> DefaultObject { get; set; }

        public MergeFile()
        {
            Util = new UtilClass();
            DuplicateLatFile = new List<string>();
            NewSqlFile = new List<string>();
            NewFileChanges = new List<string>();
            NewUpdated = new List<string>();
            NewAdded = new List<string>();
            Duplicate = new Dictionary<string, IDictionary<string, object>>();
            FinalFile = new List<string>();
            DefaultObject = new Dictionary<string, IDictionary<string, object>>();
            DefaultObject.Add("Default",new Dictionary<string, object>());

        }

        public List<string> Merge(List<string> OldSqlFile, List<string> NewFileChanges)
        {
                DuplicateLatFile = ObjectExtension.CopyObject<List<string>>(NewFileChanges);
               
                    ContainsSelect = Util.GetSelectStatements(OldSqlFile);
                var OldSelectStatementsObjects = Factory.GetDynamicObjects(ContainsSelect);
                var NewSelectStatementsObjects = Factory.GetDynamicObjects(NewFileChanges);
                this.MakeDefaultObject(OldSelectStatementsObjects);
                this.GetPrimaryKey();
                this.GetDeepCopy(NewSelectStatementsObjects);
                this.UpdateObjects(NewSelectStatementsObjects,OldSelectStatementsObjects);
                this.AddNewObjects(OldSelectStatementsObjects);
                this.MakeCorrectionOfLines(OldSelectStatementsObjects);
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
                    foreach (var Select in NewUpdated)
                    {
                        FinalFile.Add(Select);
                    }
                    FinalFile.Add("");
                    foreach (var NewSelect in NewAdded)
                    {
                        FinalFile.Add(NewSelect);
                    }

                }
                FinalFile.Add(Util.WithoutSelect[i]);
            }

            return FinalFile;
        }

          
        public void MakeDefaultObject(Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            var x = DefaultObject["Default"];
            foreach (var obj in oldSelectStatementsObjects.Last().Value)
            {
                x.Add(obj.Key, obj.Value);
            }

        }

        public void GetPrimaryKey()
        {
           this.PrimaryKey = Convert.ToInt32(DefaultObject.First().Value.First().Value);
        }

        public void GetDeepCopy(Dictionary<string, IDictionary<string, object>> newSelectStatementsObjects)
        {
            foreach (var obj in newSelectStatementsObjects)
            {
                Duplicate.Add(obj.Key, obj.Value);
            }
        }

        public void UpdateObjects(Dictionary<string,IDictionary<string,object>> newSelectStatementsObjects,
            Dictionary<string, IDictionary<string, object>> oldSelectStatementsObjects)
        {
            foreach (var NewObject in newSelectStatementsObjects)
            {
                foreach (var OldObject in oldSelectStatementsObjects)
                {
                    if (NewObject.Key == OldObject.Key)
                    {
                        foreach (var newValue in NewObject.Value)
                        {
                            OldObject.Value[newValue.Key] = newValue.Value;
                        }

                        Duplicate.Remove(OldObject.Key);
                    }
                }
            }

        }

        public void AddNewObjects(Dictionary<string,IDictionary<string,object>> oldSelectStatementsObjects)
        {
            foreach (var AddedLine in Duplicate)
            {
                foreach (var obj in AddedLine.Value)
                {
                    if (obj.Value == (object)"PrimaryKey")
                    {
                        DefaultObject.First().Value[obj.Key] = ++PrimaryKey;
                    }
                    else
                    {
                        DefaultObject.First().Value[obj.Key] = obj.Value;
                    }
                }
                if (!(oldSelectStatementsObjects.ContainsKey(DefaultObject.First().Key)))
                {
                    NewAdded.Add(UtilClass.ConvertToString(DefaultObject.First().Value));
                }

            }

        }

        public void MakeCorrectionOfLines(Dictionary<string,IDictionary<string,object>> oldSelectStatementsObjects)
        {
            foreach (var OldObject in oldSelectStatementsObjects)
            {
                NewUpdated.Add(UtilClass.ConvertToString(OldObject.Value));
            }

            if (NewAdded.Count > 0)
            {
                ContainsSelect[ContainsSelect.Count - 1] = ContainsSelect[ContainsSelect.Count - 1] + Keywords.UNION_ALL;
                NewAdded[NewAdded.Count - 1] = NewAdded[NewAdded.Count - 1].Replace(Keywords.UNION_ALL, "");
            }
            else
            {
                NewUpdated[NewUpdated.Count - 1] = NewUpdated[NewUpdated.Count - 1].Replace(Keywords.UNION_ALL, "");
            }

        }

       
    }
    
}
