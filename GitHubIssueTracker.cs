using Bam.Net;
using Bam.Net.Caching;
using Bam.Net.Data.Repositories;
using Bam.Net.IssueTracking;
using Bam.Net.IssueTracking.Data;
using Bam.Net.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bam.Github
{
    public class GitHubIssueTracker : Loggable, IIssueTracker
    {
        private readonly GitHubClient _gitHubClient;

        private readonly IRepoListDescriptorProvider _repoListDescriptorProvider;
        private readonly ICredentialProvider _credentialProvider;
        private readonly IServiceLevelAgreementProvider _serviceLevelAgreementProvider;
        private readonly ILogger _logger;

        public GitHubIssueTracker(IRepoListDescriptorProvider repoListDescriptorProvider, ICredentialProvider credentialProvider, IServiceLevelAgreementProvider serviceLevelAgreementProvider, DaoRepository daoRepository, ILogger logger)
        {
            _repoListDescriptorProvider = repoListDescriptorProvider;
            _credentialProvider = credentialProvider;
            _serviceLevelAgreementProvider = serviceLevelAgreementProvider;
            _logger = logger;

            _gitHubClient = CreateClient();
            LocalDataRepository = daoRepository;
            CachingRepository = new CachingRepository(daoRepository, logger);
        }

        public DaoRepository LocalDataRepository { get; init; }
        public CachingRepository CachingRepository { get; init; }

        public async Task<List<ITrackedIssueComment>> GetAllCommentsAsync(ITrackedIssue managedIssue)
        {
            if (!(managedIssue is GitHubIssue githubIssue))
            {
                throw new InvalidOperationException("specified issue must be a GitHubIssue");
            }

            List<ITrackedIssueComment> results = new List<ITrackedIssueComment>();
            IReadOnlyList<IssueComment> comments = await _gitHubClient.Issue.Comment.GetAllForIssue(githubIssue.RepositoryId.Value, githubIssue.Id.Value);
            try
            {                
                foreach(IssueComment comment in comments)
                {
                    results.Add(new GitHubIssueComment(comment));
                }
            }
            catch (Exception ex)
            {
                _logger.AddEntry("Error getting comments for issue ({0})", ex, managedIssue?.Id?.ToString() ?? string.Empty);
            }
            TrySetRateLimitInfo();
            return results;
        }

        public async Task<List<ITrackedIssue>> GetAllIssuesAsync()
        {
            OwnedRepoListData ownedRepoList = _repoListDescriptorProvider.GetRepoListDescriptor();

            List<ITrackedIssue> results = new List<ITrackedIssue>();
            foreach (string repo in ownedRepoList.Repositories)
            {
                Octokit.Repository repository = _gitHubClient.Repository.Get(ownedRepoList.Owner, repo).Result;
                try
                {
                    IReadOnlyList<Octokit.Issue> issues = await _gitHubClient.Issue.GetAllForRepository(repository.Id);
                    if (issues != null)
                    {
                        foreach (Octokit.Issue issue in issues)
                        {
                            GitHubIssue ghIssue = new GitHubIssue(issue, repository.Id);
                            ghIssue.Comments = await GetAllCommentsAsync(ghIssue);
                            results.Add(ghIssue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.AddEntry("Error getting issues for repo {0}", ex, repo);
                }
            }
            TrySetRateLimitInfo();
            return results;
        }

        public async Task<ITrackedIssue> CreateIssueAsync(long repositoryId, string title, string body)
        {
            try
            {
                NewIssue newIssue = new NewIssue(title) { Body = body };
                Issue issue = await _gitHubClient.Issue.Create(repositoryId, newIssue);
                return new GitHubIssue(issue, repositoryId);
            }
            catch (Exception ex)
            {
                _logger.AddEntry("Error creating issue \"{0}\" ({1}): {2}", ex, title, body);
            }
            return null;
        }

        public async Task<ITrackedIssueComment> AddCommentAsync(ITrackedIssue issue, string commentText)
        {
            try
            {
                IssueComment issuecomment = await _gitHubClient.Issue.Comment.Create(issue.RepositoryId.Value, issue.Id.Value, commentText);
                return new GitHubIssueComment(issuecomment);
            }
            catch (Exception ex)
            {
                _logger.AddEntry("Error adding comment for issue {0} ({1})", ex, issue?.Id?.ToString() ?? "[null]", issue?.Title ?? "[null]");
            }
            return null;
        }

        public ApiInfo ApiInfo { get; private set; }

        public int? HowManyRequestsICanMakePerHour { get; private set; }

        public int? HowManyReuestsIHaveLeft { get; private set; }

        public DateTimeOffset? WhenLimitWillReset{ get; private set; }

        private GitHubClient CreateClient()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("Okta-DevEx"))
            {
                Credentials = _credentialProvider.GetCredentials()
            };
            return client;
        }

        private void TrySetRateLimitInfo()
        {
            try
            {
                ApiInfo = _gitHubClient.GetLastApiInfo();
                // Prior to first API call, this will be null, because it only deals with the last call.

                // If the ApiInfo isn't null, there will be a property called RateLimit
                var rateLimit = ApiInfo?.RateLimit;

                HowManyRequestsICanMakePerHour = rateLimit?.Limit;
                HowManyReuestsIHaveLeft = rateLimit?.Remaining;
                WhenLimitWillReset = rateLimit?.Reset; // UTC time
            }
            catch (Exception ex)
            {
                _logger.AddEntry("Error setting rate limit info: {0}", ex, ex.Message);
            }
        }
    }
}
