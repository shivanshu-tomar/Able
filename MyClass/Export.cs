using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;

namespace GLAU_Exam.MyClass
{
    public class Export
    {
        public static List<object> GetFilterList(string SystemType, string Type, string AnyCondition = "", string Code = "", string Sem = "", string Sec = "")
        {
            List<Object> DataList = new List<object>();
            DataTable D = new DataTable();
            switch (SystemType)
            {
                case "AMS":
                    switch (Type)
                    {
                        case "Course+Branch+Sem":
                            D = MyConnections.Select("SELECT Main,Tag,Course_Name,Branch,`Code`,Semester,MyYear,Type,ResultPart FROM `course` A,semtocourse B WHERE A.`Code`=B.Course_Code " + (Code != "" ? " And A.Code=" + Code : "") + (Sem != "" ? " And B.Semester='" + Sem + "'" : "") + " order by Main,Tag,MyYearNum,MySemNum;", "Student");
                            foreach (DataRow item in D.Rows)
                            {
                                DataList.Add(new
                                {
                                    Main = item["Main"],
                                    Tag = item["Tag"],
                                    Course_Name = item["Course_Name"],
                                    Branch = item["Branch"],
                                    Code = item["Code"],
                                    Semester = item["Semester"],
                                    MyYear = item["MyYear"],
                                    Type = item["Type"],
                                    ResultPart = item["ResultPart"],
                                });
                            }
                            break;
                        case "CurrentSes":
                            D = MyConnections.Select("SELECT CurSession from current_session", "Student");
                            foreach (DataRow item in D.Rows)
                            {
                                DataList.Add(new
                                {
                                    Ses = item["CurSession"],
                                });
                            }
                            break;
                        default: break;

                        case "Section":
                            D = MyConnections.Select("SELECT Section_ID 'Sec' from classadvisor_section where true " + (Code != "" ? " And SUBSTRING_INDEX(Section_ID,'#',1)=" + Code : "") + (Sem != "" ? " And SUBSTRING_INDEX(SUBSTRING_INDEX(Section_ID,'#',2),'#',-1)='" + Sem + "'" : "") + " ORDER BY  Section_ID;", "Student");
                            foreach (DataRow item in D.Rows)
                            {
                                DataList.Add(new
                                {
                                    Section = item["Sec"],
                                });
                            }
                            break;

                        case "Subject":
                            if (Sec != "")
                            {
                                D = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from employee_section_subject_mapping A, subject_master B where true " + (Sec != "" ? " And SUBSTRING_INDEX(Batch_ID,'#',3)='" + Sec + "'" : "") + " And A.Subject_Code=B.Subject_Code ORDER BY B.Subject_Code;", "Student");
                            }
                            else
                            {
                                D = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from employee_section_subject_mapping A, subject_master B where true " + (Code != "" ? " And SUBSTRING_INDEX(Batch_ID,'#',1)=" + Code : "") + (Sem != "" ? " And SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',2),'#',-1)='" + Sem + "'" : "") + " And A.Subject_Code=B.Subject_Code ORDER BY B.Subject_Code;", "Student");
                            }
                            foreach (DataRow item in D.Rows)
                            {
                                DataList.Add(new
                                {
                                    SubjectCode = item["Subject_Code"],
                                    SubjectName = item["Subject_Name"],
                                    Type = item["Type"],
                                });
                            }
                            break;                        
                    }
                    break;
                default: break;
            }

            return DataList;
        }

        internal static bool IsAlphaNumeric(string userid)
        {
            return true;
        }

