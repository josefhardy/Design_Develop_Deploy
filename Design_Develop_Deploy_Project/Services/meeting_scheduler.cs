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

        // ✅ Test mode support
        private readonly List<Meeting> _testMeetings;
        private readonly List<Supervisor> _testSupervisors;

        // ✅ Production constructor (uses real repos)
        public MeetingScheduler(MeetingRepository meetingRepo, SupervisorRepository supervisorRepo)
        {
            _meetingRepo = meetingRepo;
            _supervisorRepo = supervisorRepo;

            _testMeetings = null;
            _testSupervisors = null;
        }

        // ✅ Test constructor (uses in-memory lists)
        public MeetingScheduler(List<Meeting> testMeetings, List<Supervisor> testSupervisors)
        {
            _meetingRepo = null;
            _supervisorRepo = null;

            _testMeetings = testMeetings;
            _testSupervisors = testSupervisors;
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

            // ✅ Get supervisor (test mode or production)
            var supervisor = _testSupervisors != null
                ? _testSupervisors.Find(s => s.supervisor_id == meeting.supervisor_id)
                : _supervisorRepo.GetSupervisorById(meeting.supervisor_id);

            if (supervisor == null)
            {
                message = "Supervisor not found.";
                return false;
            }

            var officeBlocks = ParseOfficeHours(supervisor.office_hours ?? "");
            if (!IsWithinOfficeHours(meeting.meeting_date, meeting.start_time, meeting.end_time, officeBlocks))
            {
                message = "Meeting is outside office hours.";
                return false;
            }

            // ✅ Get same-day meetings (test mode or production)
            var sameDayMeetings = _testMeetings != null
                ? _testMeetings.FindAll(m =>
                    m.supervisor_id == meeting.supervisor_id &&
                    m.meeting_date.Date == meeting.meeting_date.Date)
                : _meetingRepo.GetMeetingsBySupervisorAndDate(meeting.supervisor_id, meeting.meeting_date);

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

        public List<(DateTime, DateTime)> FetchAvailableSlots(int supervisorId, DateTime date)
        {
            // ✅ Get supervisor (test mode or production)
            var supervisor = _testSupervisors != null
                ? _testSupervisors.Find(s => s.supervisor_id == supervisorId)
                : _supervisorRepo.GetSupervisorById(supervisorId);

            if (supervisor == null || string.IsNullOrWhiteSpace(supervisor.office_hours))
                return new List<(DateTime, DateTime)>();

            var officeBlocks = ParseOfficeHours(supervisor.office_hours);

            var todaysBlocks = officeBlocks
                .Where(b => b.day == date.DayOfWeek)
                .ToList();

            if (!todaysBlocks.Any())
                return new List<(DateTime, DateTime)>();

            // ✅ Get same-day meetings (test mode or production)
            var meetings = _testMeetings != null
                ? _testMeetings.FindAll(m =>
                    m.supervisor_id == supervisorId &&
                    m.meeting_date.Date == date.Date)
                : _meetingRepo.GetMeetingsBySupervisorAndDate(supervisorId, date);

            var freeSlots = new List<(DateTime, DateTime)>();

            foreach (var block in todaysBlocks)
            {
                DateTime slotStart = date.Date.Add(block.start);
                DateTime blockEnd = date.Date.Add(block.end);

                while (slotStart < blockEnd)
                {
                    DateTime slotEnd = slotStart.AddMinutes(30);
                    if (slotEnd > blockEnd) break;

                    bool conflict = meetings.Any(m =>
                    {
                        DateTime meetingStart = date.Date.Add(m.start_time);
                        DateTime meetingEnd = date.Date.Add(m.end_time);
                        return slotStart < meetingEnd && slotEnd > meetingStart;
                    });

                    if (!conflict) freeSlots.Add((slotStart, slotEnd));

                    slotStart = slotStart.AddMinutes(30);
                }
            }

            return freeSlots.OrderBy(s => s.Item1).ToList();
        }

        // ✅ Private helpers
        private static bool IsWithinOfficeHours(
            DateTime date,
            TimeSpan start,
            TimeSpan end,
            List<(DayOfWeek day, TimeSpan start, TimeSpan end)> ranges)
        {
            foreach (var range in ranges)
            {
                if (range.day != date.DayOfWeek) continue;
                if (start >= range.start && end <= range.end) return true;
            }
            return false;
        }

        private static List<(DayOfWeek day, TimeSpan start, TimeSpan end)> ParseOfficeHours(string officeHours)
        {
            var result = new List<(DayOfWeek day, TimeSpan start, TimeSpan end)>();
            if (string.IsNullOrWhiteSpace(officeHours)) return result;

            var segments = officeHours.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var parts = segment.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                if (!Enum.TryParse<DayOfWeek>(parts[0], true, out var day)) continue;

                var times = parts[^1].Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (times.Length == 2 &&
                    TimeSpan.TryParse(times[0], out var start) &&
                    TimeSpan.TryParse(times[1], out var end))
                {
                    result.Add((day, start, end));
                }
            }

            return result;
        }
    }
}
