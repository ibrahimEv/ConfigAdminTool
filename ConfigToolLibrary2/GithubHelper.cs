using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ConfigToolLibrary2.Model;
using NLog;

namespace ConfigToolLibrary2
{
    public class GithubHelper
    {
        private static GitHubClient _client;
        private static long _repositoryId;
        private string _shaHeadBranch;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static List<string> ColumnDefinitionList { get; set; }
        private List<GitHubFile> GitHubFiles { get; set; }
        private List<FileDetailLocal> FileDetailsLocal { get; set; }
        public GithubHelper(string repositoryName, string githubUserToken)
        {
            _repositoryId = GetRepositoryId(repositoryName);
            _client = new GitHubClient(new ProductHeaderValue("ConfigAdmin"))
            {
                Credentials = new Credentials(githubUserToken)
                //Credentials = new Credentials("mayuresh-EVH", "MyNameIsNeo_91", AuthenticationType.Basic)
            };
            ColumnDefinitionList = new List<string>();

        }
        public async Task<List<string>> GetContentOfFile(string filePath, string branchName)
        {
            try
            {
                logger.Log(LogLevel.Debug, $"Getting content from Github.");

                IReadOnlyList<RepositoryContent> response = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, filePath, branchName);
                string fileContent = response[0].Content;

                string tableDefinition = response[0].Content.Substring(fileContent.IndexOf("CREATE TABLE", StringComparison.Ordinal),
                    fileContent.IndexOf("INSERT", StringComparison.Ordinal) - fileContent.IndexOf("CREATE TABLE", StringComparison.Ordinal));
                LoadColumnDefinition(tableDefinition);


                var regex = new Regex("\'(.*?)\'");
                List<string> fileContentList = fileContent.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None).ToList();

                fileContentList = fileContentList.Select(x => regex.Replace(x, m => m.Value.Replace(",", Constants.ReplaceCharsForComma))).ToList();

