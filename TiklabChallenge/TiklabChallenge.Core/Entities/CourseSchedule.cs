using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class CourseSchedule
    {
        public required string CourseCode { get; set; } 
        public required string ScheduleId { get; set; }
        public required Course Course { get; set; } 
        public required Schedule Schedule { get; set; }
    }
}
