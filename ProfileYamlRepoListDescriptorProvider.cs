using Bam.Net;
using Bam.Net.IssueTracking.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github
{
    public class ProfileYamlRepoListDescriptorProvider : IRepoListDescriptorProvider
    {
        public OwnedRepoListData GetRepoListDescriptor()
        {
            string githubReposListFile = "github-repos.yml";

            OwnedRepoListData ownedRepoList = BamProfile.LoadYamlData<OwnedRepoListData>(githubReposListFile);

            return ownedRepoList;
        }
    }
}
