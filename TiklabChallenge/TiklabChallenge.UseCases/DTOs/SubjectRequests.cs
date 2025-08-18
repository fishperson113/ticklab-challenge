using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.UseCases.DTOs
{
    public class CreateSubjectRequest
    {
        [Required]
        public required string SubjectCode { get; set; }

        public string? SubjectName { get; set; }

        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int DefaultCredits { get; set; }

        public string? PrerequisiteSubjectCode { get; set; }
    }

    public class UpdateSubjectRequest
    {
        public string? SubjectName { get; set; }

        public string? Description { get; set; }

        [Range(0, int.MaxValue)]
        public int? DefaultCredits { get; set; }

        public string? PrerequisiteSubjectCode { get; set; }
    }
}
