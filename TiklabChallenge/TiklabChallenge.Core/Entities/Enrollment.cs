using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public enum EnrollmentStatus { Enrolled, Waitlisted }
    public class Enrollment
    {
        public required string StudentId { get; set; }
        public required string CourseCode { get; set; }
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; }
        public required Student Student { get; set; }
        public required Course Course { get; set; }
    }
}
