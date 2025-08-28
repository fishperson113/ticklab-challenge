using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Subject
    {
        public required string SubjectCode { get; set; }      // ví dụ: "CS01"
        public string? SubjectName { get; set; }
        public string? Description { get; set; }
        public int DefaultCredits { get; set; }
        public string? PrerequisiteSubjectCode { get; set; }
        public Subject? PrerequisiteSubject { get; set; }
        [JsonIgnore]
        public ICollection<Subject> RequiredBySubjects { get; set; } = new List<Subject>();
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
