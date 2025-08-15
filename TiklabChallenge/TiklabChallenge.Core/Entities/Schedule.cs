using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public enum DayOfWeekCode { Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

    public class Schedule
    {
        public required string ScheduleId { get; set; }   // ví dụ: "A101", "B202"
        public DayOfWeekCode DayOfWeek { get; set; }         
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public required ICollection<CourseSchedule> CourseSchedules { get; set; } 
    }
}
