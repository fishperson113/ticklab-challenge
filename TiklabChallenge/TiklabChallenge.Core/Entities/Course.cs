using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Course
    {
        public required string CourseCode { get; set; }   
        public required string SubjectCode { get; set; }  
        public Subject Subject { get; set; } = default!;      
        public int? MaxEnrollment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public Schedule? Schedule { get; set; }
    }
}
