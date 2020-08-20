﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bam.GitHub;
using Bam.GitHub.Data;
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
        public void ListOpenIssuesAndPullRequests()
        {
            string githubReposListFile = "github-repos.yml";
            
            RepoListDescriptor repoList = BamProfile.LoadYamlData<RepoListDescriptor>(githubReposListFile);
            
            foreach (string repo in repoList.Repositories)
            {
                Message.PrintLine("**** {0} ****", ConsoleColor.DarkCyan, repo);
                Repository repository = _gitHubClient.Repository.Get(repoList.Owner, repo).Result;
                IReadOnlyList<Issue> issues = _gitHubClient.Issue.GetAllForRepository(repository.Id).Result;
                PrintOpenIssues(repo, issues);
                IReadOnlyList<PullRequest> pullRequests = _gitHubClient.PullRequest.GetAllForRepository(repository.Id).Result;
                PrintOpenPullRequests(repo, pullRequests);
                Thread.Sleep(300);
            }
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

        private void PrintOpenPullRequests(string repo, IReadOnlyList<PullRequest> pullRequests)
        {
            PrintPullRequests(repo, pullRequests, OpenPullRequestPredicate);
        }
        
        private void PrintNewPullRequests(string repo, IReadOnlyList<PullRequest> pullRequests)
        {
            PrintPullRequests(repo, pullRequests, NewPullRequestPredicate);
        }
        
        private void PrintPullRequests(string repo, IReadOnlyList<PullRequest> pullRequests, Func<PullRequest, bool> pullRequestPredicate)
        {
            Message.PrintLine("\t***** Pull Requests *****", ConsoleColor.White, repo);
            List<PullRequest> pullRequestList = pullRequests
                .Where(pullRequestPredicate).ToList();
            if (pullRequestList.Count != 0)
            {
                foreach (PullRequest pullRequest in pullRequestList)
                {
                    Message.PrintLine("\t\t#{0}: Assigned to {1}: Created At {2}", ConsoleColor.White, 
                        pullRequest.Number,
                        pullRequest.Assignee?.Login ?? "(Not Assigned)",
                        FormatDate(pullRequest.CreatedAt));
                    Message.PrintLine("\t\t\t{0}", ConsoleColor.White, pullRequest.Title);
                    Message.PrintLine("\t\t{0}", ConsoleColor.DarkBlue, pullRequest.Url);
                    Message.PrintLine("\t\thuman url: {0}", ConsoleColor.DarkCyan, HumanUrl(pullRequest.Url));
                    Message.PrintLine("\t\t*** Labels ***", ConsoleColor.DarkYellow);
                    PrintLabels(pullRequest);
                }
            }
            else
            {
                Message.PrintLine("\t\tNo new pull requests for: {0}", ConsoleColor.Yellow, repo);
            }
        }

        private void PrintOpenIssues(string repo, IReadOnlyList<Issue> issues)
        {
            PrintIssues(repo, issues, OpenIssuePredicate);
        }
        
        private void PrintNewIssues(string repo, IReadOnlyList<Issue> issues)
        {
            PrintIssues(repo, issues, NewIssuePredicate);
        }
        
        private void PrintIssues(string repo, IReadOnlyList<Issue> issues, Func<Issue, bool> issuePredicate)
        {
            Message.PrintLine("\t***** Issues *****", ConsoleColor.DarkBlue);
            List<Issue> issuesList = issues
                .Where(issuePredicate).ToList();
            if (issuesList.Count != 0)
            {
                foreach (Issue issue in issuesList)
                {
                    Message.PrintLine("\t\t#{0}: Assigned to {1}: Created At {2}", ConsoleColor.DarkBlue, 
                        issue.Number,
                        issue.Assignee?.Login ?? "(Not Assigned)",
                        FormatDate(issue.CreatedAt));
                    Message.PrintLine("\t\t\t{0}", ConsoleColor.DarkBlue, issue.Title);
                    Message.PrintLine("\t\t{0}", ConsoleColor.DarkBlue, issue.Url);
                    Message.PrintLine("\t\thuman url: {0}", ConsoleColor.DarkCyan, HumanUrl(issue.Url));
                    Message.PrintLine("\t\t*** Labels ***", ConsoleColor.DarkYellow);
                    PrintLabels(issue);
                }
            }
            else
            {
                Message.PrintLine("\t\tNo new issues for: {0}", ConsoleColor.Yellow, repo);
            }
        }

        private string HumanUrl(string url)
        {
            Uri input = new Uri(url);
            string pathAndQuery = input.PathAndQuery.TruncateFront("/repos/".Length);
            return new Uri($"{input.Scheme}://github.com/{pathAndQuery}").ToString();
        }
        
        private void PrintLabels(PullRequest pullRequest)
        {
            PrintLabels(pullRequest.Labels);
        }

        private void PrintLabels(Issue issue)
        {
            PrintLabels(issue.Labels);
        }

        private void PrintLabels(IEnumerable<Label> labels)
        {
            if (labels.Count() == 0)
            {
                Message.PrintLine("\t\tNo labels", ConsoleColor.Yellow);
                return;
            }
            foreach (Label label in labels)
            {
                Message.PrintLine("\t\t{0}", ConsoleColor.DarkYellow, label.Name);
            }
        }

        private bool OpenPullRequestPredicate(PullRequest pullRequest)
        {
            return pullRequest.State == ItemState.Open;
        }
        
        private bool NewPullRequestPredicate(PullRequest pullRequest)
        {
            DateTime eightDaysAgo = DateTime.Now.Subtract(TimeSpan.FromDays(8));
            DateTime created = pullRequest.CreatedAt.DateTime;

            return created > eightDaysAgo;
        }

        private bool OpenIssuePredicate(Issue issue)
        {
            return issue.State == ItemState.Open;
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