using Bam.Net.IssueTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github.DevEx
{
    public class DevExServiceLevelAgreementProvider : IServiceLevelAgreementProvider
    {
        public int Sla { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool SlaWasMet(ITrackedIssue managedIssue)
        {
            throw new NotImplementedException();
        }
    }
}
