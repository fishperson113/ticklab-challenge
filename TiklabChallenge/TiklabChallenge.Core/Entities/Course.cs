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
        public string? CourseName { get; set; }
        public string? Description { get; set; }
        public int Credits { get; set; }
        public int? MaxEnrollment { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Prerequisite>? Prerequisites { get; set; }        
        public ICollection<Prerequisite>? RequiredBy { get; set; }          
        public ICollection<CourseSchedule>? CourseSchedules { get; set; }
    }
}
