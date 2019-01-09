using ConfigToolLibrary2;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace ConfigToolV2
{
    class Program
    {
        static void Main(string[] args)
        {

            //SpellChecker.CheckSpelling();

            string excelFilePath = ConfigurationManager.AppSettings["ExcelFilePath"];
            string githubFilePath = String.Empty; //ConfigurationManager.AppSettings["GithubFilePath"];
            string headBranchName = ConfigurationManager.AppSettings["HeadBranchName"];
            string newBranchName = ConfigurationManager.AppSettings["NewBranchName"];
            string githubUserToken = ConfigurationManager.AppSettings["GithubUserToken"];
            string newSqlFilePath = ConfigurationManager.AppSettings["NewSQLFilePath"];
            string repositoryName = ConfigurationManager.AppSettings["RepositoryName"];

            ExcelHelper excelHelper = new ExcelHelper();
            MergeFile mFile = new MergeFile();

            //======================

            GithubHelper githubHelper = new GithubHelper(repositoryName, githubUserToken);
            excelHelper.LoadWorkBook(excelFilePath);
            List<string> excelSheetNames = excelHelper.GetAllWorkSheetNames();
            int i = 1;
            Console.WriteLine("Select Worksheet (Enter Number) : ");
            excelSheetNames.ForEach(x => Console.Write($"{i++}.{x}\n"));
            int sheetNumber = Convert.ToInt32(Console.ReadLine());
            string tableName = excelHelper.SelectWorkSheet(sheetNumber);

            //get file path from github
            githubFilePath = githubHelper.GetGithubFilePath(tableName, repositoryName).Result;

            List<string> contentGithubFile = githubHelper.GetContentOfFile(githubFilePath, headBranchName).Result;
            List<string> sql = githubHelper.GetColumnNames(contentGithubFile);
            List<string> excelCol = excelHelper.GetColumnNames();
            Dictionary<string, int> columnMappings = Common.GetColumnMappings(sql, excelCol);
            List<string> sqlFromExcel = excelHelper.GetSqlFromCurrentSheet(columnMappings);

            List<string> newFile = mFile.Merge(contentGithubFile, sqlFromExcel);
            File.WriteAllLines(newSqlFilePath, newFile);
            excelHelper.CloseExcel();

            //======================

            //=====Create PR
            var t = githubHelper.CreateBranch(headBranchName, newBranchName).Result;
            var t1 = githubHelper.UpdateFile(githubFilePath, string.Join("\n", newFile), newBranchName).Result;
            int prNumber = githubHelper.CreatePullRequest("New PR " + newBranchName, headBranchName, newBranchName).Result;
            int temp = githubHelper.AddReviewerToPullRequest(prNumber, new List<string>() { "ibrahimEv" }).Result;
            //================

            Console.WriteLine($"Success. File created at : {newSqlFilePath}");
            Console.ReadLine();

            #region OldCode
            //List<string> sqlFromExcel = excelHelper.ExcelOperations(excelFilePath);




            //var t2 =GithubHelper.CreateBranch2().Result;

            //headBranchName = "master";
            //newBranchName = "test_branch_11";
            //githubFilePath = "TestFile";
            //List<string> contentGithubFile = githubHelper.GetContentOfFile(githubFilePath, headBranchName).Result;
            //string updatedContent = string.Join("\n", contentGithubFile) + "\nUpdated file for branch : " + newBranchName;

            //var t =githubHelper.CreateBranch(headBranchName, newBranchName).Result;
            //var t1 = githubHelper.UpdateFile(githubFilePath, updatedContent, newBranchName).Result;
            //int prNumber = githubHelper.CreatePullRequest("New PR "+ newBranchName, headBranchName, newBranchName).Result;
            //int i = githubHelper.AddReviewerToPullRequest(prNumber,new List<string>(){ "ibrahimEv" }).Result;

            //List<string> contentGithubFile = GithubHelper.GetContentOfFile(135317263, githubFilePath, headBranchName, githubUserToken).Result;

            //List<string> newFile = mFile.Merge(contentGithubFile, sqlFromExcel);
            //File.WriteAllLines(newSqlFilePath, newFile);

            //Console.WriteLine($"Success. File created at : {newSqlFilePath}");

            //Console.ReadLine(); 
            #endregion
        }
    }
}
