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
    public class GitHubIssueComment : ITrackedIssueComment
    {
        public GitHubIssueComment(IssueComment comment)
        {
            this.Comment = comment;
            this.CreatedAt = comment.CreatedAt;
            this.Text = comment.Body;
            this.UserInfo = new UserInfo(comment?.User);
        }

        public UserInfo UserInfo { get; set; }

        public string User 
        {
            get
            {
                return UserInfo.Email;
            }
            set
            {
                UserInfo.Email = value;
            }
        }

        public IssueComment Comment { get; set; }

        public DateTimeOffset CreatedAt { get; init; }
        public string Text { get; init; }

        public object ToCommentData()
        {
            return new CommentData()
            {                
                Text = Comment.Body,
                Created = Comment?.CreatedAt.UtcDateTime,
                CreatedBy = Comment.User?.Login
            };
        }
    }
}
