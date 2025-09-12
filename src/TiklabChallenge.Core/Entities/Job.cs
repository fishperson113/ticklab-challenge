using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.Core.Entities
{
    public class Job
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public int Attempts { get; set; } = 0;
        public int MaxAttempts { get; set; } = 3;
        public DateTime? NextAttemptAt { get; set; }
        public int Priority { get; set; } = 0; // Higher number = higher priority
        public string Result { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
