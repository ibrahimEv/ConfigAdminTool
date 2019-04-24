using System.Collections.Generic;

namespace ConfigToolLibrary2.Model
{
    public class FileDetail
    {
        public string TableName { get; set; }
        public string GithubFilePath { get; set; }
        public List<string> GithubFileContentList { get; set; }
        public List<string> MergedFileContentList { get; set; }
    }
}
