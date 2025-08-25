using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.API.Controllers;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;
using Xunit;

namespace TiklabChallenge.Test.Unit.Controllers
{
    public class StudentsControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ILogger<StudentsController>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly StudentEnrollmentService _enrollmentServiceMock;
        private readonly StudentsController _controller;
        private readonly Mock<IStudentRepository> _studentRepoMock;
        private readonly Mock<IEnrollmentRepository> _enrollmentRepoMock;
        private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
        public StudentsControllerTests()
        {
            _loggerMock = new Mock<ILogger<StudentsController>>();
            _mockUow = new Mock<IUnitOfWork>();
            var defaultUser = CreateTestUser();

            // Create mock repositories
            _studentRepoMock = new Mock<IStudentRepository>();
            var courseRepoMock = new Mock<ICourseRepository>();
            var scheduleRepoMock = new Mock<IScheduleRepository>();
            _enrollmentRepoMock = new Mock<IEnrollmentRepository>();
            var subjectRepoMock = new Mock<ISubjectRepository>();
            _waitlistRepoMock = new Mock<IWaitlistRepository>();

            // Setup the IUnitOfWork to return these repositories
            _mockUow.Setup(uow => uow.Students).Returns(_studentRepoMock.Object);
            _mockUow.Setup(uow => uow.Courses).Returns(courseRepoMock.Object);
            _mockUow.Setup(uow => uow.Schedules).Returns(scheduleRepoMock.Object);
            _mockUow.Setup(uow => uow.Enrollments).Returns(_enrollmentRepoMock.Object);
            _mockUow.Setup(uow => uow.Subjects).Returns(subjectRepoMock.Object);
            _mockUow.Setup(uow => uow.Waitlists).Returns(_waitlistRepoMock.Object);
            // Setup student repository
            _studentRepoMock.Setup(repo => repo.GetByIdAsync(
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns((object userId, CancellationToken ct) => {
                    if (userId.ToString() == defaultUser.Id)
                        return Task.FromResult<Student?>(CreateTestStudent(defaultUser));
                    return Task.FromResult<Student?>(null);
                });
            courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .Returns((string courseCode, CancellationToken ct) => {
                    // Return a course for valid course codes
                    return Task.FromResult<Course?>(new Course
                    {
                        CourseCode = courseCode,
                        SubjectCode = courseCode.Split('.')[0],
                        MaxEnrollment = 25
                    });
                });
            subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(
               It.IsAny<string>(),
               It.IsAny<CancellationToken>()))
               .Returns((string subjectCode, CancellationToken ct) => {
                   // Return a subject with prerequisites for CS102 (and make CS101 a prerequisite)
                   var prerequisiteCode = subjectCode == "CS102" ? "CS101" : null;

                   return Task.FromResult<Subject?>(new Subject
                   {
                       SubjectCode = subjectCode,
                       SubjectName = $"Subject {subjectCode}",
                       PrerequisiteSubjectCode = prerequisiteCode
                   });
               });

            scheduleRepoMock.Setup(repo => repo.GetByCourseCodeAsync(
               It.IsAny<string>(),
               It.IsAny<CancellationToken>()))
               .Returns((string courseCode, CancellationToken ct) => {
                   if (courseCode == "CS101.1") 
                   {
                       return Task.FromResult<Schedule?>(new Schedule
                       {
                           CourseCode = "CS101.1",
                           DayOfWeek = DayOfWeekCode.Monday,
                           StartTime = new TimeOnly(10, 0),
                           EndTime = new TimeOnly(12, 0)
                       });
                   }
                   else if (courseCode == "CS102.1") 
                   {
                       return Task.FromResult<Schedule?>(new Schedule
                       {
                           CourseCode = "CS102.1",
                           DayOfWeek = DayOfWeekCode.Wednesday,
                           StartTime = new TimeOnly(13, 0),
                           EndTime = new TimeOnly(15, 0)
                       });
                   }
                   else if (courseCode == "CS201.1") // Course with schedule conflict with CS101.1
                   {
                       return Task.FromResult<Schedule?>(new Schedule
                       {
                           CourseCode = "CS201.1",
                           DayOfWeek = DayOfWeekCode.Monday,  // Same day as CS101.1
                           StartTime = new TimeOnly(11, 0),   // Overlaps with CS101.1
                           EndTime = new TimeOnly(13, 0)
                       });
                   }
                   else if (courseCode == "CS202.1") // Course with no schedule conflicts
                   {
                       return Task.FromResult<Schedule?>(new Schedule
                       {
                           CourseCode = "CS202.1",
                           DayOfWeek = DayOfWeekCode.Tuesday,  // Different day than CS101.1 & CS102.1
                           StartTime = new TimeOnly(10, 0),
                           EndTime = new TimeOnly(12, 0)
                       });
                   }

                   return Task.FromResult<Schedule?>(null); // Return null for other course codes
               });
            // Setup enrollment repository

            _enrollmentRepoMock.Setup(repo => repo.AddAsync(It.IsAny<Enrollment>()))
                .Returns(Task.CompletedTask);

            _enrollmentRepoMock.Setup(repo => repo.CountEnrolledAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string courseCode, CancellationToken ct) => {
                    if (courseCode == "CS301.1")
                        return 25; // Max capacity
                    return 10; // Default count
                });
            _waitlistRepoMock.Setup(repo => repo.GetByCourseAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string courseCode, CancellationToken ct) =>
                {
                    // For CS301.1 (max capacity course), return a waitlisted student
                    if (courseCode == "CS301.1")
                    {
                        var waitlistedUser = CreateTestUser("user2", "waitlisted@example.com");
                        var waitlistedEnrollment = CreateTestEnrollment(waitlistedUser.Id, courseCode);
                        waitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

                        return new List<Waitlist>
                        {
                            new Waitlist
                            {
                                StudentId = waitlistedUser.Id,
                                CourseCode = courseCode,
                                CreatedAt = DateTime.UtcNow,
                                Enrollment = waitlistedEnrollment
                            }
                        };
                    }
                    // For CS102.1 (course with prerequisites), return two waitlisted students
                    else if (courseCode == "CS102.1")
                    {
                        var failedValidationUser = CreateTestUser("user2", "failed@example.com");
                        var validWaitlistedUser = CreateTestUser("user3", "valid@example.com");

                        var failedWaitlistedEnrollment = CreateTestEnrollment(failedValidationUser.Id, courseCode);
                        failedWaitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

                        var validWaitlistedEnrollment = CreateTestEnrollment(validWaitlistedUser.Id, courseCode);
                        validWaitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

                        return new List<Waitlist>
                        {
                            new Waitlist
                            {
                                StudentId = failedValidationUser.Id,
                                CourseCode = courseCode,
                                CreatedAt = DateTime.UtcNow.AddMinutes(-30), // Earlier timestamp (first in queue)
                                Enrollment = failedWaitlistedEnrollment
                            },
                            new Waitlist
                            {
                                StudentId = validWaitlistedUser.Id,
                                CourseCode = courseCode,
                                CreatedAt = DateTime.UtcNow, // Later timestamp (second in queue)
                                Enrollment = validWaitlistedEnrollment
                            }
                        };
                    }

                    // Default: no waitlisted students
                    return new List<Waitlist>();
                });
            // Mock UserManager (requires special setup)
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Create proper mock for SubjectManagementService
            var subjectServiceMock = new SubjectManagementService(_mockUow.Object);
            var _service = new CourseSchedulingService(_mockUow.Object);

