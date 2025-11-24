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

            var officeBlocks = ParseOfficeHours(supervisor.office_hours ?? "");
            if (!IsWithinOfficeHours(meeting.meeting_date, meeting.start_time, meeting.end_time, officeBlocks))
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

            var officeBlocks = ParseOfficeHours(supervisor.office_hours);
            var todaysBlocks = officeBlocks
                .Where(b => b.day == date.DayOfWeek)
                .ToList();

            if (!todaysBlocks.Any())
                return new List<(DateTime, DateTime)>(); // No office hours on this day

            var meetings = _meetingRepo.GetMeetingsBySupervisorAndDate(supervisorId, date);
            var freeSlots = new List<(DateTime start, DateTime end)>();

            foreach (var block in todaysBlocks)
            {
                // Break the 2-hour office block into 30-minute windows
                var slotStart = date.Date.Add(block.start);
                var blockEnd = date.Date.Add(block.end);

                while (slotStart < blockEnd)
                {
                    var slotEnd = slotStart.Add(TimeSpan.FromMinutes(30));
                    if (slotEnd > blockEnd)
                        break;

                    // Skip slots in the past (for today)
                    if (slotStart.Date == DateTime.Today && slotStart < DateTime.Now)
                    {
                        slotStart = slotStart.Add(TimeSpan.FromMinutes(30));
                        continue;
                    }

                    bool conflict = meetings.Any(m =>
                        slotStart.TimeOfDay < m.end_time &&
                        slotEnd.TimeOfDay > m.start_time);

                    if (!conflict)
                        freeSlots.Add((slotStart, slotEnd));

                    slotStart = slotStart.Add(TimeSpan.FromMinutes(30));
                }
            }

            return freeSlots.OrderBy(s => s.start).ToList();
        }


        private static bool IsWithinOfficeHours(
            DateTime date,
            TimeSpan start,
            TimeSpan end,
            List<(DayOfWeek day, TimeSpan start, TimeSpan end)> ranges)
        {
            foreach (var range in ranges)
            {
                if (range.day != date.DayOfWeek)
                    continue;

                if (start >= range.start && end <= range.end)
                    return true;
            }

            return false;
        }


        private static List<(DayOfWeek day, TimeSpan start, TimeSpan end)> ParseOfficeHours(string officeHours)
        {
            var result = new List<(DayOfWeek day, TimeSpan start, TimeSpan end)>();
            if (string.IsNullOrWhiteSpace(officeHours))
                return result;

            // e.g. "Monday 09:00-11:00, Thursday 14:00-16:00"
            var segments = officeHours.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                // "Monday 09:00-11:00"
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                string dayName = parts[0];        // "Monday"
                string timeRange = parts[^1];     // "09:00-11:00"

                if (!Enum.TryParse<DayOfWeek>(dayName, true, out var day))
                    continue;

                var times = timeRange.Split('-', StringSplitOptions.RemoveEmptyEntries);
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
