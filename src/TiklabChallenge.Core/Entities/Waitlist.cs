using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Waitlist
    {
        public required string StudentId { get; set; }
        public required string CourseCode { get; set; }
        public DateTime CreatedAt { get; set; }

        public required Enrollment Enrollment { get; set; }
    }
}
