using System;
using System.Collections.Generic;
using Xunit;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Services;
using Tests;

namespace Tests
{
    public class SupervisorWorkflowTests
    {
        private SupervisorService _supervisorService;
        private Supervisor _supervisor;

        public SupervisorWorkflowTests()
        {
            // Pick the first test supervisor from TestRepos
            _supervisor = TestRepos.TestSupervisors[0];

            // Inject test supervisor and enable test mode
            _supervisorService = new SupervisorService(null, _supervisor, testMode: true);
        }

        [Fact]
        public void SupervisorCanUpdateOfficeHours_TestMode()
        {
            // Arrange
            string originalHours = _supervisor.office_hours;
            var newHours = new List<string> { "Monday 09:00-11:00", "Tuesday 14:00-16:00" };

            // Act
            _supervisorService.UpdateOfficeHours(newHours);
            string updatedHours = _supervisor.office_hours;

            // Assert
            Assert.Equal(string.Join(",", newHours), updatedHours);
            Assert.NotEqual(originalHours, updatedHours);
        }

        [Fact]
        public void SupervisorCanViewPerformanceMetrics_TestMode()
        {
            // Act
            var (meetingsBooked, wellbeingChecks) = _supervisorService.ViewPerformanceMetrics();

            // Assert
            Assert.Equal(_supervisor.meetings_booked_this_month, meetingsBooked);
            Assert.Equal(_supervisor.wellbeing_checks_this_month, wellbeingChecks);
        }
    }
}
