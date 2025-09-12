using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;

namespace TiklabChallenge.UseCases.DTOs
{
    public class UpdateProfileRequest
    {
        public string? StudentCode { get; set; }
        public string? FullName { get; set; }
    }
    public class StudentRegisterRequest
    {
        [EmailAddress]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public required string StudentCode { get; set; }
    }
    public class CourseEnrollmentRequest
    {
        public string CourseCode { get; set; } = string.Empty;
    }
    public class EnrollmentDetails
    {
        public required string StudentId { get; set; }
        public required string CourseCode { get; set; }
        public DateTime EnrolledAt { get; set; }
        public EnrollmentStatus Status { get; set; }
        public string? SubjectName { get; set; }
        public string? SubjectCode { get; set; }
    }
}
