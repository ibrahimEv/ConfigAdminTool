using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConfigToolLibrary2;

namespace ConfigToolV2
{
    class Program
    {
        static void Main(string[] args)
        {

            //SpellChecker.CheckSpelling();

            string excelFilePath = ConfigurationManager.AppSettings["ExcelFilePath"];
            string githubFilePath = ConfigurationManager.AppSettings["GithubFilePath"];
            string headBranchName = ConfigurationManager.AppSettings["HeadBranchName"];
            string newBranchName = ConfigurationManager.AppSettings["NewBranchName"];
            string githubUserToken = ConfigurationManager.AppSettings["GithubUserToken"];
            string newSqlFilePath = ConfigurationManager.AppSettings["NewSQLFilePath"];

            ExcelHelper excelHelper = new ExcelHelper();
            MergeFile mFile = new MergeFile();

            List<string> sqlFromExcel = excelHelper.ExcelOperations(excelFilePath);
            GithubHelper githubHelper = new GithubHelper(135317263, githubUserToken);

            List<string> contentGithubFile = githubHelper.GetContentOfFile(githubFilePath, headBranchName).Result;
        //    List<string> sql = githubHelper.GetColumnNames(contentGithubFile);
           // List<string> excelCol = excelHelper.GetColumnNames();
          //  Common.CompareColumnsOfSqlAndExcel(sql, excelCol);
                
            //var t2 =GithubHelper.CreateBranch2().Result;

            //headBranchName = "master";
            //newBranchName = "test_branch_11";
            //githubFilePath = "TestFile";
            //List<string> contentGithubFile = githubHelper.GetContentOfFile(githubFilePath, headBranchName).Result;
            string updatedContent = string.Join("\n", contentGithubFile) + "\nUpdated file for branch : " + newBranchName;

            //var t =githubHelper.CreateBranch(headBranchName, newBranchName).Result;
            //var t1 = githubHelper.UpdateFile(githubFilePath, updatedContent, newBranchName).Result;
            //int prNumber = githubHelper.CreatePullRequest("New PR "+ newBranchName, headBranchName, newBranchName).Result;
            //int i = githubHelper.AddReviewerToPullRequest(prNumber,new List<string>(){ "ibrahimEv" }).Result;

            //List<string> contentGithubFile = GithubHelper.GetContentOfFile(135317263, githubFilePath, headBranchName, githubUserToken).Result;

            List<string> newFile = mFile.Merge(contentGithubFile, sqlFromExcel);
            File.WriteAllLines(newSqlFilePath, newFile);

            Console.WriteLine($"Success. File created at : {newSqlFilePath}");

            Console.ReadLine();
        }
    }
}
