using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Able.Models
{
    public class UserLogin
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string LoginType { get; set; }
    }
}