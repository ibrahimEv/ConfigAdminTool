using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;

namespace ConfigToolLibrary2
{
    public class MergeFile
    {
        public List<string> DuplicateLatFile { get; set; }
        public List<string> NewFileChanges { get; set; }
        public List<string> NewSqlFile { get; set; }
        public Stopwatch TotalTime { get; set; }
        public Utils Util { get; set; }
        public IDictionary<string,object> LatestObject { get; set; }
        public int PrimaryKey { get; set; }
        public string OldSqlFileNextLine { get; set; }

        public MergeFile()
        {
            Util = new Utils();
            DuplicateLatFile = new List<string>();
            NewSqlFile = new List<string>();
            NewFileChanges = new List<string>();

        }

        public List<string> Merge(List<string> OldSqlFile, List<string> NewFileChanges)
        {
            foreach (var latLine in NewFileChanges)
            {
                DuplicateLatFile.Add(latLine);
            }


            Console.WriteLine();
            TotalTime = Stopwatch.StartNew();
            for (var index = 0; index < OldSqlFile.Count; index++)
            {
                foreach (var latLine in NewFileChanges)
                {
                    
                    if (latLine != Keywords.EMPTY_LINE && OldSqlFile[index] != Keywords.EMPTY_LINE)
                    {
                        OldSqlFileNextLine = index < OldSqlFile.Count - 1 ? OldSqlFile[index + 1] : Keywords.EMPTY_LINE;

                        if (Util.CompareString(OldSqlFile[index], OldSqlFileNextLine, latLine))
                        {
                            var Oldobj =Factory.GetDynamicObject(OldSqlFile[index]);
                            var Newobj = Factory.GetDynamicObject(latLine);
                            var latestObj =  Manipulator.GetLatestChanges(Oldobj, Newobj);
                            var NewLine =  Utils.ConvertToString(latestObj);

                            NewSqlFile.Add(NewLine);
                            
                            if (index < OldSqlFile.Count - 1) index++;
                             DuplicateLatFile.Remove(latLine);
                        }

                    }
                }

                if (index < OldSqlFile.Count)
                {
                    if (OldSqlFile[index].Contains(Keywords.BEGIN_TRY))
                    {
                        for (int s = NewSqlFile.Count - 1; s >= 0 && DuplicateLatFile.Count > 0; s--)
                        {
                            if (NewSqlFile[s].Contains(Keywords.SELECT) && !NewSqlFile[s].Contains(Keywords.UNION_ALL))
                            {
                                PrimaryKey = Convert.ToInt32(Utils.StringSplitter(NewSqlFile[s])[0]);
                                NewSqlFile[s] = NewSqlFile[s] + Keywords.UNION_ALL;
                                LatestObject = Factory.GetDynamicObject(NewSqlFile[s]);
                                break;
                            }
                        }
                        for (int x = 0; x < DuplicateLatFile.Count; x++)
                        {
                            if (DuplicateLatFile[x]!= Keywords.EMPTY_LINE && LatestObject != null)
                            {
                                PrimaryKey++;
                                DuplicateLatFile[x] = DuplicateLatFile[x].Replace(Keywords.SELECT, Keywords.SELECT + " " + PrimaryKey);
                                var AddNewobj = Factory.GetDynamicObject(DuplicateLatFile[x]);
                                var AddlatestObj = Manipulator.GetLatestChanges(LatestObject, AddNewobj);
                                string AddNewLine;
                                if (x==DuplicateLatFile.Count - 1)
                                {
                                     AddNewLine = Utils.ConvertToString(AddlatestObj);
                                    AddNewLine = AddNewLine.Replace(Keywords.UNION_ALL, "");
                                }
                                else
                                {
                                     AddNewLine = Utils.ConvertToString(AddlatestObj);
                                }
                              
                                NewSqlFile.Add(AddNewLine);
                            }
                                
                        }
                        NewSqlFile.Add(Keywords.EMPTY_LINE);
                    }
                }

                NewSqlFile.Add(OldSqlFile[index]);
                 Console.Write("\r{0}%",((index * 100) / OldSqlFile.Count)+1);


            }
            TotalTime.Stop();
            var time = TotalTime.ElapsedMilliseconds;
            Console.WriteLine();
            Console.WriteLine("Total Time Required  --  " + time + " milliseconds");
            Console.WriteLine("Total Time Required  --  " + time / 100 + " secs");
            return NewSqlFile;

        }
    }
}
