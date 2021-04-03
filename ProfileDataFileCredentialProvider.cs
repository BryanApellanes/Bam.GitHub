using Bam.Net;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github
{
    public class ProfileDataFileCredentialProvider : ICredentialProvider
    {
        public Credentials GetCredentials()
        {
            return new Credentials(BamProfile.ReadDataFile("github-api-token"));
        }
    }
}
