using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ContosoUniversity.DAL;
using ContosoUniversity.Models;

namespace ContosoUniversity.Controllers
{
    // most of the calls in the DepartmentController use async - await
    // pairs for reaching the concurrency primitives in the language.
    // the async code is not thread safe. in other words don't try to do multiple
    // operations in parallel using the same context instance.
    // if you want to take advantage of the performance benefits of async code,
    // make sure that any library packages you're using (such as for paging, etc.) 
    // also use async code if they are calling any EF methods that cause queries to the database 
    public class DepartmentController : Controller
    {
        private SchoolContext db = new SchoolContext();

        // GET: Department
        public async Task<ActionResult> Index()
        {
            // info about concurrency - async and await
            // method signature tells compiler to generate callbacks for the 
            // sections marked await and create the Task obj to be returned
            // Task<T> type represents ongoign work with a result of type T

            var departments = db.Departments.Include(d => d.Administrator);

            // await keyword applied to the web service call - 
            // when the compiler sees await it splits the method into 2 parts
            // 1 -first part ends with the operation that is started asynchronously
            // 2 -second part is put into a callback method that is called 
            // when the operation completes
            return View(await departments.ToListAsync());

            // the declaration of departments sets up a query to the db
            // but the query is not executed until the ToList is called.
            // that is the only part we will await
        }

        // GET: Department/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Find() exectues the db query so it gets the await keyword
            Department department = await db.Departments.FindAsync(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            return View(department);
        }

        // GET: Department/Create
        public ActionResult Create()
        {
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName");
            return View();
        }

