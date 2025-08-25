using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Student
    {
        public required string UserId { get; set; }
        public required ApplicationUser User { get; set; }

        public Guid StudentCode { get; set; }
        public DateTime EnrolledAt { get; set; }
    }
}
