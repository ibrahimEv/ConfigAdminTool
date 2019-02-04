using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
