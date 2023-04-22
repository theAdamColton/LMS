using LMS.Areas.Identity.Pages.Account;
using LMS.Controllers;
using LMS.Models.LMSModels;
using LMS_CustomIdentity.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;

namespace LMSControllerTests
{
    public class UnitTest1
    {
        
        // Uncomment the methods below after scaffolding
        // (they won't compile until then)
        [Fact]
        public void TestAddDepartment()
        {
            LMSContext con = MakeTinyDB();
            AdministratorController c = new AdministratorController(con);
            var quid1 = c.CreateDepartment("Quidditch", "QUID") as JsonResult;
            var quid2 = c.CreateDepartment("Quidditch", "QUID") as JsonResult;
            var trans1 = c.CreateDepartment("Tranfiguration", "TRAN") as JsonResult;
            var empty = c.CreateDepartment("", "") as JsonResult;
            var quidQuery = (
                from dep in con.Departments
                where dep.Name == "QUID"
                select new
                {
                    subject = dep.Subject,
                    name = dep.Name
                }
                );
            Assert.Equal(1, quidQuery.Count());
            Assert.Equal("Quidditch", quidQuery.First().subject);
            Assert.Equal(1, quidQuery.Count());
            Assert.Equal("QUID", quidQuery.First().name);
            if (quid2 != null && quid2.Value != null)
            {
                String? x = quid2.Value.ToString();
                if (x != null) { Assert.Equal("{ success = False }", x); }
            }
            if(quid1 != null && quid1.Value != null)
            {
                string? x = quid1.Value.ToString();
                if (x != null) { Assert.Equal("{ success = True }", x); }
            }
            if (empty != null && empty.Value != null)
            {
                string? x = empty.Value.ToString();
                if (x != null) { Assert.Equal("{ success = False }", x); }
            }


            // AdministratorController administratorController = new AdministratorController(context);
            // var result = administratorController.CreateDepartment("Quidditch", "QUID");
            // Assert.True(result["success"]);

        }
        /// <summary>
        /// First checks if create course works, and then checks if the get courses works too
        /// </summary>
        [Fact]
        public void TestCreateAndGetCourse()
        {
            LMSContext con = MakeTinyDB();
            AdministratorController c = new AdministratorController(con);
            var quid1 = c.CreateCourse("QUID", 1010, "Seeker School I");
            var errquid1 = c.CreateCourse("QUID", 1010, "Seeker School II");
            var quid2 = c.CreateCourse("QUID", 1020, "Seeker");
            var trans1 = c.CreateCourse("TRAN", 1010, "Intro to Transfiguration");
            var trans2 = c.CreateCourse("TRAN", 2010, "Transfiguration");

            var qquery = (
                from course in con.Courses
                where course.Department == "QUID"
                select new
                {
                    dept = course.Department,
                    number = course.Number,
                    name = course.Name,
                });
            //ensures that a repeated insert doesn't work
            Assert.Equal(2, qquery.Count());
            Assert.Equal("QUID", qquery.First().dept);
            Assert.Equal((uint) 1010, qquery.First().number);
        }

        [Fact]
        public void Test1()
        {
            LMSContext con = MakeTinyDB();
            // An example of a simple unit test on the CommonController
            CommonController ctrl = new CommonController(con);

            var allDepts = ctrl.GetDepartments() as JsonResult;

            dynamic x = allDepts.Value;

            Assert.Equal(1, x.Length);
            Assert.Equal("CS", x[0].subject);
        }


        /// <summary>
        /// Make a very tiny in-memory database, containing just one department
        /// and nothing else.
        /// </summary>
        /// <returns></returns>
        LMSContext MakeTinyDB()
        {
            var contextOptions = new DbContextOptionsBuilder<LMSContext>()
            .UseInMemoryDatabase("LMSControllerTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .UseApplicationServiceProvider(NewServiceProvider())
            .Options;

            LMSContext testdb = new LMSContext(contextOptions);

            //testdb.Database.EnsureDeleted();
            //testdb.Database.EnsureCreated();

            //db.Departments.Add(new Department { Name = "KSoC", Subject = "CS" });

            // TODO: add more objects to the test database

            //testdb.SaveChanges();

            return testdb;
        }

        private static ServiceProvider NewServiceProvider()
        {
            var serviceProvider = new ServiceCollection()
          .AddEntityFrameworkInMemoryDatabase()
          .BuildServiceProvider();

            return serviceProvider;
        }

    }
}