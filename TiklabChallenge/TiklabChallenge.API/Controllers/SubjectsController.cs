using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;
using TiklabChallenge.UseCases.Services;

namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<SubjectsController> _logger;
        private readonly SubjectValidationService _validationService;

        public SubjectsController(
            IUnitOfWork uow,
            ILogger<SubjectsController> logger,
            SubjectValidationService validationService)
        {
            _uow = uow;
            _logger = logger;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubjects(CancellationToken ct = default)
        {
            var subjects = await _uow.Subjects.GetAllAsync(ct);
            return Ok(subjects);
        }

        [HttpGet("{subjectCode}")]
        public async Task<IActionResult> GetSubject(string subjectCode, CancellationToken ct = default)
        {
            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);

            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            return Ok(subject);
        }

        [HttpGet("{subjectCode}/prerequisites")]
        public async Task<IActionResult> GetPrerequisiteChain(string subjectCode, CancellationToken ct = default)
        {
            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            var chain = await _validationService.GetPrerequisiteChainAsync(subjectCode, ct);
            return Ok(chain);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Subjects.ExistsAsync(s => s.SubjectCode == request.SubjectCode, ct);
                if (exists)
                    return Conflict($"Subject with code '{request.SubjectCode}' already exists.");

                // Validate prerequisite if provided
                if (!string.IsNullOrWhiteSpace(request.PrerequisiteSubjectCode))
                {
                    var (isValid, errorMessage) = await _validationService.ValidatePrerequisiteAsync(
                        request.SubjectCode, request.PrerequisiteSubjectCode, ct);

                    if (!isValid)
                        return BadRequest(errorMessage);
                }

                var subject = new Subject
                {
                    SubjectCode = request.SubjectCode,
                    SubjectName = request.SubjectName,
                    Description = request.Description,
                    DefaultCredits = request.DefaultCredits,
                    PrerequisiteSubjectCode = !string.IsNullOrWhiteSpace(request.PrerequisiteSubjectCode)
                        ? request.PrerequisiteSubjectCode.Trim()
                        : null
                };

                await _uow.Subjects.AddAsync(subject);
                await _uow.CommitAsync();

                _logger.LogInformation("Created new subject with code {SubjectCode}", subject.SubjectCode);

                return CreatedAtAction(nameof(GetSubject), new { subjectCode = subject.SubjectCode }, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subject");
                return StatusCode(500, "An error occurred while creating the subject.");
            }
        }

        [HttpPut("{subjectCode}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> UpdateSubject(
            string subjectCode,
            [FromBody] UpdateSubjectRequest request,
            CancellationToken ct = default)
        {
            try
            {
                var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
                if (subject == null)
                    return NotFound($"Subject with code '{subjectCode}' not found.");

                // Validate prerequisite if it's being changed
                if (request.PrerequisiteSubjectCode != null &&
                    request.PrerequisiteSubjectCode != subject.PrerequisiteSubjectCode)
                {
                    var (isValid, errorMessage) = await _validationService.ValidatePrerequisiteAsync(
                        subjectCode, request.PrerequisiteSubjectCode, ct);

                    if (!isValid)
                        return BadRequest(errorMessage);
                }

                // Use UpdateScalarsAsync instead of direct property updates
                await _uow.Subjects.UpdateScalarsAsync(
                    subjectCode,
                    request.SubjectName,
                    request.Description,
                    request.DefaultCredits,
                    request.PrerequisiteSubjectCode,
                    ct);

                await _uow.CommitAsync();

                // Get the updated subject
                subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);

                _logger.LogInformation("Updated subject with code {SubjectCode}", subject?.SubjectCode);

                return Ok(subject);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject");
                return StatusCode(500, "An error occurred while updating the subject.");
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateSubjectsForEnrollment(
            [FromQuery] string studentId,
            [FromQuery] string[] subjectCodes,
            CancellationToken ct = default)
        {
            try
            {
                var results = await _validationService.ValidateSubjectsForStudentAsync(
                    studentId, subjectCodes, ct);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating subjects for enrollment");
                return StatusCode(500, "An error occurred while validating subjects.");
            }
        }
    }
}