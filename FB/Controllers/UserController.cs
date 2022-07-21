using FB.Models;
using FB.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FB.Controllers
{
    public class UserController : Controller
    {
        DataContext db = new DataContext();
       
        //public static String idcook = System.Web.HttpContext.Current.User.Identity.Name;
        //public int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
        // GET: User
        [Authorize]
        public ActionResult Index()
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            List<Post> postlist = db.Posts.Where(p => p.poster_id == idcurr).ToList();
            ViewBag.image = db.Users.SingleOrDefault(u => u.id == idcurr).first_name;
            return View(postlist);
        }

        [Authorize]
        [HttpGet]
        public ActionResult CreatePost()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreatePost(Post post)
        {
            if(ModelState.IsValid)
            {
                post.poster_id = int.Parse(User.Identity.Name);
                post.time = DateTime.Now;
                db.Posts.Add(post);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
            
        }  

        [Authorize]
        [HttpGet]
        public ActionResult EditProfile()
        {
            int userId = int.Parse(User.Identity.Name);
            User user = db.Users.Where(u => u.id == userId).ToList()[0];
            user.confirmPassword = user.password;
            return View(user);
        }
        
        [HttpPost]
        public ActionResult EditProfile(User user, HttpPostedFileBase image)
        {
            //if validation is ok
            if (ModelState.IsValid)
            {
                //if email already exists in our database
                if (ExistingChecker.isEmailExistEdit(user.email))
                {
                    ModelState.AddModelError("EmailExist", "Email already taken!");
                    return View(user);
                }
                //if phone number already exists in our database
                if (ExistingChecker.isPhoneNumberExistEdit(user.phone_number))
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
                /*u.id = user.id;
                u.first_name = user.first_name;
                u.last_name = user.last_name;
                u.email = user.email;
                u.phone_number = user.phone_number;
                u.password = user.password;
                u.confirmPassword = user.confirmPassword;*/

                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);

        }

        [Authorize]
        [HttpGet]
        public ActionResult FriendesList()
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);

            List<Friend> friendeslist = db.Friends.Where(f => f.user1_id == idcurr || f.user2_id == idcurr).ToList();
            List<User> userslist = db.Users.ToList();
            //List<User> userslist = db.Users.Where(p => friendeslist.All(p2 => p2.user1_id == p.id || p2.user2_id == p.id && p.id != id)).ToList();
            List<User> userslist2 = userslist.Where(u => friendeslist.Any( f => f.user1_id == u.id || f.user2_id == u.id  ) && u.id != idcurr).ToList();
            //&& p.id != id && p.id != id
            return View(userslist2);
        }
        [Authorize]
        [HttpGet]
        public ActionResult MyProfile()
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            User user = db.Users.SingleOrDefault(u => u.id == idcurr);
            List<Friend> friendeslist = db.Friends.Where(f => f.user1_id == idcurr || f.user2_id == idcurr).ToList();
            List<User> userslist = db.Users.ToList();
            List<User> userslist2 = userslist.Where(u => friendeslist.Any(f => f.user1_id == u.id || f.user2_id == u.id)).ToList();
            ProfileViewModel profileviewmodel = new ProfileViewModel { user = user, friends = userslist2 };

            return View(profileviewmodel);

        }
        [Authorize]
        [HttpGet]

        public ActionResult ShowFriend(int? id)
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            if (id != null)
            {
                if (id == idcurr)
                    {
                    return RedirectToAction("MyProfile");
                }
                Friend friend = db.Friends.SingleOrDefault(f => (f.user1_id == id && f.user2_id == idcurr )|| (f.user1_id == idcurr && f.user2_id == id));
                if (friend == null)
                {
                    return RedirectToAction("ShowUser", new { id = id });
                }
                List<Post> postlist = db.Posts.Where(p => p.poster_id == id).ToList();
                User user = db.Users.SingleOrDefault(u => u.id == id);
                //ViewBag.user = db.Users.Where(u => u.id == id).First();
                List<Friend> friendeslist = db.Friends.Where(f => f.user1_id == idcurr || f.user2_id == idcurr).ToList();
                List<User> userslist = db.Users.ToList();
                //List<User> userslist = db.Users.Where(p => friendeslist.All(p2 => p2.user1_id == p.id || p2.user2_id == p.id && p.id != id)).ToList();
                List<User> userslist2 = userslist.Where(u => friendeslist.Any(f => f.user1_id == u.id || f.user2_id == u.id)).ToList();
                ProfileViewModel profileviewmodel = new ProfileViewModel { user = user, friends = userslist2 };
                return View(profileviewmodel);
            }
            else
                return RedirectToAction("FriendesList");

        }
        [Authorize]
        [HttpGet]

        public ActionResult ShowUser(int? id)
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            if (id != null)
            {
                if (id == idcurr)
                {
                    return RedirectToAction("MyProfile");
                }
                Friend friend = db.Friends.SingleOrDefault(f => (f.user1_id == id && f.user2_id == idcurr) || (f.user1_id == idcurr && f.user2_id == id));
                
                List<Post> postlist = db.Posts.Where(p => p.poster_id == id).ToList();
                User user = db.Users.SingleOrDefault(u => u.id == id);
                if (friend == null)
                    return View(user);
                //ViewBag.user = db.Users.Where(u => u.id == id).First();
                else
                {
                    return RedirectToAction("ShowFriend", new { id = id });
                }
            }
            else
                return RedirectToAction("FriendesList");

        }
        [HttpPost]
        public ActionResult AddReact(int id , bool react)
        {
           
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            Post post = db.Posts.Single(p => p.id == id);

            React reactto = db.Reacts.SingleOrDefault(r => r.reacter_id == idcurr && r.post_id == id);
            if (reactto == null)
            {
                React reactobj = new React { post_id = id, reacter_id = idcurr };
                if (react)
                {
                    post.likes_count++;
                    //db.Reacts.Select(r=> r.post_id == id).
                    reactobj.is_like = ReactType.Like;
                }
                else
                {
                    post.dislikes_count++;
                    reactobj.is_like = ReactType.Dislike;
                }
                db.Entry(post).State = EntityState.Modified;
                db.Reacts.Add(reactobj);

                db.SaveChanges();
                return RedirectToAction("ShowFriend", new { id = post.poster_id });
            }
            else
            {
                db.Reacts.Remove(reactto);
                React reactobj = new React { post_id = id, reacter_id = idcurr };
                if (react)
                {
                    if (reactto.is_like == ReactType.Like)
                    {
                        //if (post.likes_count > 0)
                            post.likes_count--;
                    }
                    else
                    {
                        reactobj.is_like = ReactType.Like;
                        post.likes_count++;
                    //if (post.dislikes_count > 0)
                        post.dislikes_count--;
                        db.Reacts.Add(reactobj);
                    }

                    //db.Reacts.Select(r=> r.post_id == id).
                }
                else
                {
                    if (reactto.is_like == ReactType.Dislike)
                    {
                        //if (post.dislikes_count > 0)
                            post.dislikes_count--;
                    }
                    else
                    {
                        reactobj.is_like = ReactType.Dislike;
                        //if (post.likes_count > 0)
                        post.likes_count--;
                        post.dislikes_count++;
                        db.Reacts.Add(reactobj);
                    }

                    
                }
                //db.Entry(post).State = EntityState.Modified;

                
                db.SaveChanges();
                return RedirectToAction("ShowFriend", new { id = post.poster_id });
            }

        
        }
        [HttpPost]
        public ActionResult AddComment(int id , string text )
        {
            int idcurr = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            Post post = db.Posts.Single(p => p.id == id);
            comment commentobj = new comment { commenter_id = idcurr, comment_text = text, post_id = id ,time= DateTime.Now };
            post.comments_count++;
            db.Entry(post).State = EntityState.Modified;
            db.comments.Add(commentobj);
            db.SaveChanges();
            return RedirectToAction("ShowFriend", new { id = post.poster_id });

        }
    }
}