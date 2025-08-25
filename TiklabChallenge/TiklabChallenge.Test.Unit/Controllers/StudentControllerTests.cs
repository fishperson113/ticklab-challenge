using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public StudentsControllerTests()
        {
            _loggerMock = new Mock<ILogger<StudentsController>>();
            _mockUow = new Mock<IUnitOfWork>();
            var defaultUser = CreateTestUser();

            // Create mock repositories
            _studentRepoMock = new Mock<IStudentRepository>();
            var courseRepoMock = new Mock<ICourseRepository>();
            var scheduleRepoMock = new Mock<IScheduleRepository>();
            var enrollmentRepoMock = new Mock<IEnrollmentRepository>();
            var subjectRepoMock = new Mock<ISubjectRepository>();

            // Setup the IUnitOfWork to return these repositories
            _mockUow.Setup(uow => uow.Students).Returns(_studentRepoMock.Object);
            _mockUow.Setup(uow => uow.Courses).Returns(courseRepoMock.Object);
            _mockUow.Setup(uow => uow.Schedules).Returns(scheduleRepoMock.Object);
            _mockUow.Setup(uow => uow.Enrollments).Returns(enrollmentRepoMock.Object);
            _mockUow.Setup(uow => uow.Subjects).Returns(subjectRepoMock.Object);

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
            // Setup enrollment repository
            enrollmentRepoMock.Setup(repo => repo.GetByStudentAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string studentId, CancellationToken ct) => {
                    return new List<Enrollment?>
                    {
                        CreateTestEnrollment(studentId, "CS101.1"),
                        CreateTestEnrollment(studentId, "CS102.1")
                    };
                });

            enrollmentRepoMock.Setup(repo => repo.AddAsync(It.IsAny<Enrollment>()))
                .Returns(Task.CompletedTask);

            enrollmentRepoMock.Setup(repo => repo.CountEnrolledAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync((string courseCode, CancellationToken ct) => {
                    if (courseCode == "CS301.1")
                        return 25; // Max capacity
                    return 10; // Default count
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
                _mockUow.Object,
                subjectServiceMock,
                _service);

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
        public async Task EnrollInCourse_CourseCapacityReached_ReturnsBadRequest()
        {
            // Arrange
            var user = CreateTestUser();
            var request = new CourseEnrollmentRequest { CourseCode = "CS301.1" };

            SetupUserManagerGetUser(user);
            SetupStudentRepoGetUserById(CreateTestStudent(user)); // Ensure student exists

            // Act
            var result = await _controller.EnrollInCourse(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("maximum enrollment capacity", badRequestResult.Value?.ToString() ?? string.Empty);
        }

        #endregion
    }
}