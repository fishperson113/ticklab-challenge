using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.API.Controllers;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;
using Xunit;

namespace TiklabChallenge.Test.Unit.Controllers
{
    public class CourseControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICourseRepository> _courseRepoMock;
        private readonly Mock<ILogger<CoursesController>> _loggerMock;
        private readonly CoursesController _controller;
        private readonly CourseSchedulingService _service;

        public CourseControllerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _courseRepoMock = new Mock<ICourseRepository>();
            _loggerMock = new Mock<ILogger<CoursesController>>();

            _mockUow.Setup(uow => uow.Courses).Returns(_courseRepoMock.Object);

            // Create a real service with mocked dependencies
            _service = new CourseSchedulingService(_mockUow.Object);

            // Create controller with the real service
            _controller = new CoursesController(_service, _loggerMock.Object);
        }

        // Helper to create test courses
        private List<Course?> CreateTestCourses(int count = 2, string subjectCodePrefix = "CS")
        {
            var courses = new List<Course?>();
            for (int i = 1; i <= count; i++)
            {
                courses.Add(new Course
                {
                    CourseCode = $"{subjectCodePrefix}{100 + i}",
                    SubjectCode = subjectCodePrefix,
                    MaxEnrollment = 30 - i,
                    CreatedAt = DateTime.UtcNow
                });
            }
            return courses;
        }

        // Helper to create a single test course
        private Course CreateTestCourse(string courseCode = "CS101", string subjectCode = "CS", int? maxEnrollment = 30)
        {
            return new Course
            {
                CourseCode = courseCode,
                SubjectCode = subjectCode,
                MaxEnrollment = maxEnrollment,
                CreatedAt = DateTime.UtcNow
            };
        }

        // Helper to create a CourseCreateRequest
        private CourseCreateRequest CreateCourseCreateRequest(
            string courseCode = "CS101",
            string subjectCode = "CS",
            int? maxEnrollment = 30,
            ScheduleCreateRequest? schedule = null)
        {
            return new CourseCreateRequest
            {
                CourseCode = courseCode,
                SubjectCode = subjectCode,
                MaxEnrollment = maxEnrollment,
                Schedule = schedule
            };
        }

        // Helper to create a CourseUpdateRequest
        private CourseUpdateRequest CreateCourseUpdateRequest(
            string courseCode = "CS101",
            string? subjectCode = "CS",
            int? maxEnrollment = 30,
            ScheduleUpdateRequest? schedule = null)
        {
            return new CourseUpdateRequest
            {
                CourseCode = courseCode,
                SubjectCode = subjectCode,
                MaxEnrollment = maxEnrollment,
                Schedule = schedule
            };
        }
        #region GetAllCourses Tests

        [Fact]
        public async Task GetAllCourses_ValidRequest_ReturnsOkWithCourses()
        {
            // Arrange
            var courses = CreateTestCourses();
            _courseRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(courses);

            // Act
            var result = await _controller.GetAllCourses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourses = Assert.IsAssignableFrom<IEnumerable<Course?>>(okResult.Value);
            Assert.Equal(courses.Count, returnedCourses.Count());
        }

        [Fact]
        public async Task GetAllCourses_EmptyList_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            _courseRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Course?>());

            // Act
            var result = await _controller.GetAllCourses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourses = Assert.IsAssignableFrom<IEnumerable<Course?>>(okResult.Value);
            Assert.Empty(returnedCourses);
        }

        #endregion

        #region GetCourse Tests

        [Fact]
        public async Task GetCourse_ValidCourseCode_ReturnsOkWithCourse()
        {
            // Arrange
            var courseCode = "CS101";
            var course = CreateTestCourse(courseCode);
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            // Act
            var result = await _controller.GetCourse(courseCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourse = Assert.IsType<Course>(okResult.Value);
            Assert.Equal(courseCode, returnedCourse.CourseCode);
        }

        [Fact]
        public async Task GetCourse_InvalidCourseCode_ReturnsNotFound()
        {
            // Arrange
            var courseCode = "INVALID";
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            // Act
            var result = await _controller.GetCourse(courseCode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region GetCoursesBySubject Tests

        [Fact]
        public async Task GetCoursesBySubject_ValidSubject_ReturnsOkWithCourses()
        {
            // Arrange
            var subjectCode = "CS";
            var courses = CreateTestCourses(2, subjectCode);
            _courseRepoMock.Setup(repo => repo.GetBySubjectAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(courses);

            // Act
            var result = await _controller.GetCoursesBySubject(subjectCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourses = Assert.IsAssignableFrom<IEnumerable<Course?>>(okResult.Value);
            Assert.Equal(courses.Count, returnedCourses.Count());
        }

        [Fact]
        public async Task GetCoursesBySubject_InvalidSubject_ReturnsEmptyCollection()
        {
            // Arrange
            var subjectCode = "INVALID";
            _courseRepoMock.Setup(repo => repo.GetBySubjectAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Course?>());

            // Act
            var result = await _controller.GetCoursesBySubject(subjectCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourses = Assert.IsAssignableFrom<IEnumerable<Course?>>(okResult.Value);
            Assert.Empty(returnedCourses);
        }

        #endregion

        #region CreateCourse Tests

        [Fact]
        public async Task CreateCourse_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = CreateCourseCreateRequest();
            var createdCourse = CreateTestCourse(request.CourseCode, request.SubjectCode, request.MaxEnrollment);

            // Setup mock behavior
            _courseRepoMock.Setup(repo => repo.AddAsync(It.IsAny<Course>()));
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(request.CourseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdCourse);

            // Act
            var result = await _controller.CreateCourse(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetCourse), createdAtActionResult.ActionName);
            Assert.Equal(request.CourseCode, createdAtActionResult.RouteValues?["courseCode"]);

            var returnedCourse = Assert.IsType<Course>(createdAtActionResult.Value);
            Assert.Equal(request.CourseCode, returnedCourse.CourseCode);
        }

        [Fact]
        public async Task CreateCourse_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateCourseCreateRequest();

            // Setup mock to throw exception
            _courseRepoMock.Setup(repo => repo.AddAsync(It.IsAny<Course>()))
                .Callback<Course>(_ => { throw new ArgumentException("Invalid course data"); });

            // Act
            var result = await _controller.CreateCourse(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid course data", badRequestResult.Value);
        }

        #endregion

        #region UpdateCourse Tests

        [Fact]
        public async Task UpdateCourse_ValidRequest_ReturnsOkWithUpdatedCourse()
        {
            // Arrange
            var courseCode = "CS101";
            var request = CreateCourseUpdateRequest(courseCode, "MATH", 50);
            var existingCourse = CreateTestCourse(courseCode);
            var updatedCourse = CreateTestCourse(courseCode, "MATH", 50);

            // Setup mock behavior
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCourse);
            _courseRepoMock.Setup(repo => repo.UpdateScalarsAsync(
                courseCode,
                request.MaxEnrollment,
                request.SubjectCode,
                It.IsAny<CancellationToken>()));

            // Setup final fetch to return updated course
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedCourse);

            // Act
            var result = await _controller.UpdateCourse(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCourse = Assert.IsType<Course>(okResult.Value);
            Assert.Equal(request.CourseCode, returnedCourse.CourseCode);
            Assert.Equal(request.SubjectCode, returnedCourse.SubjectCode);
            Assert.Equal(request.MaxEnrollment, returnedCourse.MaxEnrollment);
        }

        [Fact]
        public async Task UpdateCourse_InvalidRequest_ReturnsNotFound()
        {
            // Arrange
            var request = CreateCourseUpdateRequest(courseCode: "NONEXISTENT");

            // Setup mock to throw exception
            _courseRepoMock.Setup(repo => repo.GetByCourseCodeAsync(request.CourseCode, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException($"Course with code '{request.CourseCode}' not found."));

            // Act
            var result = await _controller.UpdateCourse(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        #endregion
    }
}