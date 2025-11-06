using System;
using System.Collections.Generic;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;

namespace Design_Develop_Deploy_Project.Services
{
    public class MeetingScheduler
    {
        private readonly MeetingRepository meetingRepo;

        public MeetingScheduler(MeetingRepository _meetingRepo)
        {
            meetingRepo = _meetingRepo;
        }

        public bool IsWithinOfficeHours(TimeSpan start, TimeSpan end, string officeHours)
        {
            var ranges = ParseOfficeHours(officeHours);
            foreach (var range in ranges)
            {
                if (start >= range.Start && end <= range.End)
                    return true;
            }
            return false;
        }

        public bool IsOverlapping(int supervisorId, DateTime date, TimeSpan start, TimeSpan end)
        {
            var meetings = meetingRepo.GetMeetingsBySupervisorAndDate(supervisorId, date);
            foreach (var m in meetings)
            {
                if (start < m.EndTime && end > m.StartTime)
                    return true;
            }
            return false;
        }

        private List<(TimeSpan Start, TimeSpan End)> ParseOfficeHours(string officeHours)
        {
            var result = new List<(TimeSpan, TimeSpan)>();
            var parts = officeHours.Split(',');
            foreach (var part in parts)
            {
                var times = part.Split('-');
                if (times.Length == 2 &&
                    TimeSpan.TryParse(times[0], out var start) &&
                    TimeSpan.TryParse(times[1], out var end))
                {
                    result.Add((start, end));
                }
            }
            return result;
        }
    }
}
