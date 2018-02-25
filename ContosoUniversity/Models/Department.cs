using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ContosoUniversity.Models
{
    public class Department
    {
        public int DepartmentID { get; set; }

        [StringLength(50, MinimumLength = 3)]
        public string Name { get; set; }
        [DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public decimal Budget { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }
        public int? InstructorID { get; set; }

        // v.10 - implementing optimistic concurrency in EF6
        // the timestamp attirbute specifies that this col will be included in the Where
        // clause of Update and Delete commands sent to the db. The attribute is called Timesstamp
        // because previous versions of SQL Server used a SQL timestamp data type before
        // SQL rowversion replaced it
        // The .NET type for rowversion is a byte array
        [Timestamp]
        public byte[] RowVersion { get; set; }
        
        // navigational properties
        public virtual Instructor Administrator { get; set; }
        public virtual ICollection<Course> Courses { get; set; }


    }
}