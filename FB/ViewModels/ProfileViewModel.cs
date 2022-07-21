using FB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FB.ViewModels
{
    public class ProfileViewModel
    {
        public User user { get; set; }
        public List<User> friends { get; set; }
    }
}