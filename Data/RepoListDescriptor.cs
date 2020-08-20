using Bam.Net.Data.Repositories;

namespace Bam.GitHub.Data
{
    public class RepoListDescriptor : CompositeKeyAuditRepoData
    {
        public string Owner { get; set; }
        public string[] Repositories { get; set; }
    }
}