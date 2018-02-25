using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity.Models
{
    public class Student : Person 
    {
        // HACK: we will experiment with leaving it here, i.e., only students
        // will have an email, then we will move it to person so that all
        // people will have an email (this would add email to instructors too)
        public string EmailAddress { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Enrollment Date")]
        public DateTime EnrollmentDate { get; set; }

        // v.10 - navigational prop will stay here as unique to students
        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
