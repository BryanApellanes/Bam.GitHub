using Bam.Net.IssueTracking;
using Bam.Net.IssueTracking.Data;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github
{
    public class GitHubIssue : ITrackedIssue
    {
        public GitHubIssue(Issue githubIssue, long? repoId = null)
        {
            this.Issue = githubIssue;
            this.Id = githubIssue.Number; // the number is what the external world knows github issues by, the githubIssue.Id is githubs internal identifier
            this.Title = githubIssue.Title;
            this.Body = githubIssue.Body;
            this.CreatedAt = githubIssue.CreatedAt;
            this.CommentCount = githubIssue.Comments;
            this.IssueUri = new Uri(githubIssue.Url);
            this.RepositoryId = githubIssue.Repository?.Id ?? repoId;
            this.User = new UserInfo { Login = githubIssue.User?.Login, Email = githubIssue.User?.Email };
        }

        public Issue Issue { get; init; }

        public UserInfo User { get; set; }
        
        public List<ITrackedIssueComment> Comments { get; set; }

        public int? Id { get; init; }

        public string Title { get; init; }

        public string Body { get; init; }

        public long? RepositoryId{ get; init; }

        public Uri IssueUri{ get; init; }
        public int CommentCount { get; init; }
        public DateTimeOffset CreatedAt { get; init; }

        public IssueData ToIssueData()
        {
            return new IssueData
            {
                CreatedBy = User.Login,
                Created = CreatedAt.UtcDateTime,
                Title = Title,
                Body = Body,
                CommentDatas = Comments.Select(comment => (CommentData)comment.ToCommentData()).ToList(),
            };
        }
    }
}
