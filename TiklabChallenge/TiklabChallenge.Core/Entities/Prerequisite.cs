using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Prerequisite
    {
        public required string CourseCode { get; set; } 
        public required string PrerequisiteCode { get; set; } 
        public required Course Course { get; set; } 
        public required Course RequiredCourse { get; set; }
    }
}
