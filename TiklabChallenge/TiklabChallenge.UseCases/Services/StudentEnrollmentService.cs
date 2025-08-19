using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;

namespace TiklabChallenge.UseCases.Services
{
    public class StudentEnrollmentService
    {
        private readonly IUnitOfWork _uow;
        private readonly SubjectManagementService _subjectService;
        private readonly CourseSchedulingService _courseService;

        public StudentEnrollmentService(
            IUnitOfWork uow,
            SubjectManagementService subjectService,
            CourseSchedulingService courseService)
        {
            _uow = uow;
            _subjectService = subjectService;
            _courseService = courseService;
        }

        public async Task<Student?> GetStudentProfileAsync(string userId, CancellationToken ct = default)
        {
            return await _uow.Students.GetByIdAsync(userId, ct);
        }

        public async Task UpdateStudentProfileAsync(Student student, CancellationToken ct = default)
        {
            await _uow.Students.UpdateProfileAsync(student, ct);
            await _uow.CommitAsync();
        }

        public async Task<IEnumerable<object>> GetEnrollmentDetailsAsync(string studentId, CancellationToken ct = default)
        {
            var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);
            var enrollmentDetails = new List<object>();

            foreach (var enrollment in enrollments)
            {
                if (enrollment != null)
                {
                    var course = await _uow.Courses.GetByCourseCodeAsync(enrollment.CourseCode, ct);
                    var subject = course != null
                        ? await _uow.Subjects.GetBySubjectCodeAsync(course.SubjectCode, ct)
                        : null;

                    enrollmentDetails.Add(new
                    {
                        enrollment.StudentId,
                        enrollment.CourseCode,
                        enrollment.EnrolledAt,
                        enrollment.Status,
                        SubjectName = subject?.SubjectName,
                        SubjectCode = course?.SubjectCode
                    });
                }
            }

            return enrollmentDetails;
        }

        public async Task<Enrollment> EnrollStudentInCourseAsync(
            string studentId,
            CourseEnrollmentRequest request,
            CancellationToken ct = default)
        {
            // Verify student exists
            var student = await _uow.Students.GetByIdAsync(studentId, ct);
            if (student == null)
                throw new Exception("Student profile not found.");

            // Verify the course exists
            var course = await _uow.Courses.GetByCourseCodeAsync(request.CourseCode, ct);
            if (course == null)
                throw new Exception($"Course with code '{request.CourseCode}' not found.");

            // Check if the student is already enrolled
            var existingEnrollment = await _uow.Enrollments.FirstOrDefaultAsync(
                studentId, request.CourseCode, ct);

            if (existingEnrollment != null)
                throw new Exception($"You are already enrolled in course '{request.CourseCode}'.");

            // Check enrollment capacity
            await ValidateEnrollmentCapacityAsync(request.CourseCode, ct);

            // Validate prerequisites
            await ValidatePrerequisitesAsync(studentId, course.SubjectCode, ct);

            // Check for schedule conflicts
            await ValidateScheduleConflictsAsync(studentId, request.CourseCode, ct);

            // Create the enrollment
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseCode = request.CourseCode,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Enrolled,
                Student = student,
                Course = course
            };

            await _uow.Enrollments.AddAsync(enrollment);
            await _uow.CommitAsync();

            return enrollment;
        }

        private async Task ValidateEnrollmentCapacityAsync(
            string courseCode,
            CancellationToken ct = default)
        {
            var course = await _uow.Courses.GetByCourseCodeAsync(courseCode, ct);
            if (course == null)
                throw new Exception($"Course with code '{courseCode}' not found.");

            // Check if the course has reached maximum enrollment
            var currentEnrollmentCount = await _uow.Enrollments.CountEnrolledAsync(courseCode, ct);
            if (course.MaxEnrollment.HasValue && currentEnrollmentCount >= course.MaxEnrollment.Value)
                throw new Exception($"Course '{courseCode}' has reached maximum enrollment capacity.");
        }

        private async Task ValidatePrerequisitesAsync(
            string studentId,
            string subjectCode,
            CancellationToken ct = default)
        {
            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
            if (subject == null)
                throw new Exception($"Subject with code '{subjectCode}' not found.");

            if (string.IsNullOrEmpty(subject.PrerequisiteSubjectCode))
                return; // No prerequisites needed

            var (isValid, errorMessage) = await _subjectService.CanTakeSubjectAsync(studentId, subjectCode, ct);
            if (!isValid)
                throw new Exception(errorMessage ?? $"Prerequisite requirements not met for subject '{subjectCode}'.");
        }

        private async Task ValidateScheduleConflictsAsync(
            string studentId,
            string courseCode,
            CancellationToken ct = default)
        {
            var schedules = await _uow.Schedules.GetByCourseCodeAsync(courseCode, ct);
            if (!schedules.Any())
                return; // No schedules, no conflicts

            var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);
            var enrolledCourseCodes = enrollments
                .Where(e => e != null && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e?.CourseCode)
                .ToList();

            foreach (var enrolledCourseCode in enrolledCourseCodes)
            {
                if (enrolledCourseCode == null)
                    continue;

                var enrolledSchedules = await _uow.Schedules.GetByCourseCodeAsync(enrolledCourseCode, ct);

                foreach (var newSchedule in schedules)
                {
                    if (newSchedule == null)
                        continue;

                    foreach (var existingSchedule in enrolledSchedules)
                    {
                        if (existingSchedule == null)
                            continue;

                        // Check for schedule conflict
                        if (HasTimeConflict(existingSchedule, newSchedule))
                        {
                            throw new Exception(
                                $"Schedule conflict with course '{enrolledCourseCode}' on {existingSchedule.DayOfWeek} " +
                                $"from {existingSchedule.StartTime.ToString("HH:mm")} to {existingSchedule.EndTime.ToString("HH:mm")}.");
                        }
                    }
                }
            }
        }

        private bool HasTimeConflict(Schedule schedule1, Schedule schedule2)
        {
            // Different days, no conflict
            if (schedule1.DayOfWeek != schedule2.DayOfWeek)
                return false;

            // Check if time ranges overlap
            return (schedule1.StartTime <= schedule2.StartTime && schedule1.EndTime > schedule2.StartTime) ||
                   (schedule1.StartTime >= schedule2.StartTime && schedule1.StartTime < schedule2.EndTime);
        }
    }
}