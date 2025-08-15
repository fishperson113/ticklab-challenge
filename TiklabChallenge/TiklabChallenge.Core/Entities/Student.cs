using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Student
    {
        public required string UserId { get; set; } = default!;
        public required ApplicationUser User { get; set; }
        public required string StudentCode { get; set; }
        public required string FullName { get; set; }
        public DateTime EnrolledAt { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
    }
}
