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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? RoomId { get; set; } 
        public string? CourseCode { get; set; }
        public Course? Course { get; set; }
        public DayOfWeekCode DayOfWeek { get; set; }         
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
