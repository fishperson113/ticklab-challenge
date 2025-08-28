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

        public StudentEnrollmentService(IUnitOfWork uow)
        {
            _uow = uow;
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

            // Validate prerequisites
            await ValidatePrerequisitesAsync(studentId, course.SubjectCode, ct);

            // Check for schedule conflicts
            await ValidateScheduleConflictsAsync(studentId, request.CourseCode, ct);

            var isCourseAtMaxCapacity = await IsCourseAtMaxCapacityAsync(request.CourseCode, ct);
            // Create the enrollment
            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseCode = request.CourseCode,
                EnrolledAt = DateTime.UtcNow,
                Status = isCourseAtMaxCapacity ? EnrollmentStatus.Waitlisted : EnrollmentStatus.Enrolled,
                Student = student,
                Course = course
            };

            await _uow.Enrollments.AddAsync(enrollment);

            if (isCourseAtMaxCapacity)
            {
                var waitlist = new Waitlist
                {
                    StudentId = studentId,
                    CourseCode = request.CourseCode,
                    CreatedAt = DateTime.UtcNow,
                    Enrollment = enrollment
                };

                await _uow.Waitlists.AddAsync(waitlist);
            }

            await _uow.CommitAsync();

            return enrollment;
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

            var hasCompletedPrereq = await CheckPrerequisiteCompletionAsync(studentId, subject.PrerequisiteSubjectCode, ct);

            if (!hasCompletedPrereq)
                throw new Exception($"Student must complete {subject.PrerequisiteSubjectCode} before taking {subjectCode}");
        }
        private async Task ValidateScheduleConflictsAsync(
            string studentId,
            string courseCode,
            CancellationToken ct = default)
        {
            var newSchedule = await _uow.Schedules.GetByCourseCodeAsync(courseCode, ct);
            if (newSchedule == null)
                return; // No schedule to check conflicts with

            var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);
            var enrolledCourseCodes = enrollments
                .Where(e => e != null && e.Status == EnrollmentStatus.Enrolled)
                .Select(e => e?.CourseCode)
                .ToList();

            foreach (var enrolledCourseCode in enrolledCourseCodes)
            {
                var existingSchedule = await _uow.Schedules.GetByCourseCodeAsync(enrolledCourseCode, ct);

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

        private bool HasTimeConflict(Schedule schedule1, Schedule schedule2)
        {
            // Different days, no conflict
            if (schedule1.DayOfWeek != schedule2.DayOfWeek)
                return false;

            // Check if time ranges overlap
            return (schedule1.StartTime <= schedule2.StartTime && schedule1.EndTime > schedule2.StartTime) ||
                   (schedule1.StartTime >= schedule2.StartTime && schedule1.StartTime < schedule2.EndTime);
        }
        private async Task<bool> CheckPrerequisiteCompletionAsync(
            string studentId, string prerequisiteCode, CancellationToken ct = default)
        {
            var enrollments = await _uow.Enrollments.FindAsync(
                e => e.StudentId == studentId &&
                        e.Course.SubjectCode == prerequisiteCode &&
                       (e.IsPassed || e.Status == EnrollmentStatus.Enrolled),
                ct);

            return enrollments.Any();
        }
        private async Task<bool> IsCourseAtMaxCapacityAsync(
           string courseCode,
           CancellationToken ct = default)
        {
            var course = await _uow.Courses.GetByCourseCodeAsync(courseCode, ct);
            if (course == null)
                throw new Exception($"Course with code '{courseCode}' not found.");

            if (!course.MaxEnrollment.HasValue)
                return false; // No enrollment limit

            // Get current enrollment count
            var currentEnrollmentCount = await _uow.Enrollments.CountEnrolledAsync(courseCode, ct);
            return currentEnrollmentCount >= course.MaxEnrollment.Value;
        }
        public async Task WithdrawFromCourseAsync(string studentId, string courseCode, CancellationToken ct = default)
        {
            var enrollment = await _uow.Enrollments.FirstOrDefaultAsync(studentId, courseCode, ct);
            if (enrollment == null)
                throw new Exception($"No enrollment found for course '{courseCode}'.");

            // If the student is enrolled (not waitlisted), we need to process the waitlist
            var wasEnrolled = enrollment.Status == EnrollmentStatus.Enrolled;

            // Delete the enrollment
            await _uow.Enrollments.DeleteAsync(enrollment);

            // If the student was actively enrolled, try to enroll the next student from waitlist
            if (wasEnrolled)
            {
                await ProcessNextInWaitlistAsync(courseCode, ct);
            }

            await _uow.CommitAsync();
        }
        private async Task ProcessNextInWaitlistAsync(string courseCode, CancellationToken ct = default)
        {
            var waitlistedStudents = await _uow.Waitlists.GetByCourseAsync(courseCode, ct);

            foreach (var waitlistEntry in waitlistedStudents)
            {
                try
                {
                    // Get their enrollment
                    var waitlistedEnrollment = await _uow.Enrollments.FirstOrDefaultAsync(
                        waitlistEntry.StudentId, courseCode, ct);

                    if (waitlistedEnrollment == null || waitlistedEnrollment.Status != EnrollmentStatus.Waitlisted)
                        continue; 

                    // Verify prerequisites
                    var course = await _uow.Courses.GetByCourseCodeAsync(courseCode, ct);
                    if (course != null)
                    {
                        await ValidatePrerequisitesAsync(waitlistEntry.StudentId, course.SubjectCode, ct);
                    }

                    // Check for schedule conflicts
                    await ValidateScheduleConflictsAsync(waitlistEntry.StudentId, courseCode, ct);

                    // If validation passes (no exceptions thrown), promote student to enrolled
                    waitlistedEnrollment.Status = EnrollmentStatus.Enrolled;
                    await _uow.Waitlists.DeleteAsync(waitlistEntry);

                    return;
                }
                catch (Exception)
                {
                    // If validation fails for this student, continue to the next one in the queue
                    // The failed student stays in the waitlist at their original position
                    continue;
                }
            }
        }
    }
}