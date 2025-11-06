using System;
using System.Collections.Generic;
using System.Linq;
using Design_Develop_Deploy_Project.Objects;
using Design_Develop_Deploy_Project.Repos;

namespace Design_Develop_Deploy_Project.Services
{
    public class MeetingScheduler
    {
        private readonly MeetingRepository _meetingRepo;
        private readonly SupervisorRepository _supervisorRepo;

        public MeetingScheduler(MeetingRepository meetingRepo, SupervisorRepository supervisorRepo)
        {
            _meetingRepo = meetingRepo;
            _supervisorRepo = supervisorRepo;
        }

        public bool ValidateMeeting(Meeting meeting, out string message)
        {
            message = string.Empty;

            if (meeting == null)
            {
                message = "Meeting cannot be null.";
                return false;
            }

            if (meeting.start_time >= meeting.end_time)
            {
                message = "End time must be after start time.";
                return false;
            }

            if (meeting.meeting_date.Date < DateTime.Today)
            {
                message = "Cannot schedule meetings in the past.";
                return false;
            }

            var supervisor = _supervisorRepo.GetSupervisorById(meeting.supervisor_id);
            if (supervisor == null)
            {
                message = "Supervisor not found.";
                return false;
            }

            var allowedHours = ParseOfficeHours(supervisor.office_hours ?? "");
            if (!IsWithinOfficeHours(meeting.start_time, meeting.end_time, allowedHours))
            {
                message = "Meeting is outside office hours.";
                return false;
            }

            var sameDayMeetings = _meetingRepo.GetMeetingsBySupervisorAndDate(meeting.supervisor_id, meeting.meeting_date);
            foreach (var m in sameDayMeetings)
            {
                if (meeting.start_time < m.end_time && meeting.end_time > m.start_time)
                {
                    message = "Meeting overlaps another scheduled meeting.";
                    return false;
                }
            }

            return true;
        }

        public List<(DateTime start, DateTime end)> FetchAvailableSlots(int supervisorId, DateTime date)
        {
            var supervisor = _supervisorRepo.GetSupervisorById(supervisorId);
            if (supervisor == null || string.IsNullOrWhiteSpace(supervisor.office_hours))
                return new List<(DateTime, DateTime)>();

            var officeHours = ParseOfficeHours(supervisor.office_hours);
            var meetings = _meetingRepo.GetMeetingsBySupervisorAndDate(supervisorId, date);

            var freeSlots = new List<(DateTime start, DateTime end)>();

            foreach (var (start, end) in officeHours)
            {
                var slotStart = date.Date.Add(start);
                var slotEnd = date.Date.Add(end);

                bool conflict = meetings.Any(m =>
                    slotStart.TimeOfDay < m.end_time &&
                    slotEnd.TimeOfDay > m.start_time);

                if (!conflict)
                    freeSlots.Add((slotStart, slotEnd));
            }

            return freeSlots;
        }

        private static bool IsWithinOfficeHours(TimeSpan start, TimeSpan end, List<(TimeSpan start, TimeSpan end)> ranges)
        {
            foreach (var range in ranges)
                if (start >= range.start && end <= range.end)
                    return true;
            return false;
        }

        private static List<(TimeSpan start, TimeSpan end)> ParseOfficeHours(string officeHours)
        {
            var result = new List<(TimeSpan start, TimeSpan end)>();
            if (string.IsNullOrWhiteSpace(officeHours))
                return result;

            foreach (var segment in officeHours.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var times = segment.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (times.Length == 2 &&
                    TimeSpan.TryParse(times[0], out var s) &&
                    TimeSpan.TryParse(times[1], out var e))
                {
                    result.Add((s, e));
                }
            }

            return result;
        }
    }
}
