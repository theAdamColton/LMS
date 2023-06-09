﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private LMSContext db;
        public StudentController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Catalog()
        {
            return View();
        }

        public IActionResult Class(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }


        public IActionResult ClassListings(string subject, string num)
        {
            System.Diagnostics.Debug.WriteLine(subject + num);
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }


        /*******Begin code to modify********/

        /// <summary>
        /// Returns a JSON array of the classes the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester
        /// "year" - The year part of the semester
        /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var query = (
                from s in db.Enrolleds join cl in db.Classes on
                s.Class equals cl.ClassId
                join c in db.Courses on cl.Listing equals c.CatalogId
                where s.Student == uid
                select new
                {
                    subject = c.Department,
                    number = c.Number,
                    name = c.Name,
                    season = cl.Season,
                    year = cl.Year,
                    grade = s.Grade
                });

            return Json(query.ToArray());

        }

        /// <summary>
        /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The category name that the assignment belongs to
        /// "due" - The due Date/Time
        /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid"></param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid)
        {
            var query = (
                from course in db.Courses 
                join classes in db.Classes
                on course.CatalogId equals classes.Listing
                into classListings

                from c in classListings.DefaultIfEmpty()
                join assCat in db.AssignmentCategories  
                on c.ClassId equals assCat.InClass
                into catTable

                from cat in catTable.DefaultIfEmpty()
                join ass in db.Assignments
                on cat.CategoryId equals ass.Category
                into assTable

                from ass in assTable.DefaultIfEmpty()
                join sub in db.Submissions
                on ass.AssignmentId equals sub.Assignment

                where course.Department == subject 
                && course.Number == num
                && c.Season == season
                && c.Year == year
                && sub.Student == uid
                select new
                {
                    aname = ass.Name,
                    cname = cat.Name,
                    due = ass.Due,
                    score = sub.Score,
                });
            return Json(query.ToArray());
        }



        /// <summary>
        /// Adds a submission to the given assignment for the given student
        /// The submission should use the current time as its DateTime
        /// You can get the current time with DateTime.Now
        /// The score of the submission should start as 0 until a Professor grades it
        /// If a Student submits to an assignment again, it should replace the submission contents
        /// and the submission time (the score should remain the same).
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="uid">The student submitting the assignment</param>
        /// <param name="contents">The text contents of the student's submission</param>
        /// <returns>A JSON object containing {success = true/false}</returns>
        public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
          string category, string asgname, string uid, string contents)
        {
            try 
            { 
                uint? assID = (
                from course in db.Courses 
                join classes in db.Classes
                on course.CatalogId equals classes.Listing
                into classTable

                from classes in classTable.DefaultIfEmpty()
                join cat in db.AssignmentCategories
                on classes.ClassId equals cat.InClass
                into catTable
                
                from cat in catTable.DefaultIfEmpty()
                join ass in db.Assignments
                on cat.CategoryId equals ass.Category
                where category == cat.Name
                && asgname == ass.Name
                && season == classes.Season
                && year == classes.Year
                select ass.AssignmentId).SingleOrDefault();

                var submissionQuery = (
                    from sub in db.Submissions
                    where sub.Assignment == assID
                    && sub.Student == uid
                    select sub
                );
                
                if(submissionQuery.FirstOrDefault() == null)
                {
                    Submission submission = new Submission();
                    submission.Assignment = assID.Value;
                    submission.Student = uid;
                    submission.SubmissionContents = contents;
                    submission.Score = 0;
                    submission.Time = DateTime.Now;
                    db.Add(submission);
                    db.SaveChanges();
                }
                else
                {
                    submissionQuery.ToList().ForEach(submission =>
                    {
                        submission.SubmissionContents = contents;
                        submission.Time = DateTime.Now;
                        db.SaveChanges();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false });
            }

            return Json(new { success = true });
        }


        /// <summary>
        /// Enrolls a student in a class.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing {success = {true/false}. 
        /// false if the student is already enrolled in the class, true otherwise.</returns>
        public IActionResult Enroll(string subject, int num, string season, int year, string uid)
        {
            try
            {
                var cl = (
                    from courses in db.Courses
                    join classes in db.Classes
                    on courses.CatalogId equals classes.Listing
                    where courses.Department == subject
                    && courses.Number == num
                    && classes.Season == season
                    && classes.Year == year
                    select new
                    {
                        classId = classes.ClassId
                    }

                ).FirstOrDefault();
                if (cl == null) return Json(new { success = false });

                var enrollment = new Enrolled();
                enrollment.Student = uid;
                enrollment.Class = cl.classId;
                enrollment.Grade = "--";
                db.Add(enrollment);
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch(Exception ex)
            {
                return Json(new { success = false });
            }
        }


        /// <summary>
        /// Calculates a student's GPA
        /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
        /// Assume all classes are 4 credit hours.
        /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
        /// If a student is not enrolled in any classes, they have a GPA of 0.0.
        /// Otherwise, the point-value of a letter grade is determined by the table on this page:
        /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
        public IActionResult GetGPA(string uid)
        {
            var student = db.Students.Where(s => s.UId == uid).Single();
            var grades = student.Enrolleds.Select(e => e.Grade).ToArray();
            double total_grade_points = grades.Select(g => {
                if (g == "A")
                    return 4.0;
                if (g == "A-")
                    return 3.7;
                if (g == "B+")
                    return 3.3;
                if (g == "B")
                    return 3.0;
                if (g == "C+")
                    return 2.3;
                if (g == "C")
                    return 2.0;
                if (g == "C-")
                    return 1.7;
                if (g == "D+")
                    return 1.3;
                if (g == "D")
                    return 1.0;
                if (g == "D-")
                    return 0.7;
                if (g == "E")
                    return 0.0;
                if (g == "--")
                    return 0.0;
                return -100000000000.0;
            }).Sum();

            // number of classes where the student has some grade that is not '--'
            var num_gps_to_count = grades.Where(g => g != "--").Count();
            double gpa = num_gps_to_count == 0? 0.0 : total_grade_points / num_gps_to_count;

            return Json(new { gpa = gpa });
        }
                
        /*******End code to modify********/

    }
}

