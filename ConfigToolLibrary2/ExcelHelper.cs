﻿using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;

namespace ConfigToolLibrary2
{
    public class ExcelHelper
    {
        private Excel.Worksheet CurrentWorksheet { get; set; }
        private Dictionary<string, Excel.Worksheet> WorksheetsNames { get; set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private Excel.Application _xlApp = null;

        private Excel.Workbook _xlWorkBook = null;

        private Excel.Range _range = null;
        //private static Logger ActionLogger = LogManager.GetLogger("Execution");

        public void LoadWorkBook(string excelPath)
        {
            /*  Excel.Workbook xlWorkBook*/
            ;
            WorksheetsNames = new Dictionary<string, Excel.Worksheet>();
            try
            {
                _xlApp = new Excel.Application();
                logger.Log(LogLevel.Debug, $"Loading excel file : {excelPath}");
                _xlWorkBook = _xlApp.Workbooks.Open(excelPath, 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
                foreach (Excel.Worksheet worksheet in _xlWorkBook.Worksheets)
                {
                    WorksheetsNames.Add(worksheet.Name, worksheet);
                }

            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error loading excel : {excelPath} , exception message : {ex.Message}");
                throw new Exception("Error in loading excel file " + ex.Message);
            }
        }


        public List<string> GetAllWorkSheetNames()
        {
            return WorksheetsNames.Values.Select(w => w.Name).ToList();
        }

        public string SelectWorkSheet(int index)
        {
            try
            {
                CurrentWorksheet = WorksheetsNames.Values.ElementAt(index - 1);
                logger.Log(LogLevel.Info, $"Selected sheet: {CurrentWorksheet.Name}");
                return CurrentWorksheet.Name;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error selecting sheet at index : {index} , exception message : {ex.Message}");
                throw new Exception("Error in selecting worksheet at index " + index);
            }
        }

        public List<string> GetColumnNames()
        {
            try
            {
                _range = CurrentWorksheet.UsedRange;
                //read first row for column
                int cl = _range.Columns.Count;
                List<string> colNames = new List<string>();
                for (int cCnt = 1; cCnt <= cl; cCnt++)
                {
                    if ((_range.Cells[1, cCnt] as Excel.Range).Value2 == null) break;

                    colNames.Add((_range.Cells[1, cCnt] as Excel.Range).Value2);
                }

                return colNames;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error getting column names, exception message : {ex.Message}");
                throw new Exception("Error in getting column names of " + CurrentWorksheet.Name);
            }
        }

        public List<string> GetSqlFromCurrentSheet(Dictionary<string, int> columnMapping)
        {
            List<string> sqlQueries = new List<string>();
            try
            {
                string emptyQuery = "SELECT ";
                _range = CurrentWorksheet.UsedRange;
                int rw = _range.Rows.Count;
                emptyQuery += $" AS {columnMapping.ElementAt(0).Key.Substring(0, columnMapping.ElementAt(0).Key.IndexOf(':'))}, ";
                //To handle empty rows
                for (int i = 1; i < columnMapping.Count; i++)
                {
                    if (columnMapping.ElementAt(i).Key.ToLower().Contains("char"))
                    {
                        emptyQuery += $"' ' AS {columnMapping.ElementAt(i).Key.Substring(0, columnMapping.ElementAt(i).Key.IndexOf(':'))}, ";
                    }
                    else
                        emptyQuery += $"' ' AS {columnMapping.ElementAt(i).Key.Substring(0, columnMapping.ElementAt(i).Key.IndexOf(':'))}, ";
                }

                emptyQuery = emptyQuery.TrimEnd(new[] { ',', ' ' });
                emptyQuery += " UNION ALL";

                for (int rCnt = 2; rCnt <= rw; rCnt++)
                {
                    string rowText = string.Empty;
                    string sqlQuery = "SELECT ";
                    for (int i = 0; i < columnMapping.Count; i++)
                    {
                        var t = (_range.Rows[1] as Excel.Range).Text.ToString();
                        string cellValue = (_range.Cells[rCnt, columnMapping.ElementAt(i).Value] as Excel.Range).Text
                            .ToString();
                        rowText += cellValue;
                        if (columnMapping.ElementAt(i).Key.ToLower().Contains("char"))
                        {
                            cellValue = cellValue.Replace(",", Constants.ReplaceCharsForComma);
                            if (string.IsNullOrEmpty(cellValue) && i != 0) cellValue = " ";
                            sqlQuery +=
                                $"'{cellValue}' AS {columnMapping.ElementAt(i).Key.Substring(0, columnMapping.ElementAt(i).Key.IndexOf(':'))}, ";
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(cellValue) && i == 0) cellValue = "PrimaryKey" + rCnt;
                            if (string.IsNullOrEmpty(cellValue) && i != 0) cellValue = "' '";
                            sqlQuery += $"{cellValue} AS {columnMapping.ElementAt(i).Key.Substring(0, columnMapping.ElementAt(i).Key.IndexOf(':'))}, ";
                        }
                    }

                    sqlQuery = sqlQuery.TrimEnd(new[] { ',', ' ' });
                    sqlQuery += " UNION ALL";
                    if (!string.IsNullOrWhiteSpace(rowText))
                        sqlQueries.Add(sqlQuery);
                }

                sqlQueries.RemoveAll(q => q.Equals(emptyQuery));
                return sqlQueries;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error getting sql from sheet, exception message : {ex.Message}");
                throw new Exception("Error in generating sql from Excel for sheet : " + CurrentWorksheet.Name);
            }
        }

        public void CloseExcel()
        {
            if (_xlApp != null)
            {
                int hwnd = _xlApp.Application.Hwnd;
                TryKillProcessByMainWindowHwnd(hwnd);
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary> Tries to find and kill process by hWnd to the main window of the process.</summary>
        /// <param name="hWnd">Handle to the main window of the process.</param>
        /// <returns>True if process was found and killed. False if process was not found by hWnd or if it could not be killed.</returns>
        public static bool TryKillProcessByMainWindowHwnd(int hWnd)
        {
            uint processID;
            GetWindowThreadProcessId((IntPtr)hWnd, out processID);
            if (processID == 0) return false;
            try
            {
                Process.GetProcessById((int)processID).Kill();
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            return true;
        }

        /// <summary> Finds and kills process by hWnd to the main window of the process.</summary>
        /// <param name="hWnd">Handle to the main window of the process.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when process is not found by the hWnd parameter (the process is not running). 
        /// The identifier of the process might be expired.
        /// </exception>
        /// <exception cref="Win32Exception">See Process.Kill() exceptions documentation.</exception>
        /// <exception cref="NotSupportedException">See Process.Kill() exceptions documentation.</exception>
        /// <exception cref="InvalidOperationException">See Process.Kill() exceptions documentation.</exception>
        public static void KillProcessByMainWindowHwnd(int hWnd)
        {
            uint processID;
            GetWindowThreadProcessId((IntPtr)hWnd, out processID);
            if (processID == 0)
                throw new ArgumentException("Process has not been found by the given main window handle.", "hWnd");
            Process.GetProcessById((int)processID).Kill();
        }
        //==========================================================================//

        #region OldCode

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

        #endregion



    }
}
