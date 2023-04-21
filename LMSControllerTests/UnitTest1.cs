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
            try
            {


                LMSContext con = MakeTinyDB();
                AdministratorController c = new AdministratorController(con);

                var quid2 = c.CreateDepartment("QUID", "Quidditch") as JsonResult;
                var Botany = c.CreateDepartment("BOT", "Botany") as JsonResult;
                var trans = c.CreateDepartment("TRAN", "Tranfiguration") as JsonResult;
                var empty = c.CreateDepartment("", "") as JsonResult;
                if (Botany == null || quid2 == null || trans == null || empty == null) throw new("null queries");
                var query = (
                    from dep in con.Departments
                    select dep
                    );
                Assert.Equal(3, query.Count());
                

                dynamic? botValue = Botany.Value;
                Assert.True((bool?)botValue?.GetType()?.GetProperty("success")?.GetValue(botValue, null));
                Assert.Equal("Botany", query.ToArray()[1].Name);
                Assert.Equal("BOT", query.ToArray()[1].Subject);

                dynamic? transValue = trans.Value;
                Assert.True((bool?)transValue?.GetType()?.GetProperty("success")?.GetValue(transValue, null));
                Assert.Equal("Transfiguration", query.ToArray()[2].Name);
                Assert.Equal("TRAN", query.ToArray()[2].Subject);

                dynamic? quid2Result = quid2.Value;
                Assert.False((bool?) quid2Result?.GetType()?.GetProperty("success")?.GetValue(quid2Result, null));;


                dynamic? emptyValue = empty.Value;
                Assert.True((bool?) emptyValue?.GetType()?.GetProperty("success")?.GetValue(emptyValue, null));
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.ToString());
            }

        }
        /// <summary>
        /// First checks if create course works, and then checks if the get courses works too
        /// </summary>
        [Fact]
        public void TestCreateAndGetCourse()
        {
            LMSContext con = MakeTinyDB();
            AdministratorController c = new AdministratorController(con);
            JsonResult? errquid1 = c.CreateCourse("QUID", 1010, "Quidditch I") as JsonResult;
            JsonResult? quid2 = c.CreateCourse("QUID", 1020, "Quidditch II") as JsonResult;
            JsonResult? trans1 = c.CreateCourse("TRAN", 1010, "Intro to Transfiguration") as JsonResult;

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
            Assert.Equal("QUID", qquery.ToArray()[1].dept);
            Assert.Equal((uint) 1020, qquery.ToArray()[1].number);
            Assert.Equal("Quidditch II", qquery.ToArray()[1].name);

            if (errquid1 == null || errquid1.Value == null || errquid1.Value.ToString() == null) Assert.Fail("errquid should not be null");
            String? errquidString = errquid1.Value.ToString();
            Assert.Equal("{ success = False }", errquidString);

            if(trans1 == null || trans1.Value == null || trans1.Value.ToString() == null) Assert.Fail("errquid should not be null");
            String? trans1String = trans1.Value.ToString();
            Assert.Equal("{ success = True }", trans1String); 
            
            c.CreateDepartment("TRAN", "Transfiguration");
            trans1 = c.CreateCourse("TRAN", 1010, "Intro to Transfiguration") as JsonResult;
            if (trans1 == null || trans1.Value == null || trans1.Value.ToString() == null) Assert.Fail("should not be null");
            trans1String = trans1.Value.ToString();
            Assert.Equal("{ success = False }", trans1String); 
            
            JsonResult? courses = c.GetCourses("QUID") as JsonResult;
            if (courses == null || courses.Value == null) Assert.Fail("Courses should not be null");
            dynamic x = courses.Value;
            Assert.Equal(2, x.Length);
            Assert.Equal(1010, (int) x[0].GetType().GetProperty("number").GetValue(x[0], null));
            Assert.Equal("Quidditch I", (String) x[0].GetType().GetProperty("name").GetValue(x[0], null));

            Assert.Equal(1020, (int) x[1].GetType().GetProperty("numbber").GetValue(x[0], null));
            Assert.Equal("Quidditch II", (String) x[1].GetType().GetProperty("name").GetValue(x[0], null));
            
        }

        [Fact]
        public void TestCreateClass()
        {
            LMSContext con = MakeTinyDB();
            AdministratorController c = new AdministratorController(con);
            var quidI2020 = c.CreateClass("QUID", 1010, "Fall", 2020, new DateTime(2019, 8, 1), new DateTime(2020, 1, 10), "Field", "Potter");
            var errquidI2020 = c.CreateClass("QUID", 1010, "Fall", 2020, new DateTime(3000, 8, 1), new DateTime(3002, 1, 10), "OtherField", "notPotter");
            var errquid2 = c.CreateClass("SPOR", 1010, "Fall", 2020, new DateTime(2020, 1, 1), new DateTime(2021, 10, 10), "Field", "notPotter");
            var classQuery = (from classes in con.Classes
                              select classes);
            Assert.Equal(1, classQuery.Count());
            Assert.Equal("Potter", classQuery.First().TaughtBy);
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
            //Assert.Equal("CS", x[0].subject);
        }


        /// <summary>
        /// Make a very tiny in-memory database, containing just one department
        /// and nothing else.
        /// </summary>
        /// <returns></returns>
        LMSContext MakeTinyDB()
        {
            var contextOptions = new DbContextOptionsBuilder<LMSContext>()
            .UseInMemoryDatabase("LMSControllerTest").ConfigureWarnings(warnings => warnings.Default(WarningBehavior.Ignore))
            .UseApplicationServiceProvider(NewServiceProvider())
            .Options;

            LMSContext testdb = new LMSContext(contextOptions);
            Department dep = new Department();
            dep.Name = "Quidditch";
            dep.Subject = "QUID";
            testdb.Departments.Add(dep);
            Course quid1 = new Course();

            quid1.Department = "QUID";
            quid1.Number = 1010;
            quid1.Name = "Quidditch 1";
            testdb.Courses.Add(quid1);

            testdb.SaveChanges();
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