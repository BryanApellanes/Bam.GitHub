﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bam.GitHub;
using Bam.Net.CommandLine;
using Octokit;

namespace Bam.Net.System.ConsoleActions
{
    public class IssuesAndPullRequests
    {
        private static readonly GitHubClient _gitHubClient = CreateClient();
        
        [ConsoleAction]
        public void ShowRateLimit()
        {
            ApiInfo apiInfo = _gitHubClient.GetLastApiInfo();
            // Prior to first API call, this will be null, because it only deals with the last call.

            // If the ApiInfo isn't null, there will be a property called RateLimit
            var rateLimit = apiInfo?.RateLimit;

            var howManyRequestsCanIMakePerHour = rateLimit?.Limit;
            var howManyRequestsDoIHaveLeft = rateLimit?.Remaining;
            var whenDoesTheLimitReset = rateLimit?.Reset; // UTC time
            Message.PrintLine("Calls Per Hour Limit: {0}", ConsoleColor.Yellow, howManyRequestsCanIMakePerHour);
            Message.PrintLine("Calls remaining: {0}", ConsoleColor.Yellow, howManyRequestsDoIHaveLeft);
            Message.PrintLine("Resets At: {0}", ConsoleColor.Yellow, whenDoesTheLimitReset.Value.DateTime.ToLocalTime());
        }
        
        [ConsoleAction]
        public void ListNewIssuesAndPullRequestsWithinLastEightDays()
        {
            string githubReposListFile = "github-repos.yml";
            
            RepoListDescriptor repoList = BamProfile.LoadYamlData<RepoListDescriptor>(githubReposListFile);

            foreach (string repo in repoList.Repositories)
            {
                Message.PrintLine("**** {0} ****", ConsoleColor.DarkCyan, repo);
                Repository repository = _gitHubClient.Repository.Get(repoList.Owner, repo).Result;
                IReadOnlyList<Issue> issues = _gitHubClient.Issue.GetAllForRepository(repository.Id).Result;
                PrintNewIssues(repo, issues);
                IReadOnlyList<PullRequest> pullRequests = _gitHubClient.PullRequest.GetAllForRepository(repository.Id).Result;
                PrintNewPullRequests(repo, pullRequests);
                Thread.Sleep(300);
            }
        }

        private void PrintNewPullRequests(string repo, IReadOnlyList<PullRequest> pullRequests)
        {
            Message.PrintLine("\t***** Pull Requests *****", ConsoleColor.White, repo);
            List<PullRequest> pullRequestList = pullRequests
                .Where(NewPullRequestPredicate).ToList();
            if (pullRequestList.Count != 0)
            {
                foreach (PullRequest issue in pullRequestList)
                {
                    Message.PrintLine("\t\t#{0}: Assigned to {1}: Created At {2}", ConsoleColor.White, 
                        issue.Number,
                        issue.Assignee?.Login ?? "(Not Assigned)",
                        FormatDate(issue.CreatedAt));
                    Message.PrintLine("\t\t\t{0}", ConsoleColor.White, issue.Title);
                }
            }
            else
            {
                Message.PrintLine("\t\tNo new pull requests for: {0}", ConsoleColor.Yellow, repo);
            }
        }
        
        private void PrintNewIssues(string repo, IReadOnlyList<Issue> issues)
        {
            Message.PrintLine("\t***** Issues *****", ConsoleColor.DarkBlue);
            List<Issue> issuesList = issues
                .Where(NewIssuePredicate).ToList();
            if (issuesList.Count != 0)
            {
                foreach (Issue issue in issuesList)
                {
                    Message.PrintLine("\t\t#{0}: Assigned to {1}: Created At {2}", ConsoleColor.DarkBlue, 
                        issue.Number,
                        issue.Assignee?.Login ?? "(Not Assigned)",
                        FormatDate(issue.CreatedAt));
                    Message.PrintLine("\t\t\t{0}", ConsoleColor.DarkBlue, issue.Title);
                }
            }
            else
            {
                Message.PrintLine("\t\tNo new issues for: {0}", ConsoleColor.Yellow, repo);
            }
        }
        private bool NewPullRequestPredicate(PullRequest pullRequest)
        {
            DateTime eightDaysAgo = DateTime.Now.Subtract(TimeSpan.FromDays(8));
            DateTime created = pullRequest.CreatedAt.DateTime;

            return created > eightDaysAgo;
        }
        
        private bool NewIssuePredicate(Issue issue)
        {
            DateTime eightDaysAgo = DateTime.Now.Subtract(TimeSpan.FromDays(8));
            DateTime created = issue.CreatedAt.DateTime;

            return created > eightDaysAgo;
        }
        
        private string FormatDate(DateTimeOffset value)
        {
            return value.ToLocalTime().ToString();
        }

        private static GitHubClient CreateClient()
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("Bam.GitHub"))
            {
                Credentials = new Credentials(BamProfile.ReadDataFile("github-api-token"))
            };
            return client;
        }
    }
}