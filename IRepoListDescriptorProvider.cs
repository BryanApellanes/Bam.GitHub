using Bam.Net.IssueTracking.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github
{
    public interface IRepoListDescriptorProvider 
    {
        OwnedRepoListData GetRepoListDescriptor();
    }
}
