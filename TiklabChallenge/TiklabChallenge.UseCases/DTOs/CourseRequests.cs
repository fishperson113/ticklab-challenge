using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.UseCases.DTOs
{
    public class CourseCreateRequest
    {
        [Required]
        public required string CourseCode { get; set; }
        [Required]
        public required string SubjectCode { get; set; }
        public int? MaxEnrollment { get; set; }
        public ScheduleCreateRequest? Schedule { get; set; }
    }

    public class CourseUpdateRequest
    {
        [Required]
        public required string CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? SubjectCode { get; set; }
        public int? MaxEnrollment { get; set; }
        public ScheduleUpdateRequest? Schedule { get; set; }
    }
}