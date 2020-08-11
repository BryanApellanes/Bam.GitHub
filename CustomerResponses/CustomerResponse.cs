namespace Bam.GitHub.CustomerResponses
{
    public class CustomerResponse
    {
        public CustomerResponse()
        {
            Text = "Thank you for reaching out, and I'm sorry you're seeing that error. We will review further internally to gain understanding as to the root of the issue. We'll provide updates here when there is more information to share.";
        }
        
        public string Text { get; set; }
        
        /// <summary>
        /// The id of the github issue or pull request
        /// </summary>
        public string GitHubItemId { get; set; }
    }
}