                return fileContentList;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in GetContentOfFile, filePath : {filePath}, branchName : {branchName}. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<string> CreateBranch(string mainBranchName, string newBranchName)
        {
            try
            {
                Branch mainBranch = await _client.Repository.Branch.Get(_repositoryId, mainBranchName);
                _shaHeadBranch = mainBranch.Commit.Sha;

                logger.Log(LogLevel.Debug, $"Creating branch mainBranchName : {mainBranchName}, newBranchName : {newBranchName}");
                var ref1 = new NewReference("refs/heads/" + newBranchName, _shaHeadBranch);
                var branch = await _client.Git.Reference.Create(_repositoryId, ref1);
                return branch.Ref;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in CreateBranch, mainBranchName : {mainBranchName}, newBranchName : {newBranchName}. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<string> UpdateFile(string filePath, string updatedContent, string branchName)
        {
            try
            {
                IReadOnlyList<RepositoryContent> contentsResponse = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, filePath, branchName);
                string shaOfFile = contentsResponse[0].Sha;

                logger.Log(LogLevel.Debug, $"Updating file, filePath : {filePath}, branchName : {branchName}");
                var updateFileRequest = new UpdateFileRequest("Update File", updatedContent, shaOfFile, branchName);
                var updateFileResponse = await _client.Repository.Content.UpdateFile(_repositoryId, filePath, updateFileRequest);

                return updateFileResponse.Content.Name;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in UpdateFile, filePath : {filePath}, branchName : {branchName}. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<int> CreatePullRequest(string pullRequestTitle, string baseBranchName, string newBranchName)
        {
            try
            {
                var newPullRequest = new NewPullRequest(pullRequestTitle, newBranchName, baseBranchName);
                logger.Log(LogLevel.Debug, $"Creating pull request , pullRequestTitle : {pullRequestTitle}, baseBranchName : {baseBranchName}, newBranchName : {newBranchName}");
                var pullRequestResponse = await _client.PullRequest.Create(_repositoryId, newPullRequest);

                return pullRequestResponse.Number;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in CreatePullRequest, pullRequestTitle : {pullRequestTitle}, baseBranchName : {baseBranchName}, newBranchName : {newBranchName}. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<int> AddReviewerToPullRequest(int prNumber, List<string> reviewerList)
        {
            try
            {
                var prReview = new PullRequestReviewRequest(reviewerList);
                logger.Log(LogLevel.Debug, $"Adding Reviewer To Pull Request, prNumber : {prNumber}, reviewers : {string.Join(",", reviewerList)}");
                var t = await _client.PullRequest.ReviewRequest.Create(_repositoryId, prNumber, prReview);

                return t.Number;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in AddReviewerToPullRequest, prNumber : {prNumber}, reviewers : {string.Join(",", reviewerList)}. Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetGithubFilePath(string tableName, string repositoryName, bool isMergeFile = true)
        {
            try
            {
                SearchCodeRequest searchCodeRequest;
                if (repositoryName == "Test")
                    searchCodeRequest = new SearchCodeRequest(tableName, "mayuresh-evh", repositoryName);
                else
                    searchCodeRequest = new SearchCodeRequest(tableName, "Evolent-Health", repositoryName);
                logger.Log(LogLevel.Debug, $"Get Github FilePath, tableName : {tableName}, repositoryName : {repositoryName}");
                var responSearchCode = await _client.Search.SearchCode(searchCodeRequest);
                string path = String.Empty;
                //check for one or more file
                if (responSearchCode.TotalCount > 0)
                    if (isMergeFile)
                        path = responSearchCode.Items.Single(file =>
                            file.Name.Equals($"Merge_{tableName}.sql", StringComparison.OrdinalIgnoreCase)).Path;
                    else
                        path = responSearchCode.Items.Single(file => file.Name.Contains(tableName)).Path;
                return path;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in GetGithubFilePath, tableName : {tableName}, repositoryName : {repositoryName}. Exception : {ex.Message}");
                throw;
            }
        }

        public List<string> GetColumnNames(List<string> fileContent)
        {
            try
            {
                return ColumnDefinitionList;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in GetColumnNames. Exception : {ex.Message}");
                throw;
            }
        }
        public void LoadColumnDefinition(string tableDefinition)
        {
            try
            {
                List<string> columnsWithType = new List<string>();
                string temp = tableDefinition.Substring(tableDefinition.IndexOf('(') + 1, tableDefinition.LastIndexOf(')') - tableDefinition.IndexOf('('));
                string[] col = temp.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in col)
                {
                    string[] lineSeparated = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToArray();
                    columnsWithType.Add($"{lineSeparated[0].Trim('[', ']')}::{lineSeparated[1]}");
                }

                ColumnDefinitionList = columnsWithType;
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in LoadColumnDefinition. Exception : {ex.Message}");
                throw;
            }
        }

        public int GetRepositoryId(string repositoryName)
        {
            switch (repositoryName)
            {
                case "IdentifiData": return Constants.IdentifiDataRepsitoryId;
                case "user-admin-data": return Constants.UserAdminDataRepsitoryId;
                case "Test": return Constants.TestRepsitoryId;
            }

            throw new Exception($"Invalid repository name : {repositoryName}");
        }

        public async Task<List<string>> GetAllBranches()
        {
            try
            {
                IReadOnlyList<Branch> branches = await _client.Repository.Branch.GetAll(_repositoryId);

                logger.Log(LogLevel.Debug, $"Getting all branches for repository {_repositoryId}");

                return branches.Select(br => br.Name).ToList();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in GetAllBranches, for repository {_repositoryId} . Exception : {ex.Message}");
                throw;
            }
        }

        public async Task<List<string>> GetAllCollaborators()
        {
            try
            {
                IReadOnlyList<User> users = await _client.Repository.Collaborator.GetAll(_repositoryId);
                logger.Log(LogLevel.Debug, $"Getting all branches for repository {_repositoryId}");

                return users.Select(u => u.Login).ToList();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"Error in GetAllBranches, for repository {_repositoryId} . Exception : {ex.Message}");
                throw;
            }
        }

        //public async Task CreatePublishFile(string filePath, string branchName, IProgress<string> progress)
        //{
        //    GitHubFiles = new List<GitHubFile>();
        //    var t = ((Octokit.Connection)_client.Connection).GetLastApiInfo().RateLimit;

        //    await GetAllFilesPathForRepository(branchName);
        //    var t2 = ((Octokit.Connection)_client.Connection).GetLastApiInfo().RateLimit;
        //    IReadOnlyList<RepositoryContent> responseContent = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, filePath, branchName);
        //    List<string> sqlNameList = responseContent[0].Content.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        //    int count = 1;
        //    foreach (var sqlName in sqlNameList)
        //    {
        //        count++;
        //        if (string.IsNullOrWhiteSpace(sqlName)) continue;
        //        if (!sqlName.Contains('\\')) continue;
        //        string sqlName1 = sqlName.Substring(sqlName.LastIndexOf('\\') + 1);

        //        string sqlFilePath = GitHubFiles.Single(f => f.Name.Equals(sqlName1)).Path;
        //        IReadOnlyList<RepositoryContent> response = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, sqlFilePath, branchName);
        //        string fileContent = response[0].Content;

        //        File.AppendAllText(@"D:\PublishFile\test.txt", fileContent +"\n");

        //        progress.Report("Started " + sqlName1);
        //        if (count == 50) break;
        //    }
        //}

        //public async Task<string> GetGithubFilePathForFileName(string fileName, string repositoryName)
        //{
        //    try
        //    {
        //        SearchCodeRequest searchCodeRequest;
        //        if (repositoryName == "Test")
        //            searchCodeRequest = new SearchCodeRequest("Select", "mayuresh-evh", repositoryName);
        //        else
        //            searchCodeRequest = new SearchCodeRequest("Select", "Evolent-Health", repositoryName);

        //        //logger.Log(LogLevel.Debug, $"Get Github FilePath, tableName : {tableName}, repositoryName : {repositoryName}");

        //        searchCodeRequest.FileName = fileName;

        //        var responSearchCode = await _client.Search.SearchCode(searchCodeRequest);
        //        string path = String.Empty;
        //        //check for one or more file
        //        if (responSearchCode.TotalCount > 0)
        //            path = responSearchCode.Items.Single(file => file.Name.Equals(fileName)).Path;
        //        return path;
        //    }
        //    catch (Exception ex)
        //    {
        //        //logger.Log(LogLevel.Error, $"Error in GetGithubFilePath, tableName : {tableName}, repositoryName : {repositoryName}. Exception : {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<string> CreateBranch2()
        {

            var request = new SearchCodeRequest("testing", "mayuresh-EVH", "Test");
            var request1 = new SearchCodeRequest("BatchStatus", "Evolent-Health", "user-admin-data");
            var r = await _client.Search.SearchCode(request1);
            var t = await _client.Repository.Content.GetAllContents(159133156);
            //var t = await _client.Repository.Content.
            //var tokenAuth = new Credentials("677ce1644a694de2812bbe5c684251e8eff4f745");

            //var client = new GitHubClient(new ProductHeaderValue("Github"));
            //client.Credentials = tokenAuth;
            //var r1 = await _client.Repository.Collaborator.GetAll().Branch.Get("mayuresh-EVH", "Test", "master");
            //var branchName = "TestBranch5";
            //string sha = "4d7d2bb2edb08d1989996011dcfecf146d50f363";
            //var resp = await client.Connection.Post<string>(
            //    uri: new Uri($"https://api.github.com/repos/mayuresh-EVH/Test/git/refs"),
            //    body: $"{{\"ref\": \"refs/heads/{branchName}\", \"sha\": \"{sha}\"}}",
            //    accepts: "*/*",
            //    contentType: "application/json");
            //var statusCode = resp.HttpResponse.StatusCode;

            //IReadOnlyList<RepositoryContent> response = await client.Repository.Content.GetAllContentsByRef(159133156, "TestFile", branchName);

            //string text = response[0].Content;

            //text += "\nFor new branch";
            //var updateReq = new UpdateFileRequest("Update File", text, response[0].Sha, branchName);
            //var updatefile = await client.Repository.Content.UpdateFile(159133156, "TestFile", updateReq);

            //var req = new NewPullRequest("Test pull req", branchName, "master");
            //var pr = await client.PullRequest.Create(159133156, req);

            //var ref1 = new NewReference("refs/heads/" + branchName, sha);
            //var branch = await client.Git.Reference.Create(159133156, ref1);
            return "";


        }

        public async Task<string> GetContentOfFileTest(IProgress<int> progress)
        {
            try
            {
                logger.Log(LogLevel.Debug, $"Getting content from Github.");

                var request = new SearchCodeRequest("testing", "mayuresh-EVH", "Test");
                var r = await _client.Search.SearchCode(request);
                for (int i = 0; i < 100; i++)
                {
                    Thread.Sleep(100);
                    progress.Report(i);
                }
                return r.TotalCount.ToString();
                //string fileContent = response[0].Content;

                //string tableDefinition = response[0].Content.Substring(fileContent.IndexOf("CREATE TABLE", StringComparison.Ordinal),
                //    fileContent.IndexOf("INSERT", StringComparison.Ordinal) - fileContent.IndexOf("CREATE TABLE", StringComparison.Ordinal));
                //LoadColumnDefinition(tableDefinition);


                //var regex = new Regex("\'(.*?)\'");
                //List<string> fileContentList = fileContent.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None).ToList();

                //fileContentList = fileContentList.Select(x => regex.Replace(x, m => m.Value.Replace(",", Constants.ReplaceCharsForComma))).ToList();

                //return fileContentList;
            }
            catch (Exception)
            {
                //logger.Log(LogLevel.Error, $"Error in GetContentOfFile, filePath : {filePath}, branchName : {branchName}. Exception : {ex.Message}");
                throw;
            }
        }

        //public async Task GetAllFilesPathForRepository(string branchName, string dirPath = "")
        //{
        //    IReadOnlyList<RepositoryContent> filesContent;
        //    if (dirPath == "")
        //        filesContent = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, branchName);
        //    else
        //    {
        //        filesContent = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, dirPath, branchName);

        //    }
        //    foreach (var fileContent in filesContent)
        //    {
        //        if (fileContent.Type == ContentType.Dir)
        //        {
        //            await GetAllFilesPathForRepository(branchName, fileContent.Path);
        //            //var filesContent2 = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, fileContent.Path, branchName);

        //        }

        //        GitHubFiles.Add(new GitHubFile() { Name = fileContent.Name, Path = fileContent.Path });
        //    }
        //}

        public List<FileDetailLocal> GetAllSqlFilesForDirectory(string dirPath)
        {
            return Common.GetAllSqlFilesFromDir(dirPath);
        }

        public async Task CreatePublishFile2(PublishFileDetails publishFile, string branchName, List<FileDetail> fileDetails, IProgress<string> progress)
        {
            GitHubFiles = new List<GitHubFile>();
            FileDetailsLocal = GetAllSqlFilesForDirectory(publishFile.LocalRepoPath);

            if (File.Exists(publishFile.OutputFilePath)) File.Delete(publishFile.OutputFilePath);
            File.AppendAllText(publishFile.OutputFilePath, publishFile.DefaultContent + "\n");

            var t = ((Octokit.Connection)_client.Connection).GetLastApiInfo().RateLimit;

            IReadOnlyList<RepositoryContent> responseContent = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, publishFile.GithubFilePath, branchName);
            //remove all comments in publish file
            List<string> sqlNameList = responseContent[0].Content.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            int count = 1;
            try
            {
                foreach (var sqlName in sqlNameList)
                {
                    count++;
                    if (string.IsNullOrWhiteSpace(sqlName)) continue;
                    if (!sqlName.Contains('\\')) continue;
                    string sqlName1 = sqlName.Substring(sqlName.LastIndexOf('\\') + 1);
                    var test = FileDetailsLocal.Where(f => f.Name.Contains(sqlName1.Trim())).ToList();
                    string fileContent = string.Empty;
                    if (fileDetails.Any(fd =>
                        fd.GithubFilePath.Contains(sqlName.Substring(sqlName.IndexOf(@".\") + 2).Replace('\\', '/').Trim())))
                    {
                        var test1 = fileDetails.Single(fd =>
                            fd.GithubFilePath.Contains(sqlName.Substring(sqlName.IndexOf(@".\") + 2).Replace('\\', '/').Trim())).MergedFileContentList;
                        fileContent = string.Join("\n", test1);
                    }
                    else
                    {

                        string path = string.Empty;
                        if (test.Count > 1)
                        {
                            path = test.Single(f => f.Path.Contains(sqlName.Substring(sqlName.IndexOf(@".\") + 2).Trim())).Path;
                        }
                        else
                            path = FileDetailsLocal.Single(f => f.Name.Equals(sqlName1.Trim(), StringComparison.OrdinalIgnoreCase)).Path;

                        fileContent = File.ReadAllText(path);

                    }

                    File.AppendAllText(publishFile.OutputFilePath, fileContent + "\n");

                    progress.Report("Started " + sqlName1);
                    //Thread.Sleep(100);
                    //if (count == 100) break;
                }
            }
            catch (Exception )
            {

                throw;
            }
        }
    }
}
