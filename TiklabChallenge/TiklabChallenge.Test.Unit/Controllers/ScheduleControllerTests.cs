using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    public class ScheduleControllerTests
    {
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<IScheduleRepository> _scheduleRepoMock;
        private readonly Mock<ILogger<SchedulesController>> _loggerMock;
        private readonly SchedulesController _controller;

        public ScheduleControllerTests()
        {
            _mockUow = new Mock<IUnitOfWork>();
            _scheduleRepoMock = new Mock<IScheduleRepository>();
            _loggerMock = new Mock<ILogger<SchedulesController>>();

            _mockUow.Setup(uow => uow.Schedules).Returns(_scheduleRepoMock.Object);
            var service= new CourseSchedulingService(_mockUow.Object);
            // Create controller with mocked dependencies
            _controller = new SchedulesController(service, _loggerMock.Object);
        }

        #region Helper Methods

        // Helper to create test schedules
        private List<Schedule?> CreateTestSchedules(int count = 2, string roomIdPrefix = "Room", string courseCodePrefix = "CS")
        {
            var schedules = new List<Schedule?>();
            for (int i = 1; i <= count; i++)
            {
                schedules.Add(new Schedule
                {
                    Id = $"schedule-{i}",
                    RoomId = $"{roomIdPrefix}{i}",
                    CourseCode = $"{courseCodePrefix}{100 + i}",
                    DayOfWeek = (DayOfWeekCode)(i % 5), // Cycle through days of week
                    StartTime = new TimeOnly(8 + i, 0),
                    EndTime = new TimeOnly(10 + i, 0)
                });
            }
            return schedules;
        }

        // Helper to create a single test schedule
        private Schedule CreateTestSchedule(
            string id = "schedule-1",
            string roomId = "Room1",
            string courseCode = "CS101",
            DayOfWeekCode dayOfWeek = DayOfWeekCode.Monday,
            int startHour = 9,
            int endHour = 11)
        {
            return new Schedule
            {
                Id = id,
                RoomId = roomId,
                CourseCode = courseCode,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(startHour, 0),
                EndTime = new TimeOnly(endHour, 0),
                Course = new Course { CourseCode = courseCode, SubjectCode = "CS" }
            };
        }

        #endregion

        #region GetAllSchedules Tests

        [Fact]
        public async Task GetAllSchedules_ValidRequest_ReturnsOkWithSchedules()
        {
            // Arrange
            var schedules = CreateTestSchedules();
            _scheduleRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _controller.GetAllSchedules();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Equal(schedules.Count, returnedSchedules.Count());
        }

        [Fact]
        public async Task GetAllSchedules_EmptyList_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            _scheduleRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Schedule?>());

            // Act
            var result = await _controller.GetAllSchedules();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Empty(returnedSchedules);
        }

        #endregion

        #region GetSchedule Tests

        [Fact]
        public async Task GetSchedule_ValidId_ReturnsOkWithSchedule()
        {
            // Arrange
            var scheduleId = "schedule-1";
            var schedule = CreateTestSchedule(id: scheduleId);
            _scheduleRepoMock.Setup(repo => repo.GetByIdWithDetailsAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedule);

            // Act
            var result = await _controller.GetSchedule(scheduleId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedule = Assert.IsType<Schedule>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
        }

        [Fact]
        public async Task GetSchedule_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var scheduleId = "invalid-id";
            _scheduleRepoMock.Setup(repo => repo.GetByIdWithDetailsAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Schedule?)null);

            // Act
            var result = await _controller.GetSchedule(scheduleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region GetSchedulesByRoom Tests

        [Fact]
        public async Task GetSchedulesByRoom_ValidRoomId_ReturnsOkWithSchedules()
        {
            // Arrange
            var roomId = "Room1";
            var schedules = CreateTestSchedules(2, roomId);
            _scheduleRepoMock.Setup(repo => repo.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _controller.GetSchedulesByRoom(roomId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Equal(schedules.Count, returnedSchedules.Count());
        }

        [Fact]
        public async Task GetSchedulesByRoom_EmptyResult_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            var roomId = "EmptyRoom";
            _scheduleRepoMock.Setup(repo => repo.GetByRoomIdAsync(roomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Schedule?>());

            // Act
            var result = await _controller.GetSchedulesByRoom(roomId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Empty(returnedSchedules);
        }

        #endregion

        #region GetSchedulesByCourse Tests

        [Fact]
        public async Task GetSchedulesByCourse_ValidCourseCode_ReturnsOkWithSchedules()
        {
            // Arrange
            var courseCode = "CS101";
            var schedules = CreateTestSchedules(2, "Room", courseCode);
            _scheduleRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _controller.GetSchedulesByCourse(courseCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Equal(schedules.Count, returnedSchedules.Count());
        }

        [Fact]
        public async Task GetSchedulesByCourse_EmptyResult_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            var courseCode = "NonexistentCourse";
            _scheduleRepoMock.Setup(repo => repo.GetByCourseCodeAsync(courseCode, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Schedule?>());

            // Act
            var result = await _controller.GetSchedulesByCourse(courseCode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Empty(returnedSchedules);
        }

        #endregion

        #region GetSchedulesByDay Tests

        [Fact]
        public async Task GetSchedulesByDay_ValidDay_ReturnsOkWithSchedules()
        {
            // Arrange
            var dayOfWeek = DayOfWeekCode.Monday;
            var schedules = CreateTestSchedules(2);
            _scheduleRepoMock.Setup(repo => repo.GetByDayOfWeekAsync(dayOfWeek, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedules);

            // Act
            var result = await _controller.GetSchedulesByDay(dayOfWeek);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Equal(schedules.Count, returnedSchedules.Count());
        }

        [Fact]
        public async Task GetSchedulesByDay_EmptyResult_ReturnsOkWithEmptyCollection()
        {
            // Arrange
            var dayOfWeek = DayOfWeekCode.Sunday;
            _scheduleRepoMock.Setup(repo => repo.GetByDayOfWeekAsync(dayOfWeek, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Schedule?>());

            // Act
            var result = await _controller.GetSchedulesByDay(dayOfWeek);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedules = Assert.IsAssignableFrom<IEnumerable<Schedule?>>(okResult.Value);
            Assert.Empty(returnedSchedules);
        }

        #endregion

        #region DeleteSchedule Tests

        [Fact]
        public async Task DeleteSchedule_ValidId_ReturnsNoContent()
        {
            // Arrange
            var scheduleId = "schedule-1";
            var schedule = CreateTestSchedule(id: scheduleId);
            _scheduleRepoMock.Setup(repo => repo.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedule);

            // Act
            var result = await _controller.DeleteSchedule(scheduleId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _scheduleRepoMock.Verify(repo => repo.DeleteAsync(schedule), Times.Once);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSchedule_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var scheduleId = "invalid-id";
            _scheduleRepoMock.Setup(repo => repo.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Schedule?)null);

            // Act
            var result = await _controller.DeleteSchedule(scheduleId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);
            _scheduleRepoMock.Verify(repo => repo.DeleteAsync(It.IsAny<Schedule>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteSchedule_ExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var scheduleId = "schedule-1";
            var schedule = CreateTestSchedule(id: scheduleId);
            _scheduleRepoMock.Setup(repo => repo.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(schedule);
            _scheduleRepoMock.Setup(repo => repo.DeleteAsync(schedule))
                .Callback(() => throw new Exception("Database error"));

            // Act
            var result = await _controller.DeleteSchedule(scheduleId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Contains("An error occurred", statusCodeResult.Value?.ToString() ?? string.Empty);
        }

        #endregion

        #region CreateSchedule Tests

        // Helper method to create ScheduleCreateRequest
        private ScheduleCreateRequest CreateScheduleCreateRequest(
            string roomId = "Room1",
            string courseCode = "CS101",
            DayOfWeekCode dayOfWeek = DayOfWeekCode.Monday,
            int startHour = 9,
            int endHour = 11)
        {
            return new ScheduleCreateRequest
            {
                RoomId = roomId,
                CourseCode = courseCode,
                DayOfWeek = dayOfWeek,
                StartTime = new TimeOnly(startHour, 0),
                EndTime = new TimeOnly(endHour, 0)
            };
        }

        [Fact]
        public async Task CreateSchedule_ValidRequest_ReturnsCreatedAtAction()
        {
            // Arrange
            var request = CreateScheduleCreateRequest();
            var createdSchedule = CreateTestSchedule(
                roomId: request.RoomId,
                courseCode: request.CourseCode,
                dayOfWeek: request.DayOfWeek,
                startHour: request.StartTime.Hour,
                endHour: request.EndTime.Hour);

            // Setup no conflicts
            _scheduleRepoMock.Setup(repo => repo.GetByRoomIdAsync(request.RoomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Schedule?>());

            // Setup successful add
            _scheduleRepoMock.Setup(repo => repo.AddAsync(It.IsAny<Schedule>()))
                .Callback<Schedule>(s => s.Id = createdSchedule.Id);

            // Act
            var result = await _controller.CreateSchedule(request);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetSchedule), createdAtActionResult.ActionName);
            Assert.Equal(createdSchedule.Id, createdAtActionResult.RouteValues?["id"]);

            var returnedSchedule = Assert.IsType<Schedule>(createdAtActionResult.Value);
            Assert.Equal(request.RoomId, returnedSchedule.RoomId);
            Assert.Equal(request.CourseCode, returnedSchedule.CourseCode);
            Assert.Equal(request.DayOfWeek, returnedSchedule.DayOfWeek);

            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateSchedule_ConflictingSchedule_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateScheduleCreateRequest(
                roomId: "Room1",
                dayOfWeek: DayOfWeekCode.Monday,
                startHour: 9,
                endHour: 11);

            // Setup a conflicting schedule
            var existingSchedules = new List<Schedule?>
            {
                CreateTestSchedule(
                    id: "existing-schedule",
                    roomId: "Room1",
                    dayOfWeek: DayOfWeekCode.Monday,
                    startHour: 10,  // Overlaps with request's 9-11
                    endHour: 12)
            };

            _scheduleRepoMock.Setup(repo => repo.GetByRoomIdAsync(request.RoomId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingSchedules);

            // Act
            var result = await _controller.CreateSchedule(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("conflicts", badRequestResult.Value?.ToString() ?? string.Empty);

            _scheduleRepoMock.Verify(repo => repo.AddAsync(It.IsAny<Schedule>()), Times.Never);
            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        #endregion

        #region UpdateSchedule Tests

        // Helper method to create ScheduleUpdateRequest
        private ScheduleUpdateRequest CreateScheduleUpdateRequest(
            string id = "schedule-1",
            string? roomId = "Room2",
            string? courseCode = null,
            DayOfWeekCode? dayOfWeek = DayOfWeekCode.Tuesday,
            int? startHour = 13,
            int? endHour = 15)
        {
            return new ScheduleUpdateRequest
            {
                Id = id,
                RoomId = roomId,
                CourseCode = courseCode,
                DayOfWeek = dayOfWeek,
                StartTime = startHour.HasValue ? new TimeOnly(startHour.Value, 0) : null,
                EndTime = endHour.HasValue ? new TimeOnly(endHour.Value, 0) : null
            };
        }

        [Fact]
        public async Task UpdateSchedule_ValidRequest_ReturnsOkWithUpdatedSchedule()
        {
            // Arrange
            var scheduleId = "schedule-1";
            var existingSchedule = CreateTestSchedule(id: scheduleId);
            var request = CreateScheduleUpdateRequest(id: scheduleId);
            var updatedSchedule = CreateTestSchedule(
                id: scheduleId,
                roomId: request.RoomId ?? existingSchedule.RoomId,
                courseCode: request.CourseCode ?? existingSchedule.CourseCode,
                dayOfWeek: request.DayOfWeek ?? existingSchedule.DayOfWeek,
                startHour: request.StartTime?.Hour ?? existingSchedule.StartTime.Hour,
                endHour: request.EndTime?.Hour ?? existingSchedule.EndTime.Hour);

            // Setup mock behavior
            _scheduleRepoMock.Setup(repo => repo.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingSchedule);
            _scheduleRepoMock.Setup(repo => repo.GetByIdWithDetailsAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedSchedule);

            // Act
            var result = await _controller.UpdateSchedule(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSchedule = Assert.IsType<Schedule>(okResult.Value);
            Assert.Equal(scheduleId, returnedSchedule.Id);
            Assert.Equal(request.RoomId, returnedSchedule.RoomId);

            _mockUow.Verify(uow => uow.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSchedule_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateScheduleUpdateRequest(id: "different-id");

            // Act
            var result = await _controller.UpdateSchedule(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task UpdateSchedule_ScheduleNotFound_ReturnsNotFound()
        {
            // Arrange
            var scheduleId = "nonexistent";
            var request = CreateScheduleUpdateRequest(id: scheduleId);

            _scheduleRepoMock.Setup(repo => repo.GetByIdAsync(scheduleId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Schedule?)null);

            // Act
            var result = await _controller.UpdateSchedule(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("not found", notFoundResult.Value?.ToString() ?? string.Empty);

            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateSchedule_InvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var scheduleId = "schedule-1";
            var request = CreateScheduleUpdateRequest(
                id: scheduleId,
                startHour: 15,  // Start time after end time
                endHour: 13);

            // Act
            var result = await _controller.UpdateSchedule(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid time range", badRequestResult.Value?.ToString() ?? string.Empty);

            _mockUow.Verify(uow => uow.CommitAsync(), Times.Never);
        }

        #endregion
    }
}