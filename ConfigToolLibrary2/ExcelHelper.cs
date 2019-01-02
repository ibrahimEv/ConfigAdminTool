using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace ConfigToolLibrary2
{
    public class ExcelHelper
    {
        public Excel.Worksheet CurrentWorksheet { get; set; }
        public List<string> ExcelOperations(string excelPath)
        {
            Dictionary<string, Excel.Worksheet> temp = GetAllWorkSheet(excelPath);
            string worksheetName = GetWorksheetAndTableName(temp);
            Dictionary<string, int> test = GetColumnMapping(temp[worksheetName]);
            List<string> sql = GetSqlFromExcelSheet(temp[worksheetName], test);
            return sql;
            //Console.WriteLine(string.Join("\n", sql));
        }

        public Dictionary<string, Excel.Worksheet> GetAllWorkSheet(string excelPath)
        {
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Console.WriteLine("Reading excel...");
            Dictionary<string, Excel.Worksheet> dictSheetsName = new Dictionary<string, Excel.Worksheet>();
            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Open(excelPath, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            foreach (Excel.Worksheet worksheet in xlWorkBook.Worksheets)
            {
                dictSheetsName.Add(worksheet.Name, worksheet);
            }

            return dictSheetsName;
        }

        public Dictionary<string, int> GetColumnMapping(Excel.Worksheet worksheet)
        {
            Excel.Range range;
            //read first row for column
            range = worksheet.UsedRange;
            int cl = range.Columns.Count;
            List<string> colNames = new List<string>();
            Dictionary<string, int> colMapping = new Dictionary<string, int>();
            Console.WriteLine("Select column mapping: (Ignore - I, Change - C, NoChange - Enter)");
            for (int cCnt = 1; cCnt <= cl; cCnt++)
            {
                if ((range.Cells[1, cCnt] as Excel.Range).Value2 == null) break;

                string colName = (range.Cells[1, cCnt] as Excel.Range).Value2;
                Console.WriteLine($"Column Name : {colName}");
                var key = Console.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        //colNames.Add(colName);
                        colMapping.Add(colName, cCnt);
                        break;

                    case ConsoleKey.C:
                        Console.Write("Enter Column Name : ");
                        //colNames.Add(Console.ReadLine());
                        colName = Console.ReadLine() ?? "";
                        colMapping.Add(colName, cCnt);
                        break;

                    case ConsoleKey.I:
                        break;
                }

            }

            return colMapping;
        }

        public List<string> GetSqlFromExcelSheet(Excel.Worksheet worksheet, Dictionary<string, int> columnMapping)
        {
            Excel.Range range;
            int rw = 0;
            int cl = 0;
            //string sqlQuery = String.Empty;
            List<string> sqlQueries = new List<string>();

            range = worksheet.UsedRange;
            rw = range.Rows.Count;
            cl = range.Columns.Count;

            Console.WriteLine("Generating sql from excel");
            for (int rCnt = 2; rCnt <= rw; rCnt++)
            {
                string sqlQuery = "SELECT ";
                foreach (var col in columnMapping)
                {
                    sqlQuery += $"{(range.Cells[rCnt, col.Value] as Excel.Range).Value} AS {col.Key}, ";
                }
                sqlQuery = sqlQuery.TrimEnd(new[] { ',', ' ' });
                sqlQuery += " UNION ALL";
                sqlQueries.Add(sqlQuery);
            }

            return sqlQueries;
        }

        public string GetWorksheetAndTableName(Dictionary<string, Excel.Worksheet> worksheetsNames)
        {
            for (int i = 0; i < worksheetsNames.Count; i++)
            {
                Console.WriteLine($"Sheets found in file : {i}. {worksheetsNames.Keys.ElementAt(i)}");
            }
            Console.Write("Select sheet number : ");
            int num = Convert.ToInt32(Console.ReadLine());
            CurrentWorksheet = worksheetsNames.Values.ElementAt(num);
            return worksheetsNames.Keys.ElementAt(num);
        }

        public List<string> GetColumnNames()
        {
            Excel.Range range = CurrentWorksheet.UsedRange;
            //read first row for column
            int cl = range.Columns.Count;
            List<string> colNames = new List<string>();
            for (int cCnt = 1; cCnt <= cl; cCnt++)
            {
                if ((range.Cells[1, cCnt] as Excel.Range).Value2 == null) break;

                colNames.Add((range.Cells[1, cCnt] as Excel.Range).Value2);
            }

            return colNames;
        }
    }
}