            // Create a mock for StudentEnrollmentService with the proper SubjectManagementService mock
            _enrollmentServiceMock = new StudentEnrollmentService(
                _mockUow.Object);

            // Create controller with mocked dependencies
            _controller = new StudentsController(
                _loggerMock.Object,
                _userManagerMock.Object,
                _enrollmentServiceMock);
        }
        #region Helper Methods

        // Helper to create a test student
        private Student CreateTestStudent(
            ApplicationUser user,
            string studentCode = "S0001",
            string fullName = "Test Student")
        {

            return new Student
            {
                UserId = user.Id,
                User = user,
                StudentCode = studentCode,
                FullName = fullName,
                EnrolledAt = DateTime.UtcNow.AddDays(-30)
            };
        }

        // Helper to create an ApplicationUser
        private ApplicationUser CreateTestUser(
            string id = "user1",
            string email = "student1@example.com")
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
        }

        // Helper to create a test enrollment
        private Enrollment CreateTestEnrollment(
            string studentId = "user1",
            string courseCode = "CS101.1")
        {
            var user = CreateTestUser(studentId);
            var student = new Student
            {
                UserId = studentId,
                User = user,
                StudentCode = "S" + studentId,
                FullName = "Test Student " + studentId,
                EnrolledAt = DateTime.UtcNow.AddDays(-30)
            };

            var course = new Course
            {
                CourseCode = courseCode,
                SubjectCode = courseCode.Split('.')[0],
                MaxEnrollment = 25
            };

            return new Enrollment
            {
                StudentId = studentId,
                CourseCode = courseCode,
                EnrolledAt = DateTime.UtcNow,
                Status = EnrollmentStatus.Enrolled,
                Student = student,
                Course = course
            };
        }

        // Helper to set up UserManager to return the current user
        private void SetupUserManagerGetUser(ApplicationUser user)
        {
            _userManagerMock.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
        }
        private void SetupStudentRepoGetUserById(Student student)
        {
            _studentRepoMock.Reset();

            _studentRepoMock.Setup(repo => repo.GetByIdAsync(
                student.UserId,
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Student?>(student));

            _studentRepoMock.Setup(repo => repo.GetByIdAsync(
                It.Is<object>(id => id.ToString() != student.UserId),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<Student?>(null));
        }
        #endregion

        #region GetMyProfile Tests

        [Fact]
        public async Task GetMyProfile_ValidUser_ReturnsOkWithProfile()
        {
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);

            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);

            // Act
            var result = await _controller.GetMyProfile();

            // Assert
            Assert.IsType<OkObjectResult>(result);

        }

        [Fact]
        public async Task GetMyProfile_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserManagerGetUser(null); // No user found

            // Act
            var result = await _controller.GetMyProfile();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Cannot determine current user", unauthorizedResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task GetMyProfile_NoStudentProfile_ReturnsNotFound()
        {
            // Arrange
            var user1 = CreateTestUser();
            var user2 = CreateTestUser(id:"user2");
            var student = CreateTestStudent(user2);

            SetupUserManagerGetUser(user1);
            SetupStudentRepoGetUserById(student);

            // Act
            var result = await _controller.GetMyProfile();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region UpdateMyProfile Tests

        [Fact]
        public async Task UpdateMyProfile_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);
            var updateRequest = new UpdateProfileRequest
            {
                StudentCode = "S0099",
                FullName = "Updated Name"
            };

            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);
            // Act
            var result = await _controller.UpdateMyProfile(updateRequest, CancellationToken.None);

            // Assert
            Assert.IsType<OkResult>(result);

            // Verify student properties were updated correctly
            Assert.Equal(updateRequest.StudentCode, student.StudentCode);
            Assert.Equal(updateRequest.FullName, student.FullName);
        }

        [Fact]
        public async Task UpdateMyProfile_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            var updateRequest = new UpdateProfileRequest
            {
                StudentCode = "S0099",
                FullName = "Updated Name"
            };

            SetupUserManagerGetUser(null); // No user found

            // Act
            var result = await _controller.UpdateMyProfile(updateRequest, CancellationToken.None);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Cannot determine current user", unauthorizedResult.Value?.ToString() ?? string.Empty);

         
        }

        #endregion

        #region GetMyEnrollments Tests

        [Fact]
        public async Task GetMyEnrollments_HasEnrollments_ReturnsOkWithEnrollments()
        {
            _enrollmentRepoMock.Setup(repo => repo.GetByStudentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string studentId, CancellationToken ct) =>
                {
                    return new List<Enrollment?>
                    {
                        CreateTestEnrollment(studentId, "CS101.1"),
                        CreateTestEnrollment(studentId, "CS102.1")
                    };
                });
            // Arrange
            var user = CreateTestUser();
            var enrollmentDetails = new List<object>
            {
                new
                {
                    StudentId = user.Id,
                    CourseCode = "CS101.1",
                    EnrolledAt = DateTime.UtcNow,
                    Status = EnrollmentStatus.Enrolled,
                    SubjectName = "Introduction to Programming",
                    SubjectCode = "CS101"
                },
                new
                {
                    StudentId = user.Id,
                    CourseCode = "CS102.1",
                    EnrolledAt = DateTime.UtcNow,
                    Status = EnrollmentStatus.Enrolled,
                    SubjectName = "Data Structures",
                    SubjectCode = "CS102"
                }
            };

            SetupUserManagerGetUser(user);

            // Act
            var result = await _controller.GetMyEnrollments();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEnrollments = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Equal(2, returnedEnrollments.Count());
        }

        [Fact]
        public async Task GetMyEnrollments_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            SetupUserManagerGetUser(null); // No user found

            // Act
            var result = await _controller.GetMyEnrollments();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Cannot determine current user", unauthorizedResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region EnrollInCourse Tests

        [Fact]
        public async Task EnrollInCourse_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var user = CreateTestUser();
            var request = new CourseEnrollmentRequest { CourseCode = "CS101.1" };
            var enrollment = CreateTestEnrollment(user.Id, request.CourseCode);

            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(CreateTestStudent(user)); 

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetMyEnrollments), createdAtActionResult.ActionName);
        }

        [Fact]
        public async Task EnrollInCourse_NoUser_ReturnsUnauthorized()
        {
            // Arrange
            var request = new CourseEnrollmentRequest { CourseCode = "CS101.1" };

            SetupUserManagerGetUser(null); // No user found

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Cannot determine current user", unauthorizedResult.Value?.ToString() ?? string.Empty);
        }

        [Fact]
        public async Task EnrollInCourse_CourseAtMaxCapacity_AddsToWaitlist()
        {
            // Arrange
            var user = CreateTestUser();
            var request = new CourseEnrollmentRequest { CourseCode = "CS301.1" }; // At max capacity course

            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(CreateTestStudent(user));

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);

            // Verify that the enrollment was created with Waitlisted status
            var returnedValue = createdAtActionResult.Value;
            var statusProperty = returnedValue.GetType().GetProperty("Status");
            var status = (EnrollmentStatus)statusProperty.GetValue(returnedValue);
            Assert.Equal(EnrollmentStatus.Waitlisted, status);
        }

        #endregion
        [Fact]
        public async Task EnrollInCourse_WithCompletedPrerequisites_ReturnsCreatedAtAction()
        {
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);
            var request = new CourseEnrollmentRequest { CourseCode = "CS102.1" }; // CS102 requires CS101 as prerequisite

            // Setup mocks
            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);

            // Setup prerequisite course as passed
            var course = new Course
            {
                CourseCode = "CS101.1",
                SubjectCode = "CS101"
            };
            _enrollmentRepoMock.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<Enrollment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment> {
                    new Enrollment {
                        StudentId = user.Id,
                        CourseCode = "CS101.1",
                        Status = EnrollmentStatus.Enrolled,
                        IsPassed = true,
                        Student = student,
                        Course = course
                    }
                });

            _mockUow.Setup(uow => uow.Enrollments).Returns(_enrollmentRepoMock.Object);

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetMyEnrollments), createdAtActionResult.ActionName);
        }

        [Fact]
        public async Task EnrollInCourse_WithoutCompletedPrerequisites_ReturnsBadRequest()
        {
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);
            var request = new CourseEnrollmentRequest { CourseCode = "CS102.1" }; // CS102 requires CS101 as prerequisite

            // Setup mocks
            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);

            // Setup empty prerequisites (student hasn't passed CS101)
            _enrollmentRepoMock.Setup(repo => repo.FindAsync(
                It.IsAny<Expression<Func<Enrollment, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment>()); // Return empty list - no passed prerequisites

            _mockUow.Setup(uow => uow.Enrollments).Returns(_enrollmentRepoMock.Object);

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Student must complete CS101 before taking CS102", badRequestResult.Value?.ToString() ?? string.Empty);
        }
        [Fact]
        public async Task EnrollInCourse_WithNoScheduleConflicts_ReturnsCreatedAtAction()
        {
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);
            var request = new CourseEnrollmentRequest { CourseCode = "CS202.1" }; // Course with no schedule conflicts

            // Setup mocks
            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetMyEnrollments), createdAtActionResult.ActionName);
        }

        [Fact]
        public async Task EnrollInCourse_WithScheduleConflict_ReturnsBadRequest()
        {
            _enrollmentRepoMock.Setup(repo => repo.GetByStudentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string studentId, CancellationToken ct) =>
                {
                    return new List<Enrollment?>
                    {
                            CreateTestEnrollment(studentId, "CS101.1"),
                            CreateTestEnrollment(studentId, "CS102.1")
                    };
                });
            // Arrange
            var user = CreateTestUser();
            var student = CreateTestStudent(user);
            var request = new CourseEnrollmentRequest { CourseCode = "CS201.1" }; // Course with schedule conflict

            // Setup mocks
            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(student);

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Schedule conflict", badRequestResult.Value?.ToString() ?? string.Empty);
        }
        [Fact]
        public async Task WithdrawFromCourse_CourseNotAtCapacity_ReturnsNoContent()
        {
            // Arrange
            var user = CreateTestUser();
            var courseCode = "CS101.1"; // Course not at max capacity
            var enrollment = CreateTestEnrollment(user.Id, courseCode);
            enrollment.Status = EnrollmentStatus.Enrolled;

            SetupUserManagerGetUser(user);

            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                user.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            // Act
            var result = await _controller.WithdrawFromCourse(courseCode);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task WithdrawFromCourse_CourseAtCapacityWithWaitlist_MovesStudentFromWaitlist()
        {
            // Arrange
            var user = CreateTestUser();
            var courseCode = "CS301.1"; // Course at max capacity

            var enrollment = CreateTestEnrollment(user.Id, courseCode);
            enrollment.Status = EnrollmentStatus.Enrolled;

            var waitlistedUser = CreateTestUser("user2", "waitlisted@example.com");
            var waitlistedEnrollment = CreateTestEnrollment(waitlistedUser.Id, courseCode);
            waitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

            SetupUserManagerGetUser(user);

            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                user.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                waitlistedUser.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(waitlistedEnrollment);

            // Act
            var result = await _controller.WithdrawFromCourse(courseCode);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Equal(EnrollmentStatus.Enrolled, waitlistedEnrollment.Status);
        }

        [Fact]
        public async Task WithdrawFromCourse_FirstWaitlistedStudentFailsValidation_ProcessesNextStudent()
        {
            // Arrange
            var user = CreateTestUser(); 
            var courseCode = "CS102.1"; // Course requiring CS101 prerequisite validation

            // Setup the enrollment for the withdrawing user
            var enrollment = CreateTestEnrollment(user.Id, courseCode);
            enrollment.Status = EnrollmentStatus.Enrolled;

            var failedValidationUser = CreateTestUser("user2", "failed@example.com");
            var validWaitlistedUser = CreateTestUser("user3", "valid@example.com");

            var failedStudent = CreateTestStudent(failedValidationUser, "S" + failedValidationUser.Id, "Failed Student");
            var validStudent = CreateTestStudent(validWaitlistedUser, "S" + validWaitlistedUser.Id, "Valid Student");

            var failedWaitlistedEnrollment = CreateTestEnrollment(failedValidationUser.Id, courseCode);
            failedWaitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

            var validWaitlistedEnrollment = CreateTestEnrollment(validWaitlistedUser.Id, courseCode);
            validWaitlistedEnrollment.Status = EnrollmentStatus.Waitlisted;

            var prerequisiteCourse = new Course
            {
                CourseCode = "CS101.1",
                SubjectCode = "CS101"
            };
            var prereqData = new List<Enrollment> {
                new Enrollment {
                    StudentId = validWaitlistedUser.Id,
                    CourseCode = "CS101.1",
                    IsPassed = true,
                    Course = new Course { CourseCode = "CS101.1", SubjectCode = "CS101" },
                    Student = validStudent
                }
            };
            SetupUserManagerGetUser(user);

            // Setup enrollment repository for the withdrawing user
            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                user.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(enrollment);

            // Setup enrollment repositories for the waitlisted users
            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                failedValidationUser.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(failedWaitlistedEnrollment);

            _enrollmentRepoMock.Setup(repo => repo.FirstOrDefaultAsync(
                validWaitlistedUser.Id, courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validWaitlistedEnrollment);

            _enrollmentRepoMock.Setup(r => r.FindAsync(
                          It.IsAny<Expression<Func<Enrollment, bool>>>(),
                          It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Enrollment, bool>> pred, CancellationToken _) =>
            prereqData.AsQueryable().Where(pred).ToList());

            // Act
            var result = await _controller.WithdrawFromCourse(courseCode);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // First waitlisted student should remain waitlisted
            Assert.Equal(EnrollmentStatus.Waitlisted, failedWaitlistedEnrollment.Status);

            // Second waitlisted student should be promoted to enrolled
            Assert.Equal(EnrollmentStatus.Enrolled, validWaitlistedEnrollment.Status);
        }
    }
}