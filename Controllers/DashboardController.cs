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
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }
        public JsonResult getInitial()
        {
            string status;
            if (Session["id"]==null || Session["id"].ToString() == "")
            {
               status = "Session Expire";
                return Json(status, JsonRequestBehavior.AllowGet);
            }
            status = "Success";
            string getJobs = "SELECT * FROM `jobs_master`;";
            List<object> jobs = new List<object>();
            DataTable dt = MyConnections.Select(getJobs, "able");
            for(int i = 0; i < dt.Rows.Count; i++)
            {
                jobs.Add(new
                {
                    role = dt.Rows[i]["job_role"].ToString(),
                    discription = dt.Rows[i]["job_discription"].ToString(),
                    place = dt.Rows[i]["place_of_work"].ToString(),
                    contact = dt.Rows[i]["contact"].ToString()
                });
                
            }
            string getJobRoles = "SELECT `Value` FROM `other_values` where Type = 'JOBROLE' and `Status`='Active';";
            List<object> jobsRoles = new List<object>();
            dt = MyConnections.Select(getJobRoles, "able");
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                jobsRoles.Add(new
                {
                    role = dt.Rows[i]["Value"].ToString(),
                });

            }
            return Json(new { jobs, jobsRoles,status }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult saveJob(string role,string work,string discription,string number)
        {
            string hostName = Dns.GetHostName();
            string ip = Dns.GetHostByName(hostName).AddressList[0].ToString();
            string status = "Success";
            string saveJob = "INSERT INTO `jobs_master` (`job_role`, `job_discription`, `place_of_work`, `added_by`, `status`, `added_on`, `added_by_ip`,contact) VALUES ('" + role + "', '" + discription + "', '"+ work+ "', '" + Session["name"] +"', 'Active', now(), '" +  ip+ "', '" + number + "')";
            MyConnections.DeleteInsertUpdate(saveJob, "able");
            return Json(new { status }, JsonRequestBehavior.AllowGet);
        }
    }
}