using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;
using Xunit;

namespace TiklabChallenge.Test.Unit
{
    public class CourseSchedulingServiceTests
    {
        #region Setup

        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ICourseRepository> _mockCourseRepo;
        private readonly Mock<IScheduleRepository> _mockScheduleRepo;
        private readonly CourseSchedulingService _service;

        public CourseSchedulingServiceTests()
        {
            _mockCourseRepo = new Mock<ICourseRepository>();
            _mockScheduleRepo = new Mock<IScheduleRepository>();
            _mockUow = new Mock<IUnitOfWork>();

            _mockUow.Setup(uow => uow.Courses).Returns(_mockCourseRepo.Object);
            _mockUow.Setup(uow => uow.Schedules).Returns(_mockScheduleRepo.Object);

            _service = new CourseSchedulingService(_mockUow.Object);
        }

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

        [return: NotNull]
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

        [return: NotNull]
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

        #endregion

        #region CreateCourseAsync Tests

        [Fact]
        public async Task CreateCourseAsync_WithValidRequest_CreatesAndReturnsCourse()
        {
            // Arrange
            var courseRequest = CreateCourseCreateRequest();
            Course? addedCourse = null;

            _mockCourseRepo.Setup(r => r.AddAsync(It.IsAny<Course>()))
                .Callback<Course>((c) => addedCourse = c);

            // Act
            var result = await _service.CreateCourseAsync(courseRequest);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(courseRequest.CourseCode, result.CourseCode);
            Assert.Equal(courseRequest.SubjectCode, result.SubjectCode);
            Assert.Equal(courseRequest.MaxEnrollment, result.MaxEnrollment);

            _mockCourseRepo.Verify(r => r.AddAsync(It.IsAny<Course>()), Times.Once);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        #endregion

        #region UpdateCourseAsync Tests

        [Fact]
        public async Task UpdateCourseAsync_WithValidRequest_UpdatesAndReturnsCourse()
        {
            // Arrange
            var courseCode = "CS101";
            var course = CreateTestCourse(courseCode);
            var updateRequest = CreateCourseUpdateRequest(
                courseCode: courseCode,
                subjectCode: "MATH",
                maxEnrollment: 50);

            _mockCourseRepo.Setup(r => r.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            // Act - Using the CourseUpdateRequest version of UpdateCourseAsync
            var result = await _service.UpdateCourseAsync(updateRequest);

            // Assert
            Assert.NotNull(result);
            _mockCourseRepo.Verify(r => r.UpdateScalarsAsync(
                courseCode,
                updateRequest.MaxEnrollment,
                updateRequest.SubjectCode,
                It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateCourseAsync_WithNonExistentCourse_ThrowsKeyNotFoundException()
        {
            // Arrange
            var courseCode = "NONEXISTENT";
            var updateRequest = CreateCourseUpdateRequest(courseCode: courseCode);

            _mockCourseRepo.Setup(r => r.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            // Act & Assert - Using the CourseUpdateRequest version of UpdateCourseAsync
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateCourseAsync(updateRequest));

            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        #endregion

        #region GetCoursesAsync Tests

        [Fact]
        public async Task GetAllCoursesAsync_ReturnsAllCourses()
        {
            // Arrange
            var courses = new List<Course?>
            {
                CreateTestCourse("CS101", "CS", 30),
                CreateTestCourse("CS102", "CS", 25)
            };

            _mockCourseRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(courses);

            // Act
            var result = await _service.GetAllCoursesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetCourseByCodeAsync_WithExistingCode_ReturnsCourse()
        {
            // Arrange
            var courseCode = "CS101";
            var course = CreateTestCourse(courseCode);

            _mockCourseRepo.Setup(r => r.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(course);

            // Act
            var result = await _service.GetCourseByCodeAsync(courseCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(courseCode, result.CourseCode);
        }

        [Fact]
        public async Task GetCourseByCodeAsync_WithNonExistingCode_ReturnsNull()
        {
            // Arrange
            var courseCode = "NONEXISTENT";

            _mockCourseRepo.Setup(r => r.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Course?)null);

            // Act
            var result = await _service.GetCourseByCodeAsync(courseCode);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCoursesBySubjectAsync_WithExistingSubject_ReturnsCourses()
        {
            // Arrange
            var subjectCode = "CS";
            var courses = new List<Course?>
            {
                CreateTestCourse("CS101", subjectCode, 30),
                CreateTestCourse("CS102", subjectCode, 25)
            };

            _mockCourseRepo.Setup(r => r.GetBySubjectAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(courses);

            // Act
            var result = await _service.GetCoursesBySubjectAsync(subjectCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion
    }
}