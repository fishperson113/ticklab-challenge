using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiklabChallenge.UseCases.Services
{
    public class BusinessMetricsService
    {
        private readonly Meter _meter;
        private readonly Counter<long> _loginAttemptCounter;
        private readonly Counter<long> _loginSuccessCounter;
        private readonly Counter<long> _loginFailureCounter;
        private readonly Counter<long> _courseRequestCounter;
        private readonly Histogram<double> _courseRequestDuration;

        public BusinessMetricsService()
        {
            _meter = new Meter("TiklabChallenge.Business");

            // Authentication metrics
            _loginAttemptCounter = _meter.CreateCounter<long>("login.attempts", "count", "Total number of login attempts");
            _loginSuccessCounter = _meter.CreateCounter<long>("login.success", "count", "Number of successful logins");
            _loginFailureCounter = _meter.CreateCounter<long>("login.failure", "count", "Number of failed logins");

            // Course metrics
            _courseRequestCounter = _meter.CreateCounter<long>("courses.requests", "count", "Number of course list requests");
            _courseRequestDuration = _meter.CreateHistogram<double>("courses.request.duration", "ms", "Duration of course list requests");
        }

        // Authentication metrics methods
        public void RecordLoginAttempt() => _loginAttemptCounter.Add(1);
        public void RecordLoginSuccess() => _loginSuccessCounter.Add(1);
        public void RecordLoginFailure() => _loginFailureCounter.Add(1);

        // Course metrics methods
        public void RecordCourseRequest() => _courseRequestCounter.Add(1);
        public void RecordCourseRequestDuration(double milliseconds) => _courseRequestDuration.Record(milliseconds);
    }
}
