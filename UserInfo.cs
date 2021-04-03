using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Github
{
    public class UserInfo
    {
        public UserInfo() { }
        public UserInfo(User user)
        {
            this.Login = user?.Login;
            this.Email = user?.Email;
        }

        public string Login{ get; set; }
        public string Email{ get; set; }
    }
}
