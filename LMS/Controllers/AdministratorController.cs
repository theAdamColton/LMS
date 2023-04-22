using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LMS.Controllers
{
    public class AdministratorController : Controller
    {
        private readonly LMSContext db;

        public AdministratorController(LMSContext _db)
        {
            db = _db;
        }

        

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Department(string subject)
        {
            ViewData["subject"] = subject;
            return View();
        }

        public IActionResult Course(string subject, string num)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Create a department which is uniquely identified by it's subject code
        /// </summary>
        /// <param name="subject">the subject code</param>
        /// <param name="name">the full name of the department</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the department already exists, true otherwise.</returns>
        public IActionResult CreateDepartment(string subject, string name)
        {
            try
            {
                if (Regex.IsMatch(subject, @"^\s*$") || Regex.IsMatch(name, @"^\s*$")) return Json(new { success = false });
               
                var department = new Department();
                department.Name = name;
                department.Subject = subject;
                db.Departments.Add(department);
                db.SaveChanges();
            } catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message); 
                return Json(new {success = false});
            }
            return Json(new {success = true});
        }


        /// <summary>
        /// Returns a JSON array of all the courses in the given department.
        /// Each object in the array should have the following fields:
        /// "number" - The course number (as in 5530)
        /// "name" - The course name (as in "Database Systems")
        /// </summary>
        /// <param name="subjCode">The department subject abbreviation (as in "CS")</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetCourses(string subject)
        {
            var query = (
                from course in db.Courses
                where course.Department == subject
                select new
                {
                    number = course.Number,
                    name = course.Name
                }
                );
           return Json(query.ToArray());
        }

        /// <summary>
        /// Returns a JSON array of all the professors working in a given department.
        /// Each object in the array should have the following fields:
        /// "lname" - The professor's last name
        /// "fname" - The professor's first name
        /// "uid" - The professor's uid
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetProfessors(string subject)
        {
            var query = (
                from pro in db.Professors
                where subject == pro.WorksIn
                select new
                {
                    lname = pro.LName,
                    fname = pro.FName,
                    uid = pro.UId
                });
            //var professers = db.Professors.Where(p => p.WorksInNavigation.Subject == subject);
            //var resultList = professers.Select(p => new { lname = p.LName, fname = p.FName, uid = p.UId }).ToList();
            return Json(query.ToArray());
            
        }



        /// <summary>
        /// Creates a course.
        /// A course is uniquely identified by its number + the subject to which it belongs
        /// </summary>
        /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
        /// <param name="number">The course number</param>
        /// <param name="name">The course name</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the course already exists, true otherwise.</returns>
        public IActionResult CreateCourse(string subject, int number, string name)
        {
            try
            {
                if((from course in db.Courses
                    where course.Department == subject
                    && course.Number == number
                    select course).FirstOrDefault() != null)
                {
                    System.Diagnostics.Debug.WriteLine("duplicate subject and number");
                    return Json(new { success = false });
                }
                var newCourse = new Course();
                newCourse.Department = subject;
                newCourse.Number = (uint)number;
                newCourse.Name = name;
                db.Courses.Add(newCourse); 
                db.SaveChanges();
                return Json(new { success = true });
            } catch (Exception ex) { 
                System.Diagnostics.Debug.WriteLine("Create course error",ex.Message); 
                return Json(new { success = false });
            }
        }



        /// <summary>
        /// Creates a class offering of a given course.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="number">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="location">The location</param>
        /// <param name="instructor">The uid of the professor</param>
        /// <returns>A JSON object containing {success = true/false}. 
        /// false if another class occupies the same location during any time 
        /// within the start-end range in the same semester, or if there is already
        /// a Class offering of the same Course in the same Semester,
        /// true otherwise.</returns>
        public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor)
        {
            var course = (
                from courses in db.Courses
                where courses.Department == subject
                select courses).SingleOrDefault();

            //if department doesn't exists return false
            if (course == null) return Json(new { success = false });
            


            var startTime = TimeOnly.FromDateTime(start);
            var endTime = TimeOnly.FromDateTime(end);

            // is true if there exists any classes of any course that are in the
            // same location during any time in the start-end range of the same
            // semester

            var anySameSemesterAndLocationClasses = 
                (
                from classes in db.Classes
                where classes.Season == season 
                && classes.Year == year
                && ((classes.StartTime <= startTime) && (classes.EndTime <= endTime)) 
                && classes.Location == location
                select classes
                ).Any();

            //var anySameSemesterAndLocationClasses = db.Classes.Where(
            //    c => c.Season == season && 
            //    c.Year == year && 
            //    ((c.StartTime <= startTime) && (c.EndTime <= endTime)) &&
            //    c.Location == location
            //).Any();
            if (anySameSemesterAndLocationClasses)
            {
                System.Diagnostics.Debug.WriteLine("COlliding class exists!!!!  CANT CREATE CLASS"); 
                return Json(new { success = false});
            }

            // is true if there exists any offering of course during the same semester and year
            var anyClassOfferingOfSameCourseSameSemester = 
                (from classes in course.Classes
                 where classes.Season == season
                 && classes.Year == year
                 select classes). Any();
            //course.Classes.Where(c => c.Season == season && c.Year == year).Any();
            if (anyClassOfferingOfSameCourseSameSemester)
            {
                System.Diagnostics.Debug.WriteLine("THIS COURSE IS ALREADY OFFERED THIS SAME SEASON AND YEAR!!!");
                return Json(new { success = false});
            }

            var newClass = new Class();
            newClass.StartTime = startTime;
            newClass.EndTime = endTime;
            newClass.Year = (uint)year;
            newClass.Season = season;
            newClass.Location = location;
            newClass.TaughtBy = instructor;
            newClass.ListingNavigation = course;
            db.Add(newClass);
            db.SaveChanges();
            return Json(new { success = true});
        }


        /*******End code to modify********/

    }
}

