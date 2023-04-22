using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS_CustomIdentity.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {

        private readonly LMSContext db;

        public ProfessorController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Students(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
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

        public IActionResult Categories(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult CatAssignments(string subject, string num, string season, string year, string cat)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
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

        public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }

        public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            ViewData["uid"] = uid;
            return View();
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Updates the grade of this student, in this class
        /// </summary>
        /// <param name="student"></param>
        /// <param name="thisClass"></param>
        void UpdateGrade(Student student, Class thisClass)
        {
            var enr = thisClass.Enrolleds.Where(enr => enr.Student == student.UId).Single();

            double allTotalUnscaled = thisClass.AssignmentCategories.Select(
                // assignment category weight * (sum)
                ac => {
                    // First calculates the percentage of (total points earned / total max
                    // points) of all assignments in the category. This should be a
                    // number between 0.0 - 1.0. For example, if there are 875 possible
                    // points spread among various assignments in the category, and the
                    // student earned a total of 800 among all assignments in that
                    // category, this value should be ~0.914

                    // Total of all earned points across all assignments in this ac AssignmentCategory
                    int earnedPoints = ac.Assignments.Select(ass =>
                        {
                            var sub = ass.Submissions.Where(sub => sub.Student == student.UId).SingleOrDefault();
                            return sub == null ? 0: (int)sub.Score;
                        }
                    ).Sum();
                    // Total points across all assignments
                    int totalPoints = ac.Assignments.Select(ass => (int)ass.MaxPoints).Sum();

                    // Avoids dividing by zero. If the total points is zero, then the score is 1.0 / 1.0
                    double score = totalPoints == 0? 1.0 :  (double)earnedPoints / (double)totalPoints;

                    // Multiplies the percentage by the category weight. For
                    // example, if the category weight is 15, then the scaled
                    // total for the category is ~13.71 (using the previous
                    // example).
                    double scaledScore = score * (double)ac.Weight;
                    return scaledScore;
                }
            ).Sum();
            // IMPORTANT - there is no rule that assignment
            // category weights must sum to 100. Therefore, we have to re-scale
            var totalWeights = thisClass.AssignmentCategories.Select(ac => (int)ac.Weight).Sum();
            // Avoids dividing by zero; in the case that all assignment weights
            // are zero, the total scaled score is 100.0 / 100.0
            double allTotalScaled = totalWeights == 0 ? 100.0 : (double)allTotalUnscaled * (100 / (double)totalWeights);

            // Letter 	Scoring
            //A <= 100 % -93 %
            //A -  < 93 % -90 %
            //B +  < 90 %–87 %
            //B < 87 %–83 %
            //B -  < 83 % -80 %
            //C +  < 80 %–77 %
            //C < 77 %–73 %
            //C -  < 73 % -70 %
            //D +  < 70 %–67 %
            //D < 67 %–63 %
            //D -  < 63 % -60 %
            //E < 60 %

            string grade = null;
            if (allTotalScaled >= 93)
                grade = "A";
            else if (allTotalScaled >= 90)
                grade = "A-";
            else if (allTotalScaled >= 87)
                grade = "B+";
            else if (allTotalScaled >= 83)
                grade = "B";
            else if (allTotalScaled >= 80)
                grade = "B-";
            else if (allTotalScaled >= 77)
                grade = "C+";
            else if (allTotalScaled >= 73)
                grade = "C";
            else if (allTotalScaled >= 70)
                grade = "C-";
            else if (allTotalScaled >= 67)
                grade = "D+";
            else if (allTotalScaled >= 63)
                grade = "D";
            else if (allTotalScaled >= 60)
                grade = "D-";
            else
                grade = "E";

            enr.Grade = grade;
            db.Update(enr);
            db.SaveChanges();
        }


        /// <summary>
        /// Returns a JSON array of all the students in a class.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "dob" - date of birth
        /// "grade" - the student's grade in this class
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetStudentsInClass(string subject, int num, string season, int year)
        {
            try
            {
                var query = (
                    from courses in db.Courses
                    join
                    classes in db.Classes
                    on courses.CatalogId equals classes.Listing
                    into classesTable

                    from classes in classesTable.DefaultIfEmpty()
                    join enrolled in db.Enrolleds
                    on classes.ClassId equals enrolled.Class
                    into enrolledTable

                    from enrolls in enrolledTable.DefaultIfEmpty()
                    join stud in db.Students
                    on enrolls.Student equals stud.UId
                    into studentsTable

                    where
                    courses.Department == subject
                    && courses.Number == num
                    && classes.Season == season
                    && classes.Year == year

                    from astud in studentsTable
                    select new
                    {
                        fname = astud.FName,
                        lname = astud.LName,
                        uid = astud.UId,
                        dob = astud.Dob,
                        grade = enrolls.Grade

                    }

                    );

                return Json(query.ToArray());
            }
            catch(Exception exp)
            {
                return Json(null);
            }
        }



        /// <summary>
        /// Returns a JSON array with all the assignments in an assignment category for a class.
        /// If the "category" parameter is null, return all assignments in the class.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The assignment category name.
        /// "due" - The due DateTime
        /// "submissions" - The number of submissions to the assignment
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class, 
        /// or null to return assignments from all categories</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category)
        {
            // Contains the class (if any) satisfying:
            //  subject of department matches
            //  number of course matches
            //  season and year matches
            var thisCourse = db.Courses.Where(c => c.Number == num && c.Department == subject).Single();
            var thisClass = thisCourse.Classes.Where(c => c.Season == season && c.Year == year).Single();

            ICollection<Assignment> assignments = null;
            if (category == null)
            {
                assignments = db.Assignments.Where(ass => ass.CategoryNavigation.InClass == thisClass.ClassId).ToList();
            }
            else
            {
                assignments = db.Assignments.Where(ass => ass.CategoryNavigation.Name == category && ass.CategoryNavigation.InClass == thisClass.ClassId).ToList();
            }

            return Json(assignments.Select(a => new {aname = a.Name, cname = a.CategoryNavigation.Name, due = a.Due, submissions = a.Submissions.Count }).ToList());
        }


        /// <summary>
        /// Returns a JSON array of the assignment categories for a certain class.
        /// Each object in the array should have the folling fields:
        /// "name" - The category name
        /// "weight" - The category weight
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentCategories(string subject, int num, string season, int year)
        {
            var thisClass = db.Classes.Where(c => c.ListingNavigation.Department == subject && c.ListingNavigation.Number == num && c.Season == season && c.Year == year).SingleOrDefault();
            var categories = thisClass.AssignmentCategories;
            return Json(categories.Select(cat => new
            {
                name = cat.Name,
                weight = cat.Weight
            }).ToList());
        }

        /// <summary>
        /// Creates a new assignment category for the specified class.
        /// If a category of the given class with the given name already exists, return success = false.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The new category name</param>
        /// <param name="catweight">The new category weight</param>
        /// <returns>A JSON object containing {success = true/false} </returns>
        public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight)
        {
            Class thisClass = db.Classes.Where(c => c.ListingNavigation.Department == subject && c.ListingNavigation.Number == num && c.Season == season && c.Year == year).SingleOrDefault();
            if (thisClass == null)
                return Json(new { success = false });

            bool isAssignmentCategory = thisClass.AssignmentCategories.Where(cat => cat.Name == category).Any();
            if (isAssignmentCategory)
                return Json(new { success = false });

            AssignmentCategory assignmentCategory = new AssignmentCategory();
            assignmentCategory.Name = category;
            assignmentCategory.Weight = (uint)catweight;
            assignmentCategory.InClassNavigation = thisClass;
            db.Add(assignmentCategory);
            db.SaveChanges();
            return Json(new { success = true });
        }

        /// <summary>
        /// Creates a new assignment for the given class and category.
        /// Updates students grades in Enrolled. By default, on a new assignment all students have zeros
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="asgpoints">The max point value for the new assignment</param>
        /// <param name="asgdue">The due DateTime for the new assignment</param>
        /// <param name="asgcontents">The contents of the new assignment</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents)
        {
            var thisClass = db.Classes.Where(c => c.ListingNavigation.Department == subject && c.ListingNavigation.Number == num && c.Season == season && c.Year == year).SingleOrDefault();
            if (thisClass == null)
                return Json(new { success = false });

            var assignmentCategory = thisClass.AssignmentCategories.Where(ac => ac.Name == category).SingleOrDefault();
            if (assignmentCategory == null)
                return Json(new { success = false });

            var assignment = new Assignment();
            assignment.Name = asgname;
            assignment.MaxPoints = (uint)asgpoints;
            assignment.Due = asgdue;
            assignment.Contents = asgcontents;
            assignment.CategoryNavigation = assignmentCategory;
            db.Add(assignment);
            db.SaveChanges();

            // updates the grades of all students in this class
            foreach (Student student in thisClass.Enrolleds.Select(enr => enr.StudentNavigation)) {
                UpdateGrade(student, thisClass);
            }

            return Json(new { success = true });
        }


        /// <summary>
        /// Gets a JSON array of all the submissions to a certain assignment.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "time" - DateTime of the submission
        /// "score" - The score given to the submission
        /// 
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname)
        {
            var thisClass = db.Classes.Where(c => c.ListingNavigation.Department == subject && c.ListingNavigation.Number == num && c.Season == season && c.Year == year).SingleOrDefault();
            var assignment = db.Assignments.Where(ass => ass.Name == asgname && ass.CategoryNavigation.Name == category && ass.CategoryNavigation.InClass == thisClass.ClassId).SingleOrDefault();
            return Json(assignment.Submissions.Select(sub => new { fname = sub.StudentNavigation.FName, lname = sub.StudentNavigation.LName, uid = sub.StudentNavigation.UId, time = sub.Time, score=sub.Score }).ToArray());
        }


        /// <summary>
        /// Set the score of an assignment submission
        /// Updates the student's grade
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <param name="uid">The uid of the student who's submission is being graded</param>
        /// <param name="score">The new score for the submission</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score)
        {
            try
            {
                uint? subid = (
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

                var query =
                    (
                    from asub in db.Submissions
                    where asub.Student == uid
                    && asub.Assignment == subid
                    select asub
                    ).SingleOrDefault() ;


                var thisClass = db.Classes.Where(c => c.ListingNavigation.Department == subject && c.ListingNavigation.Number == num && c.Season == season && c.Year == year).SingleOrDefault();
                var student = db.Students.Where(s => s.UId == uid).SingleOrDefault();

                if (thisClass == null || student == null || query == null)
                    return Json(new { success = false });

                query.Score = (uint)score;
                db.Update(query);
                db.SaveChanges();

                // updates grade
                UpdateGrade(student, thisClass);

                return Json(new { success = true });
            }
            catch(Exception exp)
            {
                return Json(new { success = false });
            }
        }


        /// <summary>
        /// Returns a JSON array of the classes taught by the specified professor
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester in which the class is taught
        /// "year" - The year part of the semester in which the class is taught
        /// </summary>
        /// <param name="uid">The professor's uid</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {
            var query = (
                    from classes in db.Classes
                    join courses in db.Courses
                    on classes.Listing equals courses.CatalogId
                    where classes.TaughtBy == uid
                    select new
                    {
                        subject = courses.Department,
                        number = courses.Number,
                        name = courses.Name,
                        season = classes.Season,
                        year = classes.Year,
                    }
                );
            return Json( query.ToArray());
        }


        
        /*******End code to modify********/
    }
}

