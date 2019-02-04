using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolLibrary2.Model
{
    public class PublishFileDetails
    {
        public string GithubFilePath { get; set; }
        public string LocalRepoPath { get; set; }
        public string OutputFilePath { get; set; }
        public string DefaultContent { get; set; }
   
    }
}
