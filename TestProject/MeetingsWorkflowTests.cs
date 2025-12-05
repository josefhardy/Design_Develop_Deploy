using System;
using System.Collections.Generic;
using System.Linq;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Services;
using Xunit;

namespace Tests
{
    public class MeetingWorkflowTests
    {
        private readonly MeetingScheduler _scheduler;

        public MeetingWorkflowTests()
        {
            // Use test-mode scheduler with TestRepos
            _scheduler = new MeetingScheduler(
                TestRepos.TestMeetings,
                TestRepos.TestSupervisors
            );
        }

        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var start = DateTime.Today.AddDays(1); // start from tomorrow
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        [Fact]
        public void FetchAvailableSlots_ShouldReturnNonEmptySlots()
        {
            var nextMonday = GetNextWeekday(DayOfWeek.Monday);
            var supervisorId = TestRepos.TestSupervisors[0].supervisor_id;

            var slots = _scheduler.FetchAvailableSlots(supervisorId, nextMonday);

            Assert.NotEmpty(slots);
            foreach (var (start, end) in slots)
            {
                Assert.True(start.TimeOfDay >= TimeSpan.FromHours(9));
                Assert.True(end.TimeOfDay <= TimeSpan.FromHours(11));
            }
        }

        [Fact]
        public void ValidateMeeting_ShouldReturnFalse_WhenOutsideOfficeHours()
        {
            var nextMonday = GetNextWeekday(DayOfWeek.Monday);
            var meeting = new Meeting
            {
                supervisor_id = TestRepos.TestSupervisors[0].supervisor_id,
                student_id = 201,
                meeting_date = nextMonday,
                start_time = TimeSpan.FromHours(8),
                end_time = TimeSpan.FromHours(8.5)
            };

            var isValid = _scheduler.ValidateMeeting(meeting, out string message);

            Assert.False(isValid);
            Assert.Equal("Meeting is outside office hours.", message);
        }

        [Fact]
        public void ValidateMeeting_ShouldReturnTrue_WhenValidMeeting()
        {
            // Arrange: create a fresh in-memory list for this test
            var meetings = new List<Meeting>(); // empty so no conflicts
            var scheduler = new MeetingScheduler(meetings, TestRepos.TestSupervisors);

            var nextMonday = DateTime.Today.AddDays(((int)DayOfWeek.Monday - (int)DateTime.Today.DayOfWeek + 7) % 7);

            var meeting = new Meeting
            {
                supervisor_id = TestRepos.TestSupervisors[0].supervisor_id,
                student_id = 201,
                meeting_date = nextMonday,
                start_time = TimeSpan.FromHours(10),
                end_time = TimeSpan.FromHours(10.5)
            };

            // Act
            var isValid = scheduler.ValidateMeeting(meeting, out string message);

            // Assert
            Assert.True(isValid);
            Assert.Equal(string.Empty, message);
        }

    }
}
