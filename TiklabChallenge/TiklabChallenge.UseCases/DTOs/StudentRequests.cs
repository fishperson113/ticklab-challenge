using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.UseCases.DTOs
{
    public class UpdateProfileRequest
    {
        public string? StudentCode { get; set; }
        public string? FullName { get; set; }
    }
    public class StudentRegisterRequest
    {
        [EmailAddress]
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string FullName { get; set; }
        public required string StudentCode { get; set; }
    }

}
