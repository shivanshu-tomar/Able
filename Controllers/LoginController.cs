using Able.Models;
using GLAU_Exam.MyClass;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Able.Controllers
{
    public class LoginController : Controller
    {
        // GET: Login
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }
        public ActionResult SignUp(string firstName,string lastName,string password,string email)
        {
            string status = "Success";
            string hostName = Dns.GetHostName();
            string ip = Dns.GetHostByName(hostName).AddressList[0].ToString();
            string signUpQuery = "INSERT INTO `users` (`first_name`, `last_name`, `email`, `status`, `signup_on`, `signup_by`) VALUES ('" + firstName + "', '" + lastName + "', '" + email + "', 'active', now(), '" + ip + "')";
           
            MyConnections.DeleteInsertUpdate(signUpQuery, "able");
            string getUserIdQuery = "Select max(userId) 'id' from users";
            DataTable dt = MyConnections.Select(getUserIdQuery, "able");
            string userId = dt.Rows[0]["id"].ToString();
            string storePassword = "INSERT INTO `user_login` (`userId`, `password`) VALUES ('" + userId + "', '" + password + "')";
            MyConnections.DeleteInsertUpdate(storePassword, "able");
            return Json(new { status, userId }, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ContentResult LoginFunction(UserLogin dd)
        {
            string userid = dd.Username.Trim();
            string password = dd.Password;
            string myop = dd.LoginType;
            userid = userid.ToUpper().Trim();
            if (Export.IsAlphaNumeric(userid) == false) return Content("Sorry! You Have Entered Invalid User Id.");
            if (myop == "Login")
            {
                DataTable dtCheck = MyConnections.Select("SELECT A.userId,`password`,CONCAT(first_name,' ', last_name) 'Name' FROM `user_login` A JOIN users B on A.userId=B.UserId where A.userId=" + userid, "able");
                if (dtCheck.Rows.Count > 0)
                {
                    if (password == dtCheck.Rows[0]["Password"].ToString())
                    {
                        string hostName = Dns.GetHostName();
                        string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString();
                        string q = "INSERT INTO `login_logs` (`User_ID`, `Type`, `Login_On`, `From`) VALUES ('" + userid + "', 'user', now(), '" + myIP + "')";
                        MyConnections.DeleteInsertUpdate(q, "able");
                        // Add Permission Session
                        Session["id"] = userid;
                        Session["name"] = dtCheck.Rows[0]["Name"].ToString();
                        Session["KeepAlive"] = DateTime.Now;
                       
                         
                        
                        
                            Session["ImageUrl"] = "/bootstrapdash.com/demo/azia/v1.0.0/img/faces/default.jpg";
                        
                        return Content("Ok:/Dashboard/Index");
                    }
                    else
                    {
                        return Content("Sorry! Invalid User Id And Password!");
                    }

                }
                else
                {
                    return Content("Sorry! No Permission To Access This Portal");
                }
            }
            return Content("Sorry! Invalid Service Selected");
        }

    }
}