        public static List<object> GetAttendance(string UserId,string SystemType, string Type, out List<object> SubList, string AnyCondition = "", string Code = "", string Sem = "", string Sec = "", string Sub = "",string RuleDisable="", string SDate = "", string EDate = "", string SId = "")
        {
            SubList = new List<object>();
            List<Object> DataList = new List<object>();
            DataTable D = new DataTable();
            DataTable S = new DataTable();
            DataTable A = new DataTable();
            
            string UseStatus = RuleDisable == "1" ? "`StatusOther`" : "`Status`";
            List<string> Batches = new List<string>();
            switch (SystemType)
            {                
                case "AMS":
                    switch (Type)
                    {
                        case "Overall Attendance":

                            if (Sub != "")
                            {
                                D = MyConnections.Select("SELECT A.*,DATE_FORMAT(B.RegOn, '%d.%m.%Y') 'RegOn' from(SELECT DISTINCT A.Student_ID, SUBSTRING_INDEX(SUBSTRING_INDEX(Register_ID, '#', 3), '#', -1) 'Section', Roll_NO, IFNULL(MRollNo, '---') 'UnivRNo', Student_FName, Father_Name,B.Status from student_master A, student_semester B, student_register C where A.Student_ID = B.Student_ID And B.Student_ID = C.Student_ID And " + (Sec != "" ? "Register_ID = '" + Sec + "#" + Sub + "'" : "Register_ID like '" + Code + "#" + Sem + "#%#" + Sub + "'") + "  ORDER BY SUBSTRING_INDEX(Register_ID, '#', 3), Roll_NO) A LEFT JOIN registrations B on A.Student_ID = B.ID;", "Student");
                                S = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from student_register A, subject_master B where SUBSTRING_INDEX(A.Register_ID,'#',-1)=B.Subject_Code And " + (Sec != "" ? "Register_ID = '" + Sec + "#" + Sub + "'" : "Register_ID like '" + Code + "#" + Sem + "#%#" + Sub + "'") + " ORDER BY B.Type DESC,B.Subject_Code ;", "Student");
                                A = MyConnections.Select("(SELECT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(A.Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(" + UseStatus + "!='L',Weight,0)) 'Held',SUM(IF(" + UseStatus + "!='L'," + UseStatus + ",0)) 'Attend',SUM(IF(" + UseStatus + "='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#" + Sub + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%#" + Sub + "#%'") + (SDate != "" ? " And Lecture_Date>='" + SDate + "'" : "") + (EDate != "" ? " And Lecture_Date<='" + EDate + "'" : "") + " GROUP BY B.Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(A.Batch_ID,'#',4),'#',-1))", "Attendance");

                            }
                            else
                            {
                                D = MyConnections.Select("SELECT A.*,DATE_FORMAT(B.RegOn, '%d.%m.%Y') 'RegOn' from(SELECT DISTINCT A.Student_ID, SUBSTRING_INDEX(Section_ID, '#', -1) 'Section', Roll_NO, IFNULL(MRollNo, '---') 'UnivRNo', Student_FName, Father_Name,B.Status from student_master A, student_semester B, section_master C where A.Student_ID = B.Student_ID And B.Student_ID = C.Student_ID And " + (Sec != "" ? "Section_ID = '" + Sec + "'" : "Section_ID like '" + Code + "#" + Sem + "#%'") + "  ORDER BY Section_ID, Roll_NO) A LEFT JOIN registrations B on A.Student_ID = B.ID;", "Student");
                                S = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from student_register A, subject_master B where SUBSTRING_INDEX(A.Register_ID,'#',-1)=B.Subject_Code And " + (Sec != "" ? "SUBSTRING_INDEX(Register_ID, '#', 3) = '" + Sec + "'" : "Register_ID like '" + Code + "#" + Sem + "#%'") + " ORDER BY B.Type DESC,B.Subject_Code ;", "Student");
                                A = MyConnections.Select("(SELECT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(A.Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(" + UseStatus + "!='L',Weight,0)) 'Held',SUM(IF(" + UseStatus + "!='L'," + UseStatus + ",0)) 'Attend',SUM(IF(" + UseStatus + "='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%'") + (SDate != "" ? " And Lecture_Date>='" + SDate + "'" : "") + (EDate != "" ? " And Lecture_Date<='" + EDate + "'" : "") + " GROUP BY B.Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(A.Batch_ID,'#',4),'#',-1))", "Attendance");
                            }
                            
                            foreach (DataRow item in D.Rows)
                            {
                                int TotalHeld = 0;
                                int TotalAttend = 0;
                                int TotalLeave = 0;

                                List<object> StuAtt = new List<object>();
                                if (A.Rows.Count > 0 && S.Rows.Count>0)
                                {
                                    foreach (DataRow sitem in S.Rows)
                                    {
                                        DataRow[] DRS = A.Select("Student_ID='" + item["Student_ID"] + "' And Subject='"+ sitem["Subject_Code"] + "'");
                                        if (DRS.Length > 0)
                                        {
                                            TotalHeld += Convert.ToInt32(DRS[0]["Held"]);
                                            TotalAttend += Convert.ToInt32(DRS[0]["Attend"]);
                                            TotalLeave += Convert.ToInt32(DRS[0]["Leave"]);

                                            StuAtt.Add(new
                                            {
                                                SubCode = DRS[0]["Subject"],
                                                Held = Convert.ToInt32(DRS[0]["Held"]) > 0 ? DRS[0]["Held"].ToString() : "--",
                                                Attend = Convert.ToInt32(DRS[0]["Held"]) > 0 ? DRS[0]["Attend"].ToString() : "--",
                                                Leave = DRS[0]["Leave"],
                                                Percent = Convert.ToInt32(DRS[0]["Held"]) > 0 ? Math.Round(Convert.ToDouble(DRS[0]["Attend"]) / Convert.ToInt32(DRS[0]["Held"])*100, 2).ToString() : "--"
                                            });
                                        }
                                        else
                                        {
                                            StuAtt.Add(new
                                            {
                                                SubCode = sitem["Subject_Code"],
                                                Held = "--",
                                                Attend = "--",
                                                Leave = "--",
                                                Percent = "--",
                                            });
                                        }
                                    }                                    
                                }
                                if (TotalHeld > 0 || TotalLeave > 0)
                                {
                                    StuAtt.Add(new
                                    {
                                        SubCode = "Overall",
                                        Held = TotalHeld > 0 ? TotalHeld.ToString() : "--",
                                        Attend = Convert.ToInt32(TotalHeld) > 0 ? TotalAttend.ToString() : "--",
                                        Leave = TotalLeave,
                                        Percent = Convert.ToInt32(TotalHeld) > 0 ? Math.Round(Convert.ToDouble(TotalAttend) / TotalHeld * 100, 2).ToString() : "--"
                                    });
                                }
                                DataList.Add(new
                                {
                                    ID = item["Student_ID"],
                                    Section = item["Section"],
                                    CRNo = item["Roll_NO"],
                                    UnivRNo = item["UnivRNo"],
                                    Name = ToTitle(item["Student_FName"].ToString()),
                                    FName = ToTitle(item["Father_Name"].ToString()),
                                    RegOn = item["RegOn"].ToString().Length > 0 ? item["RegOn"].ToString() : "Not Yet",
                                    StuAtt = StuAtt,
                                });
                            }

                            if (D.Rows.Count > 0)
                            {
                                foreach (DataRow item in S.Rows)
                                {
                                    SubList.Add(new
                                    {
                                        SubCode = item["Subject_Code"],
                                        SubName = ToTitle(item["Subject_Name"].ToString()),
                                        Type = ToTitle(item["Type"].ToString()),
                                    });
                                }

                                SubList.Add(new
                                {
                                    SubCode = "Overall",
                                    SubName = "Overall",
                                    Type = "Overall",
                                });
                            }
                            break;


                    case "Daily Attendance":
                            
                            if (Sub != "")
                            {
                                D = MyConnections.Select("SELECT A.*,DATE_FORMAT(B.RegOn, '%d.%m.%Y') 'RegOn' from(SELECT DISTINCT A.Student_ID, SUBSTRING_INDEX(SUBSTRING_INDEX(Register_ID, '#', 3), '#', -1) 'Section', Roll_NO, IFNULL(MRollNo, '---') 'UnivRNo', Student_FName, Father_Name,B.Status from student_master A, student_semester B, student_register C where A.Student_ID = B.Student_ID And B.Student_ID = C.Student_ID And " + (Sec != "" ? "Register_ID = '" + Sec + "#" + Sub + "'" : "Register_ID like '" + Code + "#" + Sem + "#%#" + Sub + "'") + "  ORDER BY SUBSTRING_INDEX(Register_ID, '#', 3), Roll_NO) A LEFT JOIN registrations B on A.Student_ID = B.ID;", "Student");
                                S = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from (Select DISTINCT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(`Status`='1',1,0)) 'Present',SUM(IF(`Status`='0',1,0)) 'Absent',SUM(IF(`Status`='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#" + Sub + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%#" + Sub + "#%'") + (SDate != "" ? " And Lecture_Date='" + SDate + "'" : "") + " GROUP BY Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1)) A , subject_master B where A.`Subject`=B.Subject_Code ORDER BY B.Type DESC,A.`Subject`,A.Student_ID", "Attendance");
                                A = MyConnections.Select("Select DISTINCT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(`Status`='1',1,0)) 'Present',SUM(IF(`Status`='0',1,0)) 'Absent',SUM(IF(`Status`='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#" + Sub + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%#" + Sub + "#%'") + (SDate != "" ? " And Lecture_Date='" + SDate + "'" : "") + " GROUP BY Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1)", "Attendance");                                
                            }
                            else
                            {
                                D = MyConnections.Select("SELECT A.*,DATE_FORMAT(B.RegOn, '%d.%m.%Y') 'RegOn' from(SELECT DISTINCT A.Student_ID, SUBSTRING_INDEX(Section_ID, '#', -1) 'Section', Roll_NO, IFNULL(MRollNo, '---') 'UnivRNo', Student_FName, Father_Name,B.Status from student_master A, student_semester B, section_master C where A.Student_ID = B.Student_ID And B.Student_ID = C.Student_ID And " + (Sec != "" ? "Section_ID = '" + Sec + "'" : "Section_ID like '" + Code + "#" + Sem + "#%'") + "  ORDER BY Section_ID, Roll_NO) A LEFT JOIN registrations B on A.Student_ID = B.ID;", "Student");
                                S = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from (Select DISTINCT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(`Status`='1',1,0)) 'Present',SUM(IF(`Status`='0',1,0)) 'Absent',SUM(IF(`Status`='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%'") + (SDate != "" ? " And Lecture_Date='" + SDate + "'" : "") + " GROUP BY Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1)) A , subject_master B where A.`Subject`=B.Subject_Code ORDER BY B.Type DESC,A.`Subject`,A.Student_ID", "Attendance");
                                A = MyConnections.Select("Select DISTINCT Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF(`Status`='1',1,0)) 'Present',SUM(IF(`Status`='0',1,0)) 'Absent',SUM(IF(`Status`='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID " + (Sec != "" ? "And Batch_ID like '" + Sec + "#%'" : "And Batch_ID like '" + Code + "#" + Sem + "#%'") + (SDate != "" ? " And Lecture_Date='" + SDate + "'" : "") + " GROUP BY Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1)", "Attendance");                                
                            }

                            foreach (DataRow item in D.Rows)
                            {
                                int TotalPresent = 0;
                                int TotalAbsent = 0;
                                int TotalLeave = 0;

                                List<object> StuAtt = new List<object>();
                                if (A.Rows.Count > 0 && S.Rows.Count > 0)
                                {
                                    foreach (DataRow sitem in S.Rows)
                                    {
                                        DataRow[] DRS = A.Select("Student_ID='" + item["Student_ID"] + "' And Subject='" + sitem["Subject_Code"] + "'");
                                        if (DRS.Length > 0)
                                        {
                                            TotalPresent += Convert.ToInt32(DRS[0]["Present"]);
                                            TotalAbsent += Convert.ToInt32(DRS[0]["Absent"]);
                                            TotalLeave += Convert.ToInt32(DRS[0]["Leave"]);

                                            string MyStatus = "";
                                            if (Convert.ToInt32(DRS[0]["Present"]) > 0) MyStatus = MyStatus + DRS[0]["Present"] + "-P,";
                                            if (Convert.ToInt32(DRS[0]["Absent"]) > 0) MyStatus = MyStatus + DRS[0]["Absent"] + "-A,";
                                            if (Convert.ToInt32(DRS[0]["Leave"]) > 0) MyStatus = MyStatus + DRS[0]["Leave"] + "-L,";

                                            if (MyStatus.Length > 0) MyStatus = MyStatus.Substring(0, MyStatus.Length - 1);

                                            StuAtt.Add(new
                                            {
                                                SubCode = DRS[0]["Subject"],
                                                Held = MyStatus,
                                                Attend = "--",
                                                Leave = "--",
                                                Percent = "--",
                                            });
                                        }
                                        else
                                        {
                                            StuAtt.Add(new
                                            {
                                                SubCode = sitem["Subject_Code"],
                                                Held = "--",
                                                Attend = "--",
                                                Leave = "--",
                                                Percent = "--",
                                            });
                                        }
                                    }
                                }
                                if (TotalPresent > 0 || TotalAbsent > 0 || TotalLeave>0)
                                {
                                    string MyStatus = "";                                    
                                    if ((TotalPresent + TotalAbsent) > 0) MyStatus = Math.Round((decimal)TotalPresent / (TotalPresent + TotalAbsent) * 100, 2).ToString();
                                    else
                                    {
                                        if (TotalLeave > 0) MyStatus = MyStatus + TotalLeave + "-L, ";
                                    }

                                    StuAtt.Add(new
                                    {
                                        SubCode = "Overall",
                                        Held = MyStatus,
                                        Attend = "--",
                                        Leave = "--",
                                        Percent = "--",
                                    });
                                }
                                DataList.Add(new
                                {
                                    ID = item["Student_ID"],
                                    Section = item["Section"],
                                    CRNo = item["Roll_NO"],
                                    UnivRNo = item["UnivRNo"],
                                    Name = ToTitle(item["Student_FName"].ToString()),
                                    FName = ToTitle(item["Father_Name"].ToString()),
                                    RegOn = item["RegOn"].ToString().Length > 0 ? item["RegOn"].ToString() : "Not Yet",
                                    StuAtt = StuAtt,
                                });
                            }

                            if (D.Rows.Count > 0)
                            {
                                foreach (DataRow item in S.Rows)
                                {
                                    SubList.Add(new
                                    {
                                        SubCode = item["Subject_Code"],
                                        SubName = ToTitle(item["Subject_Name"].ToString()),
                                        Type = ToTitle(item["Type"].ToString()),
                                    });
                                }

                                SubList.Add(new
                                {
                                    SubCode = "Overall",
                                    SubName = "Overall",
                                    Type = "Overall",
                                });
                            }
                            break;



                        case "Student Attendance":

                            D = MyConnections.Select("SELECT A.*,DATE_FORMAT(B.RegOn, '%d.%m.%Y') 'RegOn' from(SELECT DISTINCT A.Student_ID, SUBSTRING_INDEX(Section_ID, '#', -1) 'Section', Roll_NO, IFNULL(MRollNo, '---') 'UnivRNo', Student_FName, Father_Name,B.Status,LOWER(Sub_Category) as 'SEmail',Corr_Mob_Student 'SMob',Corr_Mob_Father 'FMob' from student_master A, student_semester B, section_master C where A.Student_ID = B.Student_ID And B.Student_ID = C.Student_ID And A.Student_ID='" + SId + "' ORDER BY SUBSTRING_INDEX(Section_ID, '#', 3), Roll_NO) A LEFT JOIN registrations B on A.Student_ID = B.ID;", "Student");
                            S = MyConnections.Select("SELECT DISTINCT B.Subject_Code,B.Subject_Name,B.Type from student_register A, subject_master B where Student_ID='" + SId + "' " + (Sub != "" ? " And Subject_Code='" + Sub + "'" : "") + " And SUBSTRING_INDEX(Register_ID,'#',-1)=Subject_Code ORDER BY B.Type DESC,B.Subject_Code", "Attendance");
                            A = MyConnections.Select("SELECT DISTINCT Student_ID,DATE_FORMAT(Lecture_Date,'%d.%m.%Y') as 'LecDate',SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) 'Subject',SUM(IF("+UseStatus+"='1',1,0)) 'Present',SUM(IF("+UseStatus+"='0',1,0)) 'Absent',SUM(IF("+UseStatus+"='L',1,0)) 'Leave' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID And Student_ID='" + SId + "' " + (Sub != "" ? " And Batch_ID like '%#" + Sub + "#%'" : "") + (SDate != "" ? " And Lecture_Date>='" + SDate + "'" : "") + (EDate != "" ? " And Lecture_Date<='" + EDate + "'" : "") + " GROUP BY Lecture_Date desc,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',4),'#',-1) ORDER BY Lecture_Date;", "Attendance");

                            List<string> LecDates = new List<string>();

                            foreach (DataRow item in D.Rows)
                            {
                                List<object> StuAtt = new List<object>();
                                if (A.Rows.Count > 0 && S.Rows.Count > 0)
                                {
                                    foreach (DataRow sitem in A.Rows)
                                    {
                                        string MyStatus = "";
                                        if (Convert.ToInt32(sitem["Present"]) > 0) MyStatus = MyStatus + sitem["Present"] + "-P,";
                                        if (Convert.ToInt32(sitem["Absent"]) > 0) MyStatus = MyStatus + sitem["Absent"] + "-A,";
                                        if (Convert.ToInt32(sitem["Leave"]) > 0) MyStatus = MyStatus + sitem["Leave"] + "-L,";

                                        if (MyStatus.Length > 0) MyStatus = MyStatus.Substring(0, MyStatus.Length - 1);

                                        StuAtt.Add(new
                                        {
                                            SubCode = sitem["Subject"],
                                            LecDate = sitem["LecDate"],
                                            Held = MyStatus
                                        });

                                        if (LecDates.IndexOf(sitem["LecDate"].ToString()) < 0) LecDates.Add(sitem["LecDate"].ToString());
                                    }
                                }

                                DataList.Add(new
                                {
                                    SEmail = item["SEmail"],
                                    SMob = item["SMob"],
                                    FMob = item["FMob"],
                                    ID = item["Student_ID"],
                                    Section = item["Section"],
                                    CRNo = item["Roll_NO"],
                                    UnivRNo = item["UnivRNo"],
                                    Name = ToTitle(item["Student_FName"].ToString()),
                                    FName = ToTitle(item["Father_Name"].ToString()),
                                    RegOn = item["RegOn"].ToString().Length > 0 ? item["RegOn"].ToString() : "Not Yet",
                                    StuAtt = StuAtt,
                                    LecDates = LecDates,
                                    FDuration = SDate.Length > 0 ? (SDate.Split('-')[2] + "." + SDate.Split('-')[1] + "." + SDate.Split('-')[0]) : "Start",
                                    TDuration = EDate.Length > 0 ? (EDate.Split('-')[2] + "." + EDate.Split('-')[1] + "." + EDate.Split('-')[0]) : "End",
                                    AttPer = "",
                                });
                            }                            

                            if (D.Rows.Count > 0)
                            {
                                foreach (DataRow item in S.Rows)
                                {
                                    SubList.Add(new
                                    {
                                        SubCode = item["Subject_Code"],
                                        SubName = ToTitle(item["Subject_Name"].ToString()),
                                        Type = ToTitle(item["Type"].ToString()),
                                    });
                                }

                                SubList.Add(new
                                {
                                    SubCode = "Overall",
                                    SubName = "Overall",
                                    Type = "Overall",
                                });
                            }
                            break;


                        case "Faculty Time Table":

                            bool AllowOfflineUpload = Export.BarrierStatus("allowofflineupload");
                            string L_OverallLock = OverallLock() ? "Y" : "N";
                            DateTime L_TermLock = TermLock();
                            DateTime L_LastDate = GapDays();
                            string AllowedTillDate = L_LastDate.Month + "/" + L_LastDate.Day + "/" + L_LastDate.Year;

                            D = MyConnections.Select("Select DATE_FORMAT(start_date,'%d.%m.%Y') as 'ondate',id,`extra2` as 'course',A.semester,SUBSTRING_INDEX(sectionid,'#',-1) 'section',sectionid,batchid,`extra3` AS `subcode`,`extra4` AS `subname`,IF(extra6='Lec','Lecture',IF(extra6 like 'L%',REPLACE(extra6,'L','Batch-'),REPLACE(extra6,'T','Batch-'))) as 'batch',classmode,buildingname,roomno,dayname(`start_date`) AS 'thisday',if(`extra5` = 'Lec','Lecture',if(`extra5`= 'Lab','Laboratory',if(`extra5` = 'Tut','Tutorial',`extra5`))) as 'classtype',lectureno,SUBSTRING_INDEX(`extra1`,' - ',1) 'StartTime',SUBSTRING_INDEX(`extra1`,' - ',-1) 'EndTime',CAST(DATE_FORMAT(start_date,'%Y-%m-%d') as CHAR) 'ForDate',extra7,CAST(DATE_FORMAT(start_date,'%Y-%m-%d %H:%i:%s') as CHAR) 'LecTime',ZoomMeetingId,IF(DATE(start_date)<DATE(now()),'Y','N') 'IsPast',IF(start_date>now(),'U',IF(DATE(start_date)=DATE(now()),'T','P')) 'CurStat',`status`,TotalAB,TotalL,TotalP,TotalStu,LecId,DATE_FORMAT(UplOn,'%d.%m.%Y %h:%i %p') 'UplOn' from mytimetable A, semtocourse B,course C where A.coursecode=B.course_code And A.semester=B.Semester And end_date='" + SDate + "' And batchid like '%#%' And B.course_code=C.Code And employeeid='" + UserId + "' ORDER BY start_date,lectureno;", "Attendance");
                            A = MyConnections.Select("Select ClassRoomNo,ZoomUserId,AttendanceDownloadedOn,AttendanceUploadedOn,RecordingLink,MeetingStartUrl,MeetingJoinUrl,IF(ScheduleDate<DATE(now()),'P',IF(ScheduleDate>DATE(now()),'F',IF(TIME(now())<TimeFrom,'F',IF(TIME(now())>TimeTill,'P','C')))) 'SlotStatus',MeetingId,MeetingTitle,MeetingTotalTime,MeetingInstancesCount,Duration from zoom_schedules where UsedForType='Regular' And ScheduleDate='" + SDate + "' And `Status`='Active' And MeetingId is not NULL And EmployeeCode='" + UserId + "';", "Attendance");
                            
                            foreach (DataRow item in D.Rows)
                            {
                                string MinCheck = "-1";
                                object ForZoom = new
                                {
                                    ClassRoomNo = "",
                                };

                                

                                if (item["ZoomMeetingId"].ToString().Length > 0)
                                {
                                    DataRow[] DRS = A.Select("MeetingId=" + item["ZoomMeetingId"].ToString());
                                    if (DRS.Length > 0)
                                    {
                                        //MeetingTotalTime,MeetingInstancesCount,Duration
                                        ForZoom = new
                                        {
                                            ClassRoomNo = DRS[0]["ClassRoomNo"].ToString(),
                                            ZoomUserId = DRS[0]["ZoomUserId"].ToString(),
                                            AttendanceDownloadedOn = DRS[0]["AttendanceDownloadedOn"].ToString(),
                                            AttendanceUploadedOn = DRS[0]["AttendanceUploadedOn"].ToString(),
                                            RecordingLink = DRS[0]["RecordingLink"].ToString(),
                                            MeetingStartUrl = DRS[0]["MeetingStartUrl"].ToString(),
                                            MeetingJoinUrl = DRS[0]["MeetingJoinUrl"].ToString(),
                                            SlotStatus = DRS[0]["SlotStatus"].ToString(),
                                            MeetingTotalTime = DRS[0]["MeetingTotalTime"].ToString(),
                                            MeetingInstancesCount = DRS[0]["MeetingInstancesCount"].ToString(),
                                            Duration = DRS[0]["Duration"].ToString(),
                                        };
                                    }
                                }


                                string Message = "Attendance Pending For This Lecture";
                                Int64 TotalStu = Convert.ToInt64(item["TotalStu"]);
                                string Status = "Pending";
                                if (item["LecId"].ToString() == "-1" || item["LecId"].ToString() == "0")
                                {
                                    DataTable Stu = MyConnections.Select("SELECT COUNT(*) from student_batch where Batch_ID='" + item["batchid"] + "'", "Attendance");
                                    if (Stu.Rows.Count > 0 && Stu.Rows[0][0].ToString().Length > 0)
                                    {
                                        TotalStu = Convert.ToInt64(Stu.Rows[0][0].ToString());
                                    }                                    
                                }

                                object ForAttendance = new
                                {
                                    Student = TotalStu,
                                    UploadedOn="",
                                    LecId=0,
                                };


                                if (item["CurStat"].ToString() == "U")
                                {
                                    Status = "Upcoming";
                                    Message = "Upcoming Clasess";
                                }
                                else
                                {
                                    S = MyConnections.Select("SELECT A.Lecture_ID,COUNT(*) 'Student',SUM(IF(StatusOther='L',1,0)) 'Leave',SUM(IF(StatusOther='1',1,0)) 'PresentA',SUM(IF(Status='1',1,0)) 'Present',SUM(IF(StatusOther='0',1,0)) 'AbsentA',SUM(IF(Status='0',1,0)) 'Absent',ROUND(IF(SUM(IF(StatusOther!='L',Weight,0))<=0,0.00,SUM(IF(StatusOther='1',Weight,0))/SUM(IF(StatusOther!='L',Weight,0))*100),2) 'PercentA',ROUND(IF(SUM(IF(`Status`!='L',Weight,0))<=0,0.00,SUM(IF(`Status`='1',Weight,0))/SUM(IF(`Status`!='L',Weight,0))*100),2) 'Percent',DATE_FORMAT(MAX(A.UploadedOn),'%d.%m.%Y %h:%i %p') 'UplOn' from lecture_master A, lecture_attendance B where A.Lecture_ID=B.Lecture_ID And A.Batch_ID='" + item["batchid"].ToString() + "' And A.Lecture_Date='" + item["ForDate"].ToString() + "' And A.Lecture_No='" + item["lectureno"].ToString() + "' group by A.Lecture_ID", "Attendance");
                                    if (S.Rows.Count > 0)
                                    {
                                        Status = "Uploaded";
                                        Message = "Attendance Uploaded : " + S.Rows[0]["UplOn"].ToString();

                                        item["status"] = "Uploaded";
                                        item["UplOn"] = S.Rows[0]["UplOn"].ToString();

                                        ForAttendance = new
                                        {
                                            Student = Convert.ToInt64(S.Rows[0]["Student"].ToString()),
                                            Leave = Convert.ToInt64(S.Rows[0]["Leave"].ToString()),
                                            PresentA = Convert.ToInt64(S.Rows[0]["PresentA"].ToString()),
                                            Present = Convert.ToInt64(S.Rows[0]["Present"].ToString()),
                                            AbsentA = Convert.ToInt64(S.Rows[0]["AbsentA"].ToString()),
                                            Absent = Convert.ToInt64(S.Rows[0]["Absent"].ToString()),

                                            Percent = Convert.ToDouble(S.Rows[0]["Percent"].ToString()),
                                            PercentA = Convert.ToDouble(S.Rows[0]["PercentA"].ToString()),
                                            UploadedOn = S.Rows[0]["UplOn"].ToString(),
                                            LecId = Convert.ToInt64(S.Rows[0]["Lecture_ID"].ToString()),
                                        };
                                    }
                                    else
                                    {
                                        if (Status == "Pending")
                                        {
                                            if (Export.CheckDOJLeaveEtc(HttpContext.Current.Session["User_ID"].ToString(), SDate, out Message)) Status = "Leave";
                                        }
                                    }
                                }


                                if (Status == "Pending")
                                {

                                    //Possible Status = { "Lock", "Uploaded", "Lock Open", "Token Open", "Fine", "Future", "Leave" };
                                    string det = "";
                                    bool nextcheck = true;
                                    string nextchecktype = "F";                                    
                                    string stat = Export.checktiming(item["ForDate"].ToString(), item["classtype"].ToString(), item["sectionid"].ToString(), item["batchid"].ToString(), item["lectureno"].ToString(), out det, out nextcheck, out nextchecktype, out MinCheck);

                                    //msg= U,O,F,E
                                    if (nextcheck == true)
                                    {
                                        // 15-Min Case Disabled 
                                        stat = Export.allowedtill(UserId, item["batchid"].ToString(), item["ForDate"].ToString(), item["lectureno"].ToString(), out det, nextchecktype, L_OverallLock, Convert.ToDateTime(L_TermLock), Convert.ToDateTime(AllowedTillDate).ToString("MM/dd/yyyy"));
                                    }
                                    else
                                    {
                                        // Future, 15-Min OK, Uploaded
                                        if (stat == "U")
                                        {
                                            stat = "Uploaded";
                                        }
                                        if (stat == "O")
                                        {
                                            stat = "Fine";
                                        }
                                        if (stat == "F")
                                        {
                                            stat = "Future";
                                        }
                                    }                                    


                                    switch (stat)
                                    {
                                        case "Lock":
                                            Status = "Locked";
                                            Message = "Can`t Upload : Status Locked";
                                            break;

                                        case "Uploaded":
                                            Status = "Uploaded";
                                            Message = "Already Uploaded";
                                            break;

                                        case "Future":
                                            Status = "Upcoming";
                                            Message = "Upcoming Clasess";
                                            break;

                                        case "Leave":
                                            Status = "Leave";
                                            Message = "Can`t Upload : On Leave";
                                            break;


                                        case "Lock Open":
                                            break;
                                        case "Fine":

                                            if (Export.CheckPrepTableStatus("EnableExact15MinLock") && MinCheck != "-1") MinCheck = MinCheck;                                            
                                            else MinCheck = "-1";
                                            
                                            break;

                                        case "Token Open":
                                            Status = "Token";
                                            Message = "Allowed With Token";
                                            break;
                                    }



                                    if (Status == "Pending" || Status == "Token" || Status=="Locked")
                                    {
                                        if (item["IsPast"].ToString() != "Y" && item["ZoomMeetingId"].ToString() != "-1" && item["ZoomMeetingId"].ToString() != "")
                                        {
                                            Status = "Zoom";
                                            Message = "Upload By Zoom : If Taken";
                                        }
                                        else
                                        {
                                            if (item["IsPast"].ToString() == "Y" && item["ZoomMeetingId"].ToString() != "-1" && item["ZoomMeetingId"].ToString() != "")
                                            {
                                                Status = "Locked";
                                                Message = "Upload By Zoom : If Taken";
                                            }
                                            else
                                            {
                                                if (AllowOfflineUpload == false && (Status == "Pending" || Status == "Token"))
                                                {
                                                    Status = "Locked";
                                                    Message = "Can`t Upload : Offline Disabled";
                                                }
                                            }
                                        }
                                    }
                                }

                                DataList.Add(new
                                {
                                    id = Convert.ToInt64(item["id"]),
                                    course = item["course"],
                                    ondate = item["ondate"],
                                    semester = item["semester"],
                                    section = item["section"],
                                    batchid = item["batchid"],
                                    subcode = item["subcode"],
                                    subname = Export.ToTitle(item["subname"].ToString()),
                                    batch = item["batch"],
                                    classmode = item["classmode"],
                                    buildingname = item["buildingname"],
                                    roomno = item["roomno"],
                                    thisday = item["thisday"],

                                    lectureno = Convert.ToInt64(item["lectureno"]),

                                    ZoomMeetingId = item["ZoomMeetingId"],
                                    classtype = item["classtype"],

                                    StartTime = item["StartTime"],
                                    EndTime = item["EndTime"],

                                    Slot = item["StartTime"]+" - "+item["EndTime"],

                                    ForDate = item["ForDate"],
                                    LecTime = item["LecTime"],

                                    IsPast = item["IsPast"],
                                    CurStat = item["CurStat"],
                                    status = Status,                                    
                                    Message = Message,

                                    IsEngaged=item["extra7"].ToString()==""?"Regular": item["extra7"].ToString(),

                                    ForAttendance = ForAttendance,
                                    ForZoom = ForZoom,
                                    Role="Faculty",
                                    MinCheck = MinCheck,
                                });
                            }
                            break;


                        case "Upload Attendance Student":

                            bool DisableEJ = Export.CheckPrepTableStatus("DisableEJInAttendance");
                            long TId = 0;
                            if (Int64.TryParse(SId, out TId))
                            {
                                D = MyConnections.Select("Select DATE_FORMAT(A.start_date,'%d.%m.%Y') as 'ondate',id,`extra2` as 'course',A.semester,SUBSTRING_INDEX(sectionid,'#',-1) 'section',sectionid,batchid,`extra3` AS `subcode`,`extra4` AS `subname`,IF(extra6='Lec','Lecture',IF(extra6 like 'L%',REPLACE(extra6,'L','Batch-'),REPLACE(extra6,'T','Batch-'))) as 'batch',classmode,buildingname,roomno,dayname(A.`start_date`) AS 'thisday',if(`extra5` = 'Lec','Lecture',if(`extra5`= 'Lab','Laboratory',if(`extra5` = 'Tut','Tutorial',`extra5`))) as 'classtype',lectureno,SUBSTRING_INDEX(`extra1`,' - ',1) 'StartTime',SUBSTRING_INDEX(`extra1`,' - ',-1) 'EndTime',CAST(DATE_FORMAT(A.start_date,'%Y-%m-%d') as CHAR) 'ForDate',extra7,CAST(DATE_FORMAT(A.start_date,'%Y-%m-%d %H:%i:%s') as CHAR) 'LecTime',ZoomMeetingId,IF(DATE(A.start_date)<DATE(now()),'Y','N') 'IsPast',IF(A.start_date>now(),'U',IF(DATE(A.start_date)=DATE(now()),'T','P')) 'CurStat',`status`,TotalAB,TotalL,TotalP,TotalStu,LecId,DATE_FORMAT(UplOn,'%d.%m.%Y %h:%i %p') 'UplOn' from mytimetable A, (SELECT employeeid, start_date,temp_end from mytimetable where id= " + TId + ") B where A.start_date = B.start_date And A.employeeid = B.employeeid And `status`='Pending'  And A.temp_end=B.temp_end ORDER BY batchid ;", "Attendance");
                                foreach (DataRow DR in D.Rows)
                                {
                                    string Rule = "DEACTIVATED";
                                    DataTable dtrule = MyConnections.Select("Select Registration_Date_Apply from rule where Course_Code='" + DR["batchid"].ToString().Split('#')[0] + "'", "Attendance");
                                    if (dtrule.Rows.Count > 0) Rule = dtrule.Rows[0]["Registration_Date_Apply"].ToString().ToUpper();
                                    Batches.Add(DR["batchid"].ToString() + "|" + DR["id"].ToString() + "|" + Rule.ToUpper());

                                    SubList.Add(new
                                    {
                                        TId = Convert.ToInt64(DR["id"]),
                                        Lecture_ID = -1,
                                        BatchId = DR["batchid"],
                                        Course = DR["course"],
                                        Sem = DR["semester"],
                                        Sec = DR["section"],
                                        Sub = DR["subcode"],

                                        Batch = DR["batch"],                                        
                                        Slot = DR["StartTime"] + " - " + DR["EndTime"],                                        
                                        LecTime = DR["LecTime"],

                                        ForDate = DR["ForDate"],
                                        lectureno = Convert.ToInt64(DR["lectureno"]),
                                        IsEngaged = DR["extra7"].ToString() == "" ? "Regular" : DR["extra7"].ToString(),
                                        weight = 1,
                                        Topic = "",
                                        SubTopic = "",
                                        MinCheck = "-1",
                                        Role = "Faculty",
                                        Rule = Rule,
                                    });
                                }
                            }
                            else
                            {
                                string Rule = "DEACTIVATED";
                                DataTable dtrule = MyConnections.Select("Select Registration_Date_Apply from rule where Course_Code='" + SId.Split('#')[0] + "'", "Attendance");
                                if (dtrule.Rows.Count > 0)
                                {
                                    Batches.Add(SId + "|-1|" + dtrule.Rows[0]["Registration_Date_Apply"].ToString().ToUpper());
                                    Rule = dtrule.Rows[0]["Registration_Date_Apply"].ToString();
                                }
                                else
                                {
                                    Batches.Add(SId + "|-1|DEACTIVATED");
                                }
                                dtrule = MyConnections.Select("SELECT Tag as 'Course',SUBSTRING_INDEX(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',2),'#',-1) as 'Sem',SUBSTRING_INDEX(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',3),'#',-1) as 'Sec',SUBSTRING_INDEX(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',4),'#',-1) as 'Sub',IF(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',-1)='All','Lecture',IF(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',-1) like 'L%',REPLACE(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',-1),'L','Batch-'),REPLACE(SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',-1),'T','Batch-'))) as 'Batch' from course where `Code`=SUBSTRING_INDEX('" + Batches[0].Split('|')[0] + "','#',1)", "Attendance");
                                if (dtrule.Rows.Count > 0)
                                {
                                    SubList.Add(new
                                    {
                                        TId = Batches[0].Split('|')[1],
                                        Lecture_ID = -1,
                                        BatchId = Batches[0].Split('|')[0],
                                        Course = dtrule.Rows[0]["Course"].ToString(),
                                        Sem = dtrule.Rows[0]["Sem"].ToString(),
                                        Sec = dtrule.Rows[0]["Sec"].ToString(),
                                        Sub = dtrule.Rows[0]["Sub"].ToString(),
                                        Batch = dtrule.Rows[0]["Batch"].ToString(),
                                        Slot = "No Time Table",
                                        LecTime="",
                                        ForDate = SDate,
                                        lectureno = -1,
                                        IsEngaged = "Regular",
                                        weight = 1,
                                        Topic = "",
                                        SubTopic = "",
                                        Rule = Rule,
                                        MinCheck = "-1"
                                    });
                                }
                            }
                            
                            DataList = StudentListForAttendance(-1,Batches, SDate, DisableEJ);                            
                            break;



                        case "View Attendance":
                        case "Edit Attendance":
                        case "Delete Attendance":

                            long LId = 0;
                            
                            if (Int64.TryParse(SId, out LId))
                            {
                                D = MyConnections.Select("SELECT Weight,Lecture_No,Batch_ID,Lecture_ID,LectureType,Topics,Subtopics,LectureTime,LectureType from lecture_master where Lecture_ID=" + LId, "Attendance");
                                foreach (DataRow DR in D.Rows)
                                {
                                    string Rule = "DEACTIVATED";
                                    DataTable dtrule = MyConnections.Select("Select Registration_Date_Apply from rule where Course_Code='" + DR["Batch_ID"].ToString().Split('#')[0] + "'", "Attendance");
                                    if (dtrule.Rows.Count > 0) Rule = dtrule.Rows[0]["Registration_Date_Apply"].ToString().ToUpper();
                                    Batches.Add(DR["Batch_ID"].ToString() + "|" + (Sub != "" ? Convert.ToInt64(Sub) : -1) + "|" + Rule.ToUpper());

                                    SubList.Add(new
                                    {
                                        TId = Sub!=""? Convert.ToInt64(Sub):-1,
                                        Lecture_ID = LId,

                                        BatchId = DR["Batch_ID"],
                                        Course = "",
                                        Sem = "",
                                        Sec = "",
                                        Sub = "",

                                        Batch = "",
                                        Slot = DR["LectureTime"],
                                        LecTime = "",

                                        ForDate = "",
                                        lectureno = Convert.ToInt32(DR["Lecture_No"]),
                                        IsEngaged = DR["LectureType"],
                                        weight = Convert.ToInt32(DR["Weight"]),
                                        Topic = DR["Topics"],
                                        SubTopic = DR["Subtopics"],
                                        MinCheck = "-1",
                                        Role = "Faculty",
                                        Rule = Rule,
                                    });
                                }
                                DataList = StudentListForAttendance(LId, Batches, SDate, false);
                            }
                            
                            break;

                    }
                    break;
                default: break;
            }

            return DataList;
        }

        public static List<object> StudentListForAttendance(Int64 LId,List<string> Batches,string MyDate,bool  DisableEJ)
        {
            string[] dd = MyDate.Split('-');
            DateTime AtDate = new DateTime(Convert.ToInt32(dd[0]), Convert.ToInt32(dd[1]), Convert.ToInt32(dd[2]));
            List<object> Student = new List<object>();
            foreach (string BDR in Batches)
            {
                string TId = BDR.Split('|')[1];
                string RegitrationRule = BDR.Split('|')[2];

                string Q = "Select 'Absent' as 'Status','Absent' as `StatusOther`,A.Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',3),'#',-1) 'Section',A.Roll_No,MRollNo,Student_FName,Father_Name from student_batch A, student_master B, student_semester C where A.Batch_ID='" + BDR.Split('|')[0] + "' And A.Student_ID=B.Student_ID And A.Student_ID=C.Student_ID And `Status` in ('Y') ORDER BY Roll_No";
                if (LId != -1)
                {
                    Q = "Select IF(A.`Status`='L','Leave',IF(A.`Status`!='0','Present','Absent')) `Status`,IF(A.StatusOther='L','Leave',IF(A.StatusOther!='0','Present','Absent')) `StatusOther` ,A.Student_ID,SUBSTRING_INDEX(SUBSTRING_INDEX(Batch_ID,'#',3),'#',-1) 'Section',A.Roll_No,MRollNo,Student_FName,Father_Name from lecture_attendance A, student_master B, student_semester C,lecture_master D where A.Lecture_ID=" + LId + " And A.Lecture_ID=D.Lecture_ID And A.Student_ID=B.Student_ID And A.Student_ID=C.Student_ID  ORDER BY Roll_No";
                }
                DataTable dt = MyConnections.Select(Q, "Attendance");                
                if (dt.Rows.Count > 0)
                {
                    for (int k = 0; k < dt.Rows.Count; k++)
                    {                        
                        if (DisableEJ)
                        {
                            DataTable D = MyConnections.Select("SELECT CompanyName as 'Company',IF(FinalStatus='Approved',CONCAT('Early Joining @ From : ',DATE_FORMAT(ExpectedJoiningDate,'%d.%m.%Y')),CONCAT('Early Joining @ From : ',DATE_FORMAT(ExpectedJoiningDate,'%d.%m.%Y'),' - Till : ',DATE_FORMAT(ReturnedOn,'%d.%m.%Y'))) 'Reason' from placement_management.student_earlyjoining where ((FinalStatus ='Approved' And ExpectedJoiningDate<='" + MyDate + "' And ReturnedStatus!='Approved') Or (FinalStatus ='Approved' And ReturnedStatus ='Approved'  And ReturnedOn is not NULL And ExpectedJoiningDate<='" + MyDate + "' And ReturnedOn>='" + MyDate + "')) And Student_ID='" + dt.Rows[k]["Student_ID"].ToString() + "' Order By Id Desc Limit 1", "Attendance");
                            if (D.Rows.Count > 0) continue;
                        }
                        string CurAttendance = "--", IsRegistered = "Y";
                        DataTable dtsub = MyConnections.Select("Select SUM(if(`Status`!='L',`Status`,0)) 'Att',SUM(if(`Status`!='L',Weight,0)) 'Held',ROUND(SUM(if(`Status`!='L',`Status`,0))/SUM(if(`Status`!='L',Weight,0))*100,2) 'Per' from lecture_master A, lecture_attendance B where A.Batch_ID like '" + BDR.Split('|')[0] + "' And A.Lecture_ID=B.Lecture_ID And B.Student_ID='" + dt.Rows[k]["Student_ID"].ToString() + "'", "Attendance");
                        if (dtsub.Rows.Count > 0)
                        {
                            if (dtsub.Rows[0][1].ToString().Length > 0)
                            {
                                double val = 0.00;
                                double.TryParse(dtsub.Rows[0][2].ToString(), out val);
                                CurAttendance = val.ToString("0.00");
                            }
                        }

                        if (dt.Rows[k]["MRollNo"].ToString().Length > 0)
                        {
                            DataTable dtsub21 = MyConnections.Select("Select DATE_FORMAT(ApprovedOn,'%m/%d/%Y') from student_payment_details A, student_semester B where A.Student_ID=B.Student_ID And A.`Session`=B.MySes And MySes>=(Select * from current_session) And A.Semester=B.Semester And A.Student_ID='" + dt.Rows[k]["Student_ID"].ToString() + "' And ApprovedOn is not NULL;", "Attendance");
                            if (dtsub21.Rows.Count > 0)
                            {
                                if (AtDate < Convert.ToDateTime(dtsub21.Rows[0][0].ToString())) IsRegistered = "N";
                            }
                            else
                            {
                                IsRegistered = "N";
                            }
                        }

                        Student.Add(new
                        {
                            id = Convert.ToInt64(dt.Rows[k]["Student_ID"]),

                            Section = dt.Rows[k]["Section"],
                            CRNo = dt.Rows[k]["Roll_No"],
                            UnivRNo = dt.Rows[k]["MRollNo"],
                            Name = ToTitle(dt.Rows[k]["Student_FName"].ToString()),
                            FName = ToTitle(dt.Rows[k]["Father_Name"].ToString()),
                            RegOn = IsRegistered,
                            CurAttendance = CurAttendance,
                            BatchId = BDR.Split('|')[0],
                            TId = TId,

                            Status = dt.Rows[k]["Status"],
                            StatusOther = dt.Rows[k]["StatusOther"],
                        });
                    }                    
                }
            }

            return Student;
        }
        

        public static bool CheckPrepTableStatus(string type)
        {
            DataTable dt = MyConnections.Select("Select `" + type + "` from prepfeedbacktypes where `" + type + "`='Yes'", "Attendance");
            if (dt.Rows.Count > 0)
                return true;
            else
            {
                return false;
            }
        }
        public static DateTime GapDays()
        {
            MySqlConnection conLinq = MyConnections.MyConnectionString("Attendance");
            int cnt = 0;
            string yx = DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0');
            int extra = 0;
            while (true)
            {
                MySqlDataAdapter dah1 = new MySqlDataAdapter("Select distinct `Date` from holiday_calender where Date=DATE(SUBDATE('" + yx + "',INTERVAL " + (cnt + 1) + " DAY))", conLinq);
                DataSet dth1 = new DataSet();
                dah1.Fill(dth1);
                if (dth1.Tables[0].Rows.Count <= 0)
                {
                    break;
                }
                else
                {
                    extra++;
                }
                cnt++;
            }

            string y2 = DateTime.Now.AddDays(-extra - 1).Year.ToString(), m2 = DateTime.Now.AddDays(-extra - 1).Month.ToString(), d2 = DateTime.Now.AddDays(-extra - 1).Day.ToString();

            MySqlDataAdapter dadys = new MySqlDataAdapter("Select * from gap_days ", conLinq);
            DataSet dtdys = new DataSet();
            dadys.Fill(dtdys);

            DateTime DatePicker1 = DateTime.Now.AddDays(-(Convert.ToInt32(dtdys.Tables[0].Rows[0][0].ToString())) + extra - 1);
            string y = DatePicker1.Year.ToString(), m = DatePicker1.Month.ToString(), d = DatePicker1.Day.ToString();


            MySqlDataAdapter dah = new MySqlDataAdapter("Select distinct `Date` from holiday_calender where Date>= '" + y + "-" + m + "-" + d + "' And Date<= '" + y2 + "-" + m2 + "-" + d2 + "'", conLinq);
            DataSet dth = new DataSet();
            dah.Fill(dth);






            DateTime ExtraDtm = DateTime.Now.Date.AddDays(-(Convert.ToInt32(dtdys.Tables[0].Rows[0][0].ToString()) + extra - 1 + dth.Tables[0].Rows.Count));

            cnt = 0;
            string yx2 = ExtraDtm.Year.ToString() + "-" + ExtraDtm.Month.ToString().PadLeft(2, '0') + "-" + ExtraDtm.Day.ToString().PadLeft(2, '0');
            int extra2 = 0;
            while (true)
            {
                MySqlDataAdapter dah1 = new MySqlDataAdapter("Select distinct `Date` from holiday_calender where Date=DATE(SUBDATE('" + yx2 + "',INTERVAL " + (cnt) + " DAY))", conLinq);
                DataSet dth1 = new DataSet();
                dah1.Fill(dth1);
                if (dth1.Tables[0].Rows.Count <= 0)
                {
                    break;
                }
                else
                {
                    extra2++;
                }
                cnt++;
            }

            return DateTime.Now.Date.AddDays(-(Convert.ToInt32(dtdys.Tables[0].Rows[0][0].ToString()) + extra2 + extra - 1 + dth.Tables[0].Rows.Count));
        }

        public static DateTime TermLock()
        {
            MySqlConnection conLinq = MyConnections.MyConnectionString("Attendance");
            MySqlDataAdapter darule = new MySqlDataAdapter("Select DATE_FORMAT(LockDate,'%m/%d/%Y') from term_lock where status='Activated'", conLinq);
            DataTable dtrule = new DataTable();
            darule.Fill(dtrule);

            return Convert.ToDateTime(dtrule.Rows[0][0].ToString());
        }

        public static bool OverallLock()
        {
            MySqlConnection conLinq = MyConnections.MyConnectionString("Attendance");
            MySqlDataAdapter darule = new MySqlDataAdapter("Select attendancemodify from barriers where attendancemodify='Activated'", conLinq);
            DataTable dtrule = new DataTable();
            darule.Fill(dtrule);
            if (dtrule.Rows.Count <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool BarrierStatus(string type)
        {
            string ret = "";
            string q = "Select * from prepfeedbacktypes where " + type + "='Yes'";
            DataTable dtempsub = MyConnections.Select(q, "Attendance");
            if (dtempsub.Rows.Count > 0)
            {
                return true;
            }

            return false;
        }

        public static bool CheckDOJLeaveEtc(string EmpCode, string OnDate, out string Message)
        {
            DataTable DOJCheck = MyConnections.Select("Select checkempdoj,checkempleave from barriers", "Attendance");
            Message = "";
            //DOJ & Left
            if (DOJCheck.Rows[0]["checkempdoj"].ToString().ToUpper() != "NO")
            {
                DataTable D = MyConnections.Select("Select IF(doj>'" + OnDate + "',CONCAT('Joined On ',CAST(DATE_FORMAT(doj,'%d.%m.%Y')  as CHAR)),IF(`status`='INACTIVE' And enddate is not NULL And enddate<'" + OnDate + "',CONCAT('Left On ',CAST(DATE_FORMAT(enddate,'%d.%m.%Y') as CHAR)),'')) 'Remark' from salary_management.emp_master where employee_code='" + EmpCode + "'", "Attendance");
                if (D.Rows.Count > 0 && D.Rows[0][0].ToString().Length > 0)
                {
                    Message = D.Rows[0][0].ToString();
                    return true;
                }
            }


            //Leave Pending & Approved
            if (DOJCheck.Rows[0]["checkempleave"].ToString().ToUpper() != "NO")
            {
                DataTable D = MyConnections.Select("Select CAST(CONCAT('On ',LeaveType,' : ',CAST(DATE_FORMAT(LeaveFrom,'%d.%m.%Y')  as CHAR),' - ',CAST(DATE_FORMAT(LeaveTo,'%d.%m.%Y')  as CHAR)) as CHAR) 'Msg' from salary_management.leaveentries_approval where Employee_Code = '" + EmpCode + "' And LeaveFrom<= '" + OnDate + "' And '" + OnDate + "' <= LeaveTo And LeaveStatus = 'FullDay' And `Status` in ('Pending','Approved') And LeaveTypeId!= 18;", "Attendance");
                if (D.Rows.Count > 0)
                {
                    Message = D.Rows[0][0].ToString();
                    return true;
                }
                else
                {
                    //Leave Direct Entry
                    D = MyConnections.Select("Select CAST(CONCAT('On ',LeaveType,' : ',CAST(DATE_FORMAT(LeaveFrom,'%d.%m.%Y')  as CHAR),' - ',CAST(DATE_FORMAT(LeaveTo,'%d.%m.%Y')  as CHAR)) as CHAR) 'Msg' from salary_management.leaveentries where Employee_Code = '" + EmpCode + "' And LeaveFrom<= '" + OnDate + "' And '" + OnDate + "' <= LeaveTo And LeaveStatus = 'FullDay' And LeaveType!= 'SRL';", "Attendance");
                    if (D.Rows.Count > 0)
                    {
                        Message = D.Rows[0][0].ToString();
                        return true;
                    }
                }
            }
            return false;
        }

        

        public static string checktiming(string date, string category, string secid, string batch_id, string lecno, out string details, out bool nextcheck, out string nextchecktype, out string MinCheck)
        {
            MinCheck = "-1";
            string msg = "", q = "";
            details = "";

            MySqlConnection conLinq = MyConnections.MyConnectionString("Attendance");

            MySqlDataAdapter darchk = new MySqlDataAdapter("Select CAST(DATE_FORMAT(UpdatedOn,'%d %b, %Y %h:%i %p') as CHAR) 'LastUpdate',Lecture_ID from lecture_master where Batch_ID='" + batch_id + "' And Lecture_Date=DATE('" + date + "') and Lecture_No=" + lecno, conLinq);
            DataSet dthrchk = new DataSet();
            darchk.Fill(dthrchk);
            if (dthrchk.Tables[0].Rows.Count > 0)
            {
                details = "Last Updated : " + dthrchk.Tables[0].Rows[0][0].ToString();
                nextcheck = false;
                nextchecktype = "N";
                msg = "U";
            }
            else
            {
                q = "Select DISTINCT IF('" + date + "'>now(),'F','N') 'Status' from timetableinfobase";
                MySqlDataAdapter da_11 = new MySqlDataAdapter(q, conLinq);
                DataTable dt_11 = new DataTable();
                da_11.Fill(dt_11);

                if (dt_11.Rows.Count > 0 && dt_11.Rows[0][0].ToString() == "F")
                {
                    nextcheck = false;
                    nextchecktype = "F";
                    msg = "F";
                }
                else
                {


                    q = "Select  IF(DATE_ADD('" + date + "',INTERVAL -1 MINUTE)>now(),'F',IF(TIMEDIFF(now(),DATE_ADD('" + date + "',INTERVAL -1 MINUTE))<=AllowedTill,'O','E')) 'Status',AllowedTill from timetableinfobase where Type='" + category + "' And `Status`='Active';";
                    MySqlDataAdapter da = new MySqlDataAdapter(q, conLinq);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        MinCheck = dt.Rows[0]["AllowedTill"].ToString().ToUpper();
                        msg = dt.Rows[0]["Status"].ToString().ToUpper();
                        if (dt.Rows[0]["Status"].ToString().ToUpper() == "E")
                        {
                            string[] splt = batch_id.Split('#');
                            string conds = "";
                            {
                                conds = "Status='Open' And (CourseId='" + splt[0] + "' OR CourseId='" + splt[0] + "#" + splt[1] + "' OR CourseId='" + splt[0] + "#" + splt[1] + "#" + splt[2] + "' OR CourseId = '" + splt[0] + "#" + splt[1] + "#" + splt[2] + "#" + splt[3] + "' OR CourseId = '" + batch_id + "')";
                            }
                            q = "Select * from time_locks where  " + conds;
                            MySqlDataAdapter da1 = new MySqlDataAdapter(q, conLinq);
                            DataTable dt1 = new DataTable();
                            da1.Fill(dt1);

                            if (dt1.Rows.Count > 0)
                            {
                                MinCheck = "-1";
                                nextcheck = true;
                                nextchecktype = "F";
                            }
                            else
                            {
                                nextcheck = true;
                                nextchecktype = "P";
                            }
                        }
                        else
                        {
                            nextcheck = false;
                            nextchecktype = "F";
                        }
                    }
                    else
                    {
                        nextcheck = true;
                        nextchecktype = "F";
                        msg = "O";
                    }
                }
            }
            return msg;
        }

        public static string allowedtill(string empcode, string batchid, string sqldate, string lecno, out string details1, string nextchecktype, string overlock, DateTime termlock, string allowtill)
        {
            MySqlConnection conLinq = MyConnections.MyConnectionString("Attendance");
            string[] dats = sqldate.Split('-');
            DateTime dtm = new DateTime(Convert.ToInt32(dats[0]), Convert.ToInt32(dats[1]), Convert.ToInt32(dats[2]));
            details1 = "";

            if (nextchecktype == "F")
            {
                if (dtm > DateTime.Now.Date)
                {
                    return "Future";
                }

                if (overlock == "Y")
                {
                    return "Lock";
                }
                if (dtm < Convert.ToDateTime(termlock))
                {
                    return "Lock";
                }
                if (dtm < Convert.ToDateTime(allowtill))
                {
                    bool chk = true;
                    string[] splt = batchid.Split('#');
                    string conds = "";
                    if (splt[4] == "All")
                    {
                        conds = "And (Course_Code='" + splt[0] + "' OR Course_Code='" + splt[0] + "#" + splt[1] + "#" + splt[2] + "$Section" + "' OR Course_Code = '" + batchid.Replace("#All", "") + "$Register'  OR Course_Code = '" + batchid + "$Batch')";
                    }
                    else
                    {
                        conds = "And (Course_Code='" + splt[0] + "' OR Course_Code='" + splt[0] + "#" + splt[1] + "#" + splt[2] + "$Section" + "' OR Course_Code = '" + splt[0] + "#" + splt[1] + "#" + splt[2] + "#" + splt[3] + "$Register'  OR Course_Code = '" + batchid + "$Batch')";
                    }

                    MySqlDataAdapter darchk = new MySqlDataAdapter("Select * from rule where Status='Deactivated' " + conds, conLinq);
                    DataSet dthrchk = new DataSet();
                    darchk.Fill(dthrchk);

                    if (dthrchk.Tables[0].Rows.Count > 0)
                    {
                        return "Lock Open";
                    }
                    else
                    {
                        string tmp = "Select * from employee_token_for_attendance where Batch_ID = '" + batchid + "' And Employee_ID = '" + empcode + "' And Lecture_Date = '" + sqldate + "' And Status = 'N' And Date_of_token=DATE(now())  And Category='Upload Attendance By Faculty'";
                        MySqlDataAdapter dacnt = new MySqlDataAdapter(tmp, conLinq);
                        DataSet dtcnt = new DataSet();
                        dacnt.Fill(dtcnt);
                        if (dtcnt.Tables[0].Rows.Count > 0)
                        {
                            return "Token Open";
                        }
                    }
                }
                else
                {
                    return "Fine";
                }
                {

                }
                return "Lock";
            }
            else
            {
                if (overlock == "Y")
                {
                    return "Lock";
                }
                if (dtm < Convert.ToDateTime(termlock))
                {
                    return "Lock";
                }
                {
                    bool chk = true;
                    string[] splt = batchid.Split('#');
                    string conds = "";
                    if (splt[4] == "All")
                    {
                        conds = "And (Course_Code='" + splt[0] + "' OR Course_Code='" + splt[0] + "#" + splt[1] + "#" + splt[2] + "$Section" + "' OR Course_Code = '" + batchid.Replace("#All", "") + "$Register'  OR Course_Code = '" + batchid + "$Batch')";
                    }
                    else
                    {
                        conds = "And (Course_Code='" + splt[0] + "' OR Course_Code='" + splt[0] + "#" + splt[1] + "#" + splt[2] + "$Section" + "' OR Course_Code = '" + splt[0] + "#" + splt[1] + "#" + splt[2] + "#" + splt[3] + "$Register'  OR Course_Code = '" + batchid + "$Batch')";
                    }

                    MySqlDataAdapter darchk = new MySqlDataAdapter("Select * from rule where Status='Deactivated' " + conds, conLinq);
                    DataSet dthrchk = new DataSet();
                    darchk.Fill(dthrchk);

                    if (dthrchk.Tables[0].Rows.Count > 0)
                    {
                        return "Lock Open";
                    }
                    else
                    {
                        string tmp = "Select * from employee_token_for_attendance where Batch_ID = '" + batchid + "' And Employee_ID = '" + empcode + "' And Lecture_Date = '" + sqldate + "' And Status = 'N' And Date_of_token=DATE(now())  And Category='Upload Attendance By Faculty'";
                        MySqlDataAdapter dacnt = new MySqlDataAdapter(tmp, conLinq);
                        DataSet dtcnt = new DataSet();
                        dacnt.Fill(dtcnt);
                        if (dtcnt.Tables[0].Rows.Count > 0)
                        {
                            return "Token Open";
                        }
                    }
                }
                return "Lock";
            }
        }


        public static string ToTitle(string Text)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(Text.ToLower());
        }

        public static string Encrypt(string clearText)
        {
            string EncryptionKey = "GLA" + DateTime.Now.Year + "UNI" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "VER" + DateTime.Now.Day.ToString().PadLeft(2, '0') + "SITY" + DateTime.Now.Hour.ToString().PadLeft(2, '0');
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }

        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = "GLA" + DateTime.Now.Year + "UNI" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "VER" + DateTime.Now.Day.ToString().PadLeft(2, '0') + "SITY" + DateTime.Now.Hour.ToString().PadLeft(2, '0');
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return cipherText;
        }
        public static string GetSession()
        {
            string q = "Select * from current_session";
            DataTable dtempsub = MyConnections.Select(q, "Student");

            return dtempsub.Rows[0][0].ToString();
        }
        public static string GetMACAddress()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    IPInterfaceProperties properties = adapter.GetIPProperties();
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }
            }
            //string[] computer_name = System.Net.Dns.GetHostEntry(System.Web.HttpContext.Current.Request.ServerVariables["remote_addr"]).HostName.Split(new Char[] { '.' });
            //return computer_name[0].ToString().ToUpper() + "-" + sMacAddress;
            return System.Environment.MachineName + "-" + sMacAddress;
        }

        public static string GetLanIPAddress()
        {
            return System.Web.HttpContext.Current.Request.UserHostAddress;
        }
        public static string EncryptOrDecrypt(string text)
        {
            string newText = "";
            int key = 7;
            for (int i = 0; i < text.Length; i++)
            {
                int charValue = Convert.ToInt32(text[i]); //get the ASCII value of the character
                charValue ^= key; //xor the value

                newText += char.ConvertFromUtf32(charValue); //convert back to string
            }

            return newText;
        }
        public static void DeleteAttendanceInformation(string Course, string Semester, string Section, string Subject, string Batch, string Student_ID, string ForDate, string Status, string ActivityType, string EmployeeID, string EmloyeeName)
        {
            string q = "insert into delete_attendance_info (Session,Course,Semester,Section,Subject,Batch,Student_ID,ForDate,Status,ActivityType,EmployeeID,EmloyeeName,IP,MAC,DoneOn) values ('" + GetSession() + "','" + Course + "','" + Semester + "','" + Section + "','" + Subject + "','" + Batch + "','" + Student_ID + "'," + ForDate + ",'" + Status + "','" + ActivityType + "','" + EmployeeID + "','" + EmloyeeName + "','" + GetLanIPAddress() + "','" + GetMACAddress() + "',now())";
            MyConnections.DeleteInsertUpdate(q, "Student");
        }

        public static void UploadContinuousLectures(string batchid, string lecdat, int lecno, string lectime, string uploadedby, string lecid)
        {
            //And employeeid='"+uploadedby+"'
            DataTable M = MyConnections.Select("Select id,lectureno,extra1 from mytimetable where batchid='" + batchid + "' And end_date='" + lecdat + "'  And lectureno=" + (lecno + 1) + " And SUBSTRING_INDEX(extra1,' - ',1)= SUBSTRING_INDEX('" + lectime + "',' - ',-1) And `status`='Pending'  And employeeid='" + uploadedby + "';", "Attendance");
            if (M.Rows.Count > 0)
            {
                MyConnections.DeleteInsertUpdate("insert into lecture_master (Lecture_ID,Lecture_Date,Lecture_No,Batch_ID,Type,Weight,Topics,Subtopics,UploadedBy,UploadedOn,UploadedFrom,UpdatedBy,UpdatedOn,UpdatedFrom,LectureType,LectureTime) Select (Select IFNULL(MAX(A.Lecture_ID)+1,1) from lecture_master A),Lecture_Date,'" + (lecno + 1) + "',Batch_ID,Type,Weight,Topics,Subtopics,UploadedBy,now() as 'UploadedOn',UploadedFrom,UpdatedBy,now() as 'UpdatedOn',UpdatedFrom,LectureType,'" + M.Rows[0]["extra1"].ToString() + "' from lecture_master where Lecture_ID ='" + lecid + "';", "Attendance");
                DataTable LIds = MyConnections.Select("Select MAX(Lecture_ID) from lecture_master  where UploadedBy='" + uploadedby + "' And Lecture_ID>" + lecid + ";", "Attendance");
                if (LIds.Rows.Count > 0 && LIds.Rows[0][0].ToString().Length > 0)
                {
                    MyConnections.DeleteInsertUpdate("insert into lecture_attendance (Lecture_ID,Student_ID,Roll_No,`Status`,StatusOther,Rate,RateOn,`Comment`,LastUpdatedBy,LastUpdatedOn,LastUpdatedFrom) Select '" + LIds.Rows[0][0].ToString() + "',Student_ID,Roll_No,`Status`,StatusOther,Rate,RateOn,`Comment`,LastUpdatedBy,now() as 'LastUpdatedOn',LastUpdatedFrom from lecture_attendance where Lecture_ID ='" + lecid + "'; ", "Attendance");
                    MyConnections.DeleteInsertUpdate("update mytimetable set `status` = 'Completed',LecId="+ LIds.Rows[0][0].ToString() + " where id=" + M.Rows[0][0].ToString(), "Attendance");
                    UploadContinuousLectures(batchid, lecdat, lecno + 1, M.Rows[0]["extra1"].ToString(), uploadedby, LIds.Rows[0][0].ToString());
                }
            }
        }
    }
}