        // POST: Department/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "DepartmentID,Name,Budget,StartDate,InstructorID")] Department department)
        {
            if (ModelState.IsValid)
            {
                // Add is not a query to the db - just creating the C# object in memory
                db.Departments.Add(department);

                //  here is the async call - with await tag in front
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // GET: Department/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // here is the actual execute query instruction - w/ await keyword
            Department department = await db.Departments.FindAsync(id);
            if (department == null)
            {
                return HttpNotFound();
            }
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", department.InstructorID);
            return View(department);
        }

        // POST: Department/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int? id, byte[] rowVersion)
        {
            // orig signature
            //public async Task<ActionResult> Edit([Bind(Include = "DepartmentID,Name,Budget,StartDate,InstructorID")] Department department)

            // 
            string[] fieldsToBind = new string[] { "Name", "Budget", "StartDate", "InstructorID", "RowVersion" };
            if(id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // here is the possible concurrent call - 
            var departmentToUpdate = await db.Departments.FindAsync(id);
            if(departmentToUpdate == null)
            {
                // If the FindAsync method returns null, the department was deleted by another user. 
                // The code shown uses the posted form values to create a department entity so that the 
                // Edit page can be redisplayed with an error message. As an alternative, you wouldn't 
                // have to re-create the department entity if you 
                // display only an error message without redisplaying the department fields.
                Department deletedDepartment = new Department();
                TryUpdateModel(deletedDepartment, fieldsToBind);
                ModelState.AddModelError(string.Empty,
                    "Unable to save changes. The department was deleted by another user.");
                ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", deletedDepartment.InstructorID);
                return View(deletedDepartment);
            }

            if (TryUpdateModel(departmentToUpdate, fieldsToBind))
            {
                try
                {
                    //The view stores the original RowVersion value in a hidden field, and the method receives it in the 
                    //rowVersion parameter. Before you call SaveChanges, you have to put that original RowVersion property 
                    //value in the OriginalValues collection for the entity. Then when the Entity Framework creates a 
                    //SQL UPDATE command, that command will include a 
                    //WHERE clause that looks for a row that has the original RowVersion value.
                    db.Entry(departmentToUpdate).OriginalValues["RowVersion"] = rowVersion;
                    await db.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    //If no rows are affected by the UPDATE command(no rows have the original RowVersion value),  
                    //the Entity Framework throws a DbUpdateConcurrencyException exception, and the code 
                    //in the catch block gets the affected Department entity from the exception object.
                    var entry = ex.Entries.Single();
                    var clientValues = (Department)entry.Entity;

                    //The GetDatabaseValues method returns null if someone has deleted the row from the database; 
                    //otherwise, you have to cast the returned object to the Department class in order to access the 
                    //Department properties. (Because you already checked for deletion, databaseEntry would be null only 
                    //if the department was deleted after FindAsync executes and before SaveChanges executes.)
                    var databaseEntry = entry.GetDatabaseValues();
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Unable to save changes. The department was deleted by another user.");
                    }
                    else
                    {
                        var databaseValues = (Department)databaseEntry.ToObject();

                        if (databaseValues.Name != clientValues.Name)
                        {
                            ModelState.AddModelError("Name", "Current value: " + databaseValues.Name);
                        }
                        if (databaseValues.Budget != clientValues.Budget)
                        {
                            ModelState.AddModelError("Budget", "Current value: " +
                                String.Format("{0:c}", databaseValues.Budget));
                        }
                        if (databaseValues.StartDate != clientValues.StartDate)
                        {
                            ModelState.AddModelError("StartDate", "Current value: " +
                                String.Format("{0:d}", databaseValues.StartDate));
                        }
                        if (databaseValues.InstructorID != clientValues.InstructorID)
                        {
                            ModelState.AddModelError("InstructorID", "Current value: " +
                                db.Instructors.Find(databaseValues.InstructorID).FullName);
                        }

                        // everbody gets this, any concurrency issue from above
                        ModelState.AddModelError(string.Empty, "The record you attempted to edit "
                            + "was modified by another user after you got the original value. The "
                            + "edit operation was cancelled and the current values in the database "
                            + "have been displayed. If you still want to edit this record, click "
                            + "the Save button again. Otherwise click the Back to List hyperlink.");

                        //Finally, the code sets the RowVersion value of the Department object to the new value 
                        //retrieved from the database. This new RowVersion value will be stored in the hidden field 
                        //when the Edit page is redisplayed, and the next time the user clicks Save, only 
                        //concurrency errors that happen since the redisplay of the Edit page will be caught.
                        departmentToUpdate.RowVersion = databaseValues.RowVersion;

                    }
                }
                catch (RetryLimitExceededException dex)
                {
                    // Log the error (uncomment the dex var and add a line here to write to a log
                    Console.WriteLine("Message: " + dex.Message + " Trace: " + dex.StackTrace);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            // now its the department to update
            ViewBag.InstructorID = new SelectList(db.Instructors, "ID", "FullName", departmentToUpdate.InstructorID);
            return View(departmentToUpdate);
        }

        // GET: Department/Delete/5
        public async Task<ActionResult> Delete(int? id, bool? concurrencyError)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // again, here is the actual execute query statement
            Department department = await db.Departments.FindAsync(id);
            if (department == null)
            {
                if(concurrencyError.GetValueOrDefault())
                {
                    return RedirectToAction("Index");
                }
                return HttpNotFound();
            }

            if (concurrencyError.GetValueOrDefault())
            {
                ViewBag.ConcurrencyErrorMessage = "The record you attempted to delete "
                    + "was modified by another user after you got the original values. "
                    + "The delete operation was cancelled and the current values in the "
                    + "database have been displayed. If you still want to delete this "
                    + "record click the Delete button again. Otherwise "
                    + "click the Back to List hyperlink.";
            }

            return View(department);
        }

        // POST: Department/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(Department department)
        {
            try
            {
                db.Entry(department).State = EntityState.Deleted;

                // here is the actual async call
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch(DbUpdateConcurrencyException)
            {
                return RedirectToAction("Delete", 
                    new { concurrencyError = true, id = department.DepartmentID });
            }
            catch(DataException dex)
            {
                Console.WriteLine($"Message: {dex.Message} Sourse: {dex.Source}");
                // Log the error (uncomment the dex var and add a line here to write a log)
                ModelState.AddModelError(string.Empty, "Unable to delete. Try again, and if the problem persists contact your system administrator.");
                return View(department);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
