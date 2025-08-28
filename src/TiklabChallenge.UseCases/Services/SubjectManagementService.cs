using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiklabChallenge.Core.Entities;
using TiklabChallenge.Core.Interfaces;
using TiklabChallenge.UseCases.DTOs;

namespace TiklabChallenge.UseCases.Services
{
    public class SubjectManagementService
    {
        private readonly IUnitOfWork _uow;

        public SubjectManagementService(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<IEnumerable<Subject?>> GetAllSubjectsAsync(CancellationToken ct = default)
        {
            return await _uow.Subjects.GetAllAsync(ct);
        }

        public async Task<Subject?> GetSubjectByCodeAsync(string subjectCode, CancellationToken ct = default)
        {
            return await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
        }

        public async Task<Subject> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken ct = default)
        {
            // Check if subject with this code already exists
            var exists = await _uow.Subjects.ExistsAsync(s => s.SubjectCode == request.SubjectCode, ct);
            if (exists)
                throw new InvalidOperationException($"Subject with code '{request.SubjectCode}' already exists.");

            // Validate prerequisite if provided
            if (!string.IsNullOrWhiteSpace(request.PrerequisiteSubjectCode))
            {
                var (isValid, errorMessage) = await ValidatePrerequisiteAsync(
                    request.SubjectCode, request.PrerequisiteSubjectCode, ct);

                if (!isValid)
                    throw new ArgumentException(errorMessage);
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

            return subject;
        }

        public async Task<Subject?> UpdateSubjectAsync(
            string subjectCode,
            UpdateSubjectRequest request,
            CancellationToken ct = default)
        {
            var subject = await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
            if (subject == null)
                throw new KeyNotFoundException($"Subject with code '{subjectCode}' not found.");

            // Validate prerequisite if it's being changed
            if (request.PrerequisiteSubjectCode != null &&
                request.PrerequisiteSubjectCode != subject.PrerequisiteSubjectCode)
            {
                var (isValid, errorMessage) = await ValidatePrerequisiteAsync(
                    subjectCode, request.PrerequisiteSubjectCode, ct);

                if (!isValid)
                    throw new ArgumentException(errorMessage);
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
            return await _uow.Subjects.GetBySubjectCodeAsync(subjectCode, ct);
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
    }
}
