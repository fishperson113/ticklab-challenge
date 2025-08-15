using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.Core.Shared;
using TiklabChallenge.UseCases.DTOs;
namespace TiklabChallenge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Student)]
    public class StudentsController : ControllerBase
    {
        private readonly ILogger<StudentsController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(ILogger<StudentsController> logger, IUnitOfWork unitOfWork
            , UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var student = await _unitOfWork.Students.GetByIdAsync(user.Id);
            if (student is null) return NotFound("Student profile not found.");

            return Ok(new
            {
                student.UserId,
                student.StudentCode,
                student.FullName
            });
        }
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest dto, CancellationToken ct)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized("Cannot determine current user.");

            var student = await _unitOfWork.Students.GetByIdAsync(user.Id);
            if (student is null) return NotFound("Student profile not found.");

            if (dto.StudentCode != null)
            {
                student.StudentCode = dto.StudentCode;
            }

            if(dto.FullName != null)
            {
                student.FullName = dto.FullName;
            }    

            await _unitOfWork.Students.UpdateProfileAsync(student);
            await _unitOfWork.CommitAsync();

            return Ok();
        }
    }
}
