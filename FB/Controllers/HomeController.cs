using FB.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace FB.Controllers
{
    public class HomeController : Controller
    {
        DataContext db = new DataContext();


        [HttpGet]
        public ActionResult Register()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("MyProfile", "User");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Register(User user, HttpPostedFileBase image)
        {
            bool Status = false;
            String Message = "";

            //if validation is ok
            if (ModelState.IsValid)
            {
                //if email already exists in our database
                if (ExistingChecker.isEmailExist(user.email))
                {
                    ModelState.AddModelError("EmailExist", "Email already taken!");
                    return View(user);
                }
                //if phone number already exists in our database
                if (ExistingChecker.isPhoneNumberExist(user.phone_number))
                {
                    ModelState.AddModelError("PhoneNumberExist", "Phone number already taken!");
                    return View(user);
                }
                if (image != null && image.ContentLength != 0)
                {
                    string ext = Path.GetExtension(image.FileName);
                    var exts = new[] { "jpg", "jpeg", "png" };
                    if (exts.Contains(ext.Substring(1)))
                    {
                        string imageGuid = Guid.NewGuid().ToString();
                        user.profile_picture = imageGuid + ext;
                        image.SaveAs(Server.MapPath($"~/uploads/profile_pictures/{user.profile_picture}"));
                    }
                    else
                    {
                        ModelState.AddModelError("NotValidImage", "File extention is not valid! try uplaod png,jpg or jpeg image");
                        return View(user);
                    }

                }
                //hash password
                user.password = Crypt.Hash(user.password);
                user.confirmPassword = Crypt.Hash(user.confirmPassword);

                //add user to database
                db.Users.Add(user);
                db.SaveChanges();
                //status true to let the view know that the registration is done
                Status = true;
                //go to login
                return RedirectToAction("Login");
            }
            else
            {
                Message = "Invalid Request";
            }
            ViewBag.Status = Status;
            ViewBag.Message = Message;
            return View(user);

        }


        [HttpGet]
        public ActionResult Login()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("MyProfile", "User");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl)
        {
            string Message = "";
            //if validation is ok
            if (ModelState.IsValid)
            {
                //if our database have entry with the entered email and password then should login
                login.password = Crypt.Hash(login.password);
                if (db.Users.Where(u => u.email == login.email && u.password == login.password).ToList().Count() == 1)
                {
                    //get id of the user to keep it in the cookie
                    int id = db.Users.Where(user => user.email == login.email).Select(user => user.id).ToList()[0];
                    // if remeber me checked make the session last one year otherwise last only 2 hours
                    int timeoutMins = login.rememberMe ? 525600 : 525600;
                    var ticket = new FormsAuthenticationTicket(id.ToString(), login.rememberMe, timeoutMins);
                    string encrypted = FormsAuthentication.Encrypt(ticket);
                    var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                    cookie.Expires = DateTime.Now.AddMinutes(timeoutMins);
                    cookie.HttpOnly = true;
                    Response.Cookies.Add(cookie);

                    if (Url.IsLocalUrl(ReturnUrl))
                    {
                        return Redirect(ReturnUrl);
                    }
                    return RedirectToAction("MyProfile", "User");
                }
                else
                {
                    Message = "Incorrect email or password";
                }
            }
            ViewBag.Message = Message;
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [Authorize]
        public ActionResult Search()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Search(string SearchName)
        {
            var UserId = int.Parse(User.Identity.Name);
            var result = db.Users.Where(a => (a.email.Contains(SearchName) ||
            a.phone_number.Contains(SearchName))&& a.id!=UserId).ToList();
            ViewBag.search = SearchName;
            return View(result);
        }
        //public ActionResult _Profile(int UserId)
        
            //User users = db.Users.Where(x => x.id == UserId).FirstOrDefault();

            //return RedirectToAction("ShowUser","User", new { id = UserId });
       
        [HttpPost]
        public ActionResult SendReq(int friId, int USerId  )
        {

            friend_requests friend_requests = new friend_requests();
            friend_requests.sender_id = USerId;
            friend_requests.reciver_id = friId;
            friend_requests.date = DateTime.Now;
            db.friend_requests.Add(friend_requests);
            db.SaveChanges();
            return RedirectToAction("ShowUser","User",new { id = friend_requests.reciver_id });

        }
        [HttpGet]
        public ActionResult Requests()
        {
            int UserId = int.Parse(User.Identity.Name);
            List<friend_requests> requests = db.friend_requests.Where(f => f.reciver_id == UserId).ToList();
            List<User> userslist = db.Users.ToList();
            List<User> users = db.Users.ToList().Where(u => requests.Any(r => r.reciver_id == UserId && u.id ==r.sender_id)).ToList();
            return View(users);
        }

        public ActionResult Accept(int UserId, int FrindId)
        {
            Friend Friend = new Friend();
            Friend.user1_id = UserId;
            Friend.user2_id = FrindId;
            Friend.date = DateTime.Now;
            db.Friends.Add(Friend);
            db.SaveChanges();

            friend_requests friend_requests = new friend_requests();
            var Drop = db.friend_requests.FirstOrDefault(x => x.reciver_id == UserId);
            db.friend_requests.Remove(Drop);
            db.SaveChanges();
            return RedirectToAction("ShowFriend", "User", new { id = FrindId });
        }
        public ActionResult Delete(int UserId, int FrindId)
        {
            friend_requests friend_requests = new friend_requests();
            friend_requests.reciver_id = UserId;
            friend_requests.sender_id = FrindId;
            friend_requests.date = DateTime.Now;
            var Drop = db.friend_requests.FirstOrDefault(x => x.reciver_id == UserId);
            db.friend_requests.Remove(Drop);
            db.SaveChanges();
            return RedirectToAction("ShowFriend", "User", new { id = FrindId });
        }
    }
}