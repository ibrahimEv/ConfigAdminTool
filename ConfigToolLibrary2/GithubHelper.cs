using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConfigToolLibrary2
{
    public class GithubHelper
    {
        private static GitHubClient _client;
        private static long _repositoryId;
        private string _shaHeadBranch;
        public GithubHelper(long repositoryId, string githubUserToken)
        {
            _repositoryId = repositoryId;
            _client = new GitHubClient(new ProductHeaderValue("ConfigAdmin"))
            {
                Credentials = new Credentials(githubUserToken)
            };
        }
        public async Task<List<string>> GetContentOfFile(string filePath, string branchName)
        {
            Console.WriteLine("Getting content from Github.");

            IReadOnlyList<RepositoryContent> response = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, filePath, branchName);
            List<string> strList = response[0].Content.Split(new[] { "\n", "\r\n", "\r" }, StringSplitOptions.None).ToList();

            return strList;
        }

        public async Task<string> CreateBranch(string mainBranchName, string newBranchName)
        {
            Branch mainBranch = await _client.Repository.Branch.Get(_repositoryId, mainBranchName);
            _shaHeadBranch = mainBranch.Commit.Sha;

            var ref1 = new NewReference("refs/heads/" + newBranchName, _shaHeadBranch);
            var branch = await _client.Git.Reference.Create(_repositoryId, ref1);
            return branch.Ref;
        }

        public async Task<string> UpdateFile(string filePath, string updatedContent, string branchName)
        {
            //check if sha not null
            IReadOnlyList<RepositoryContent> contentsResponse = await _client.Repository.Content.GetAllContentsByRef(_repositoryId, filePath, branchName);
            string shaOfFile = contentsResponse[0].Sha;

            var updateFileRequest = new UpdateFileRequest("Update File", updatedContent, shaOfFile, branchName);
            var updateFileResponse = await _client.Repository.Content.UpdateFile(_repositoryId, filePath, updateFileRequest);

            return updateFileResponse.Content.Name;
        }

        public async Task<int> CreatePullRequest(string pullRequestTitle, string baseBranchName, string newBranchName)
        {
            var newPullRequest = new NewPullRequest(pullRequestTitle, newBranchName, baseBranchName);
            var pullRequestResponse = await _client.PullRequest.Create(_repositoryId, newPullRequest);

            return pullRequestResponse.Number;
        }

        public async Task<int> AddReviewerToPullRequest(int prNumber, List<string> reviewerList)
        {
            var prReview = new PullRequestReviewRequest(reviewerList);
            var t = await _client.PullRequest.ReviewRequest.Create(_repositoryId, prNumber, prReview);

            return t.Number;
        }

        public async Task<string> GetGithubFilePath(string tableName, string repositoryName)
        {
            var searchCodeRequest = new SearchCodeRequest(tableName, "Evolent-Health", repositoryName);
            var responSearchCode = await _client.Search.SearchCode(searchCodeRequest);
            string path = String.Empty;
            //check for one or more file
            if (responSearchCode.TotalCount > 0)
                path = responSearchCode.Items.Single(file=>file.Name.StartsWith("Merge")).Path;
            return path;
        }

        public List<string> GetColumnNames(List<string> fileContent)
        {
            string insertLine = fileContent.First(x => x.StartsWith("INSERT INTO"));
            insertLine = insertLine.Substring(insertLine.IndexOf('(')).Trim('(', ')');
            return insertLine.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        public static async Task<string> CreateBranch2()
        {

            var request = new SearchCodeRequest("testing", "mayuresh-EVH", "Test");
            var request1 = new SearchCodeRequest("BatchStatus", "Evolent-Health", "user-admin-data");
            var r = await _client.Search.SearchCode(request1);
            //var tokenAuth = new Credentials("677ce1644a694de2812bbe5c684251e8eff4f745");

            //var client = new GitHubClient(new ProductHeaderValue("Github"));
            //client.Credentials = tokenAuth;
            //var r = await client.Repository.Branch.Get("mayuresh-EVH", "Test", "master");
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
    }
}
