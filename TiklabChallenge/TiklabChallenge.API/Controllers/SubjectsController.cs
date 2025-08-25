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
        private readonly ILogger<SubjectsController> _logger;
        private readonly SubjectManagementService _subjectService;

        public SubjectsController(
            ILogger<SubjectsController> logger,
            SubjectManagementService subjectService)
        {
            _logger = logger;
            _subjectService = subjectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubjects(CancellationToken ct = default)
        {
            var subjects = await _subjectService.GetAllSubjectsAsync(ct);
            return Ok(subjects);
        }

        [HttpGet("{subjectCode}")]
        public async Task<IActionResult> GetSubject(string subjectCode, CancellationToken ct = default)
        {
            var subject = await _subjectService.GetSubjectByCodeAsync(subjectCode, ct);

            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            return Ok(subject);
        }

        [HttpGet("{subjectCode}/prerequisites")]
        public async Task<IActionResult> GetPrerequisiteChain(string subjectCode, CancellationToken ct = default)
        {
            var subject = await _subjectService.GetSubjectByCodeAsync(subjectCode, ct);
            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            var chain = await _subjectService.GetPrerequisiteChainAsync(subjectCode, ct);
            return Ok(chain);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken ct = default)
        {
            try
            {
                var subject = await _subjectService.CreateSubjectAsync(request, ct);

                _logger.LogInformation("Created new subject with code {SubjectCode}", subject.SubjectCode);

                return CreatedAtAction(nameof(GetSubject), new { subjectCode = subject.SubjectCode }, subject);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
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
                var subject = await _subjectService.UpdateSubjectAsync(subjectCode, request, ct);

                _logger.LogInformation("Updated subject with code {SubjectCode}", subject?.SubjectCode);

                return Ok(subject);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subject");
                return StatusCode(500, "An error occurred while updating the subject.");
            }
        }
    }
}