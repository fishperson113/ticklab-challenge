using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.UseCases.DTOs
{
    public class ScheduleCreateRequest
    {
        public string? RoomId { get; set; }
        public string? CourseCode { get; set; }
        [Required]
        public DayOfWeekCode DayOfWeek { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(RoomId) &&
                   StartTime < EndTime;
        }
    }

    public class ScheduleUpdateRequest
    {
        [Required]
        public required string Id { get; set; }
        public string? RoomId { get; set; }

        public string? CourseCode { get; set; }

        public DayOfWeekCode? DayOfWeek { get; set; }

        public TimeOnly? StartTime { get; set; }

        public TimeOnly? EndTime { get; set; }
        public bool HasValidTimeRange()
        {
            return !StartTime.HasValue || !EndTime.HasValue || StartTime < EndTime;
        }
    }
}