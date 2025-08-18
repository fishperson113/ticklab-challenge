using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;

namespace TiklabChallenge.UseCases.Services
{
    public class SubjectValidationService
    {
        public readonly IUnitOfWork _uow;

        public SubjectValidationService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // Check if a student can take a subject based on prerequisites
        public async Task<(bool IsValid, string? ErrorMessage)> CanTakeSubjectAsync(
            string studentId, string subjectCode, CancellationToken ct = default)
        {
            // Get the subject
            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
            if (subject == null)
                return (false, $"Subject '{subjectCode}' not found.");

            // If no prerequisites, student can take it
            if (string.IsNullOrEmpty(subject.PrerequisiteSubjectCode))
                return (true, null);

            // Check if student has completed the prerequisite
            var hasCompletedPrereq = await CheckPrerequisiteCompletionAsync(studentId, subject.PrerequisiteSubjectCode, ct);

            return hasCompletedPrereq
                ? (true, null)
                : (false, $"Student must complete {subject.PrerequisiteSubjectCode} before taking {subjectCode}");
        }

        private async Task<bool> CheckPrerequisiteCompletionAsync(
            string studentId, string prerequisiteCode, CancellationToken ct = default)
        {
            // Get all enrollments for the student
            var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);

            return enrollments.Any(e =>
                e?.CourseCode.StartsWith(prerequisiteCode) == true &&
                (e?.Status == EnrollmentStatus.Enrolled));
        }
        public async Task<(bool IsValid, string? ErrorMessage)> ValidatePrerequisiteAsync(
            string subjectCode, string? prerequisiteCode, CancellationToken ct = default)
        {
            // If no prerequisite specified, it's valid
            if (string.IsNullOrWhiteSpace(prerequisiteCode))
                return (true, null);

            // Trim the prerequisite code
            var trimmedPrereqCode = prerequisiteCode.Trim();

            // Check if the prerequisite subject exists
            var prereqExists = await _uow.Subjects.ExistsAsync(s => s.SubjectCode == trimmedPrereqCode, ct);
            if (!prereqExists)
                return (false, $"Prerequisite subject '{trimmedPrereqCode}' not found.");

            // Check for circular dependency
            var hasCircular = await HasCircularPrerequisiteAsync(subjectCode, trimmedPrereqCode, ct);
            if (hasCircular)
                return (false, $"Setting '{trimmedPrereqCode}' as prerequisite would create a circular dependency.");

            return (true, null);
        }

        public async Task<bool> HasCircularPrerequisiteAsync(
            string subjectCode, string prereqCode, CancellationToken ct = default)
        {
            if (subjectCode == prereqCode)
                return true;

            var visited = new HashSet<string>();
            return await CheckCircularDependencyAsync(subjectCode, prereqCode, visited, ct);
        }

        private async Task<bool> CheckCircularDependencyAsync(
            string originalSubjectCode,
            string currentPrereqCode,
            HashSet<string> visited,
            CancellationToken ct = default)
        {
            if (visited.Contains(currentPrereqCode))
                return false;

            visited.Add(currentPrereqCode);

            var prereq = await _uow.Subjects.GetBySubjectCodeAsync(currentPrereqCode, ct);

            if (prereq == null || string.IsNullOrEmpty(prereq.PrerequisiteSubjectCode))
                return false;

            if (prereq.PrerequisiteSubjectCode == originalSubjectCode)
                return true;

            return await CheckCircularDependencyAsync(originalSubjectCode, prereq.PrerequisiteSubjectCode, visited, ct);
        }
        public async Task<List<Subject>> GetPrerequisiteChainAsync(string subjectCode, CancellationToken ct = default)
        {
            var chain = new List<Subject>();
            var visited = new HashSet<string>();

            await BuildPrerequisiteChainAsync(subjectCode, chain, visited, ct);

            return chain;
        }

        private async Task BuildPrerequisiteChainAsync(
            string subjectCode,
            List<Subject> chain,
            HashSet<string> visited,
            CancellationToken ct = default)
        {
            if (visited.Contains(subjectCode))
                return;

            visited.Add(subjectCode);

            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
            if (subject == null || string.IsNullOrEmpty(subject.PrerequisiteSubjectCode))
                return;

            var prereq = await _uow.Subjects.GetBySubjectCodeAsync(subject.PrerequisiteSubjectCode, ct);
            if (prereq != null)
            {
                chain.Add(prereq);
                await BuildPrerequisiteChainAsync(prereq.SubjectCode, chain, visited, ct);
            }
        }

        public async Task<Dictionary<string, string?>> ValidateSubjectsForStudentAsync(
            string studentId,
            IEnumerable<string> subjectCodes,
            CancellationToken ct = default)
        {
            var results = new Dictionary<string, string?>();

            foreach (var subjectCode in subjectCodes)
            {
                var (isValid, errorMessage) = await CanTakeSubjectAsync(studentId, subjectCode, ct);
                if (!isValid)
                {
                    results[subjectCode] = errorMessage;
                }
                else
                {
                    results[subjectCode] = null;  // null means no error
                }
            }

            return results;
        }
    }
}
