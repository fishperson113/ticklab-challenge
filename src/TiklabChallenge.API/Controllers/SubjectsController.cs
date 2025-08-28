using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
        private readonly IRedisCacheService _cache;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectsController(
            ILogger<SubjectsController> logger,
            SubjectManagementService subjectService,
            IRedisCacheService cache,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _subjectService = subjectService;
            _cache = cache;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubjects(CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"all_subjects_{user.Id}" : "all_subjects_anonymous";

            var subjects = _cache?.Get<IEnumerable<Subject?>>(cacheKey);
            if (subjects is not null)
            {
                _logger.LogInformation("Retrieved all subjects from cache");
                return Ok(subjects);
            }

            // If not in cache, get from database
            subjects = await _subjectService.GetAllSubjectsAsync(ct);

            // Store in cache
            _cache?.Set(cacheKey, subjects);

            return Ok(subjects);
        }

        [HttpGet("{subjectCode}")]
        public async Task<IActionResult> GetSubject(string subjectCode, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"subject_{subjectCode}_{user.Id}" : $"subject_{subjectCode}_anonymous";

            var subject = _cache?.Get<Subject?>(cacheKey);
            if (subject is not null)
            {
                _logger.LogInformation("Retrieved subject {SubjectCode} from cache", subjectCode);
                return Ok(subject);
            }

            // If not in cache, get from database
            subject = await _subjectService.GetSubjectByCodeAsync(subjectCode, ct);

            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            // Store in cache
            _cache?.Set(cacheKey, subject);

            return Ok(subject);
        }

        [HttpGet("{subjectCode}/prerequisites")]
        public async Task<IActionResult> GetPrerequisiteChain(string subjectCode, CancellationToken ct = default)
        {
            // Cache-aside: Try to get from cache first
            var user = await _userManager.GetUserAsync(User);
            var cacheKey = user != null ? $"prereq_chain_{subjectCode}_{user.Id}" : $"prereq_chain_{subjectCode}_anonymous";

            var chain = _cache?.Get<IEnumerable<Subject?>>(cacheKey);
            if (chain is not null)
            {
                _logger.LogInformation("Retrieved prerequisite chain for {SubjectCode} from cache", subjectCode);
                return Ok(chain);
            }

            // Verify subject exists
            var subject = await _subjectService.GetSubjectByCodeAsync(subjectCode, ct);
            if (subject == null)
                return NotFound($"Subject with code '{subjectCode}' not found.");

            // If not in cache, get from database
            chain = await _subjectService.GetPrerequisiteChainAsync(subjectCode, ct);

            // Store in cache
            _cache?.Set(cacheKey, chain);

            return Ok(chain);
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken ct = default)
        {
            try
            {
                // Write-around: Update database first
                var subject = await _subjectService.CreateSubjectAsync(request, ct);

                // Invalidate affected cache entries
                _cache?.Remove("all_subjects_anonymous");

                // If this subject has a prerequisite, invalidate the prerequisite chain cache for that prerequisite
                if (!string.IsNullOrEmpty(request.PrerequisiteSubjectCode))
                {
                    _cache?.Remove($"prereq_chain_{request.PrerequisiteSubjectCode}_anonymous");
                }

                _logger.LogInformation("Created new subject with code {SubjectCode} and invalidated relevant caches",
                    subject.SubjectCode);

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
                // Get original subject to check if prerequisite changed
                var originalSubject = await _subjectService.GetSubjectByCodeAsync(subjectCode, ct);
                var originalPrerequisite = originalSubject?.PrerequisiteSubjectCode;

                // Write-around: Update database first
                var subject = await _subjectService.UpdateSubjectAsync(subjectCode, request, ct);

                // Invalidate affected cache entries
                _cache?.Remove("all_subjects_anonymous");
                _cache?.Remove($"subject_{subjectCode}_anonymous");
                _cache?.Remove($"prereq_chain_{subjectCode}_anonymous");

                // If prerequisite changed, invalidate the old and new prerequisite chain caches
                if (originalPrerequisite != null)
                {
                    _cache?.Remove($"prereq_chain_{originalPrerequisite}_anonymous");
                }

                if (request.PrerequisiteSubjectCode != null &&
                    request.PrerequisiteSubjectCode != originalPrerequisite)
                {
                    _cache?.Remove($"prereq_chain_{request.PrerequisiteSubjectCode}_anonymous");
                }

                // Also invalidate course caches since they depend on subject data
                _cache?.Remove("all_courses_anonymous");

                _logger.LogInformation("Updated subject with code {SubjectCode} and invalidated relevant caches",
                    subject?.SubjectCode);

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