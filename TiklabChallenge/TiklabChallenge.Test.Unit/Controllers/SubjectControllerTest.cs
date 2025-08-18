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
    public class SubjectControllerTest
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ISubjectRepository> _subjectRepoMock;
        private readonly Mock<ILogger<SubjectsController>> _loggerMock;
        private readonly SubjectValidationService _validationServiceMock;
        private readonly SubjectsController _controller;

        public SubjectControllerTest()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _subjectRepoMock = new Mock<ISubjectRepository>();
            _loggerMock = new Mock<ILogger<SubjectsController>>();
            _validationServiceMock = new SubjectValidationService(_mockUow.Object);

            _mockUow.Setup(uow => uow.Subjects).Returns(_subjectRepoMock.Object);

            // Create controller with mocked dependencies
            _controller = new SubjectsController(_mockUow.Object, _loggerMock.Object, _validationServiceMock);
        }

        #region Helper Methods

        // Helper to create test subjects
        private List<Subject> CreateTestSubjects(int count = 2, string codePrefix = "MATH")
        {
            var subjects = new List<Subject>();
            for (int i = 1; i <= count; i++)
            {
                subjects.Add(new Subject
                {
                    SubjectCode = $"{codePrefix}{100 + i}",
                    SubjectName = $"Test Subject {i}",
                    Description = $"Description for test subject {i}",
                    DefaultCredits = 3,
                    PrerequisiteSubjectCode = i > 1 ? $"{codePrefix}{100 + i - 1}" : null
                });
            }
            return subjects;
        }

        // Helper to create a single test subject
        private Subject CreateTestSubject(
            string subjectCode = "MATH101",
            string subjectName = "Test Subject",
            string description = "Test Description",
            int defaultCredits = 3,
            string? prerequisiteSubjectCode = null)
        {
            return new Subject
            {
                SubjectCode = subjectCode,
                SubjectName = subjectName,
                Description = description,
                DefaultCredits = defaultCredits,
                PrerequisiteSubjectCode = prerequisiteSubjectCode
            };
        }

        // Helper to create a CreateSubjectRequest
        private CreateSubjectRequest CreateSubjectRequest(
            string subjectCode = "MATH101",
            string subjectName = "Test Subject",
            string description = "Test Description",
            int defaultCredits = 3,
            string? prerequisiteSubjectCode = null)
        {
            return new CreateSubjectRequest
            {
                SubjectCode = subjectCode,
                SubjectName = subjectName,
                Description = description,
                DefaultCredits = defaultCredits,
                PrerequisiteSubjectCode = prerequisiteSubjectCode
            };
        }

        // Helper to create a UpdateSubjectRequest
        private UpdateSubjectRequest CreateUpdateSubjectRequest(
            string? subjectName = "Updated Subject",
            string? description = "Updated Description",
            int? defaultCredits = 4,
            string? prerequisiteSubjectCode = null)
        {
            return new UpdateSubjectRequest
            {
                SubjectName = subjectName,
                Description = description,
                DefaultCredits = defaultCredits,
                PrerequisiteSubjectCode = prerequisiteSubjectCode
            };
        }

        #endregion

        #region GetAllSubjects Tests

        [Fact]
        public async Task GetAllSubjects_ValidRequest_ReturnsOkWithSubjects()
        {
            // Arrange
            var subjects = CreateTestSubjects();
            _subjectRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(subjects);

            // Act
            var result = await _controller.GetAllSubjects();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSubjects = Assert.IsAssignableFrom<IEnumerable<Subject?>>(okResult.Value);
            Assert.Equal(subjects.Count, returnedSubjects.Count());
        }

        [Fact]
        public async Task GetAllSubjects_EmptyList_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            _subjectRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Subject?>());

            // Act
            var result = await _controller.GetAllSubjects();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSubjects = Assert.IsAssignableFrom<IEnumerable<Subject?>>(okResult.Value);
            Assert.Empty(returnedSubjects);
        }

        #endregion

        #region GetSubject Tests

        [Fact]
        public async Task GetSubject_ValidCode_ReturnsOkWithSubject()
        {
            // Arrange
            var subjectCode = "MATH101";
            var subject = CreateTestSubject(subjectCode);
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subject);

            // Act
            var result = await _controller.GetSubject(subjectCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSubject = Assert.IsType<Subject>(okResult.Value);
            Assert.Equal(subjectCode, returnedSubject.SubjectCode);
        }

        [Fact]
        public async Task GetSubject_InvalidCode_ReturnsNotFound()
        {
            // Arrange
            var subjectCode = "INVALID";
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Subject?)null);

            // Act
            var result = await _controller.GetSubject(subjectCode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region GetPrerequisiteChain Tests

        [Fact]
        public async Task GetPrerequisiteChain_ValidSubject_ReturnsOkWithChain()
        {
            // Arrange
            var math101 = CreateTestSubject("MATH101", "Math 101", "Basic Mathematics");
            var math102 = CreateTestSubject("MATH102", "Math 102", "Advanced Mathematics", 3, "MATH101");
            var expectedChain = new List<Subject> { math101 };


            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync("MATH102", It.IsAny<CancellationToken>()))
                .ReturnsAsync(math102);
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync("MATH101", It.IsAny<CancellationToken>()))
                .ReturnsAsync(math101);

            // Act
            var result = await _controller.GetPrerequisiteChain("MATH102");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedChain = Assert.IsAssignableFrom<List<Subject>>(okResult.Value);

            Assert.Single(returnedChain);
            Assert.Equal("MATH101", returnedChain[0].SubjectCode);
        }

        [Fact]
        public async Task GetPrerequisiteChain_InvalidSubject_ReturnsNotFound()
        {
            // Arrange
            var subjectCode = "INVALID";
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Subject?)null);

            // Act
            var result = await _controller.GetPrerequisiteChain(subjectCode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region CreateSubject Tests

        [Fact]
        public async Task CreateSubject_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = CreateSubjectRequest();
            var subject = CreateTestSubject(
                request.SubjectCode,
                request.SubjectName,
                request.Description,
                request.DefaultCredits,
                request.PrerequisiteSubjectCode);

            // Setup GetBySubjectCodeAsync to return the created subject
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(request.SubjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subject);

            // Act
            var result = await _controller.CreateSubject(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetSubject), createdAtActionResult.ActionName);
            Assert.Equal(request.SubjectCode, createdAtActionResult.RouteValues?["subjectCode"]);

            var returnedSubject = Assert.IsType<Subject>(createdAtActionResult.Value);
            Assert.Equal(request.SubjectCode, returnedSubject.SubjectCode);
            Assert.Equal(request.SubjectName, returnedSubject.SubjectName);
            Assert.Equal(request.Description, returnedSubject.Description);
            Assert.Equal(request.DefaultCredits, returnedSubject.DefaultCredits);

            _subjectRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Subject>()), Times.Once);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateSubject_SubjectAlreadyExists_ReturnsConflict()
        {
            // Arrange
            var request = CreateSubjectRequest();

            // Setup repo to return true for exists (subject already exists)
            _subjectRepoMock.Setup(repo => repo.ExistsAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Subject, bool>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CreateSubject(request);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("already exists", conflictResult.Value?.ToString() ?? string.Empty);

            _subjectRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Subject>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateSubject_InvalidPrerequisite_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateSubjectRequest(prerequisiteSubjectCode: "INVALID");

            // Act
            var result = await _controller.CreateSubject(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("not found", badRequestResult.Value?.ToString() ?? string.Empty);

            _subjectRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Subject>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        #endregion

        #region UpdateSubject Tests

        [Fact]
        public async Task UpdateSubject_ValidRequest_ReturnsOkWithUpdatedSubject()
        {
            // Arrange
            var subjectCode = "MATH101";
            var subject = CreateTestSubject(subjectCode);
            var request = CreateUpdateSubjectRequest();
            var updatedSubject = CreateTestSubject(
                subjectCode,
                request.SubjectName ?? subject.SubjectName,
                request.Description ?? subject.Description,
                request.DefaultCredits ?? subject.DefaultCredits,
                request.PrerequisiteSubjectCode);

            // Setup GetBySubjectCodeAsync to return the subject
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subject);

            // Setup to return updated subject after commit
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedSubject);

            // Act
            var result = await _controller.UpdateSubject(subjectCode, request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSubject = Assert.IsType<Subject>(okResult.Value);
            Assert.Equal(subjectCode, returnedSubject.SubjectCode);
            Assert.Equal(request.SubjectName, returnedSubject.SubjectName);
            Assert.Equal(request.Description, returnedSubject.Description);
            Assert.Equal(request.DefaultCredits, returnedSubject.DefaultCredits);

            _subjectRepoMock.Verify(repo => repo.UpdateScalarsAsync(
                subjectCode,
                request.SubjectName,
                request.Description,
                request.DefaultCredits,
                request.PrerequisiteSubjectCode,
                It.IsAny<CancellationToken>()), Times.Once);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSubject_SubjectNotFound_ReturnsNotFound()
        {
            // Arrange
            var subjectCode = "NONEXISTENT";
            var request = CreateUpdateSubjectRequest();

            // Setup GetBySubjectCodeAsync to return null
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Subject?)null);

            // Act
            var result = await _controller.UpdateSubject(subjectCode, request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);

            _subjectRepoMock.Verify(repo => repo.UpdateScalarsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateSubject_InvalidPrerequisite_ReturnsBadRequest()
        {
            // Arrange
            var subjectCode = "MATH101";
            var subject = CreateTestSubject(subjectCode);
            var request = CreateUpdateSubjectRequest(prerequisiteSubjectCode: "INVALID");

            // Setup GetBySubjectCodeAsync to return the subject
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync(subjectCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(subject);

            // Act
            var result = await _controller.UpdateSubject(subjectCode, request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("not found", badRequestResult.Value?.ToString() ?? string.Empty);

            _subjectRepoMock.Verify(repo => repo.UpdateScalarsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        #endregion

        #region ValidateSubjectsForEnrollment Tests

        [Fact]
        public async Task ValidateSubjectsForEnrollment_ValidRequest_ReturnsOkWithResults()
        {
            // Arrange
            var studentId = "student1";
            var subjectCodes = new[] { "MATH101", "CS101" };
            var validationResults = new Dictionary<string, string?>
            {
                { "MATH101", null }, // null means no error
                { "CS101", "Student must complete MATH101 before taking CS101" }
            };
            var math101 = CreateTestSubject("MATH101");
            var cs101 = CreateTestSubject("CS101", prerequisiteSubjectCode: "MATH101");

            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync("MATH101", It.IsAny<CancellationToken>()))
                .ReturnsAsync(math101);
            _subjectRepoMock.Setup(repo => repo.GetBySubjectCodeAsync("CS101", It.IsAny<CancellationToken>()))
                .ReturnsAsync(cs101);

            var mockEnrollmentRepo = new Mock<IEnrollmentRepository>();
            _mockUow.Setup(uow => uow.Enrollments).Returns(mockEnrollmentRepo.Object);

            mockEnrollmentRepo.Setup(repo => repo.GetByStudentAsync(studentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Enrollment?>());

            // Act
            var result = await _controller.ValidateSubjectsForEnrollment(studentId, subjectCodes);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedResults = Assert.IsType<Dictionary<string, string?>>(okResult.Value);
            Assert.Equal(2, returnedResults.Count);
            Assert.Null(returnedResults["MATH101"]);
            Assert.Contains("must complete", returnedResults["CS101"]);
        }

        #endregion
    }
}