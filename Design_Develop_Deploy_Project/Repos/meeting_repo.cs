using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Design_Develop_Deploy_Project.Objects;

namespace Design_Develop_Deploy_Project.Repos;

public class MeetingRepository
{
	public string _connectionString { get; set; }

    public MeetingRepository(string connectionString)
	{
		_connectionString = connectionString;
    }

	public MeetingRepository() { }

    public bool AddMeeting(Meeting meeting)
    {
        if (meeting == null) throw new ArgumentNullException(nameof(meeting));
        if (meeting.student_id <= 0 || meeting.supervisor_id <= 0)
            throw new ArgumentException("Invalid student or supervisor ID.");
        if (meeting.start_time >= meeting.end_time)
            throw new ArgumentException("Start time must be before end time.");
        if (meeting.meeting_date.Date < DateTime.Today)
            throw new ArgumentException("Cannot schedule a meeting in the past.");

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1. Check for conflicts
                string conflictQuery = @"
                SELECT COUNT(*) 
                FROM Meetings
                WHERE supervisor_id = @SupervisorId
                  AND meeting_date = @MeetingDate
                  AND ((@StartTime < end_time AND @EndTime > start_time))";

                using (var cmd = new SQLiteCommand(conflictQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", meeting.supervisor_id);
                    cmd.Parameters.AddWithValue("@MeetingDate", meeting.meeting_date.Date);
                    cmd.Parameters.AddWithValue("@StartTime", meeting.start_time);
                    cmd.Parameters.AddWithValue("@EndTime", meeting.end_time);

                    int conflictCount = Convert.ToInt32(cmd.ExecuteScalar());
                    if (conflictCount > 0)
                    {
                        // Conflict exists
                        return false;
                    }
                }

                // 2. Insert the meeting
                string insertQuery = @"
                INSERT INTO Meetings (student_id, supervisor_id, meeting_date, start_time, end_time, notes)
                VALUES (@StudentId, @SupervisorId, @MeetingDate, @StartTime, @EndTime, @Notes)";

                using (var cmd = new SQLiteCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", meeting.student_id);
                    cmd.Parameters.AddWithValue("@SupervisorId", meeting.supervisor_id);
                    cmd.Parameters.AddWithValue("@MeetingDate", meeting.meeting_date.Date);
                    cmd.Parameters.AddWithValue("@StartTime", meeting.start_time);
                    cmd.Parameters.AddWithValue("@EndTime", meeting.end_time);
                    cmd.Parameters.AddWithValue("@Notes", meeting.notes ?? "");

                    cmd.ExecuteNonQuery();
                }

                // Optionally, update supervisor's meetings booked count
                string updateSupervisorQuery = @"
                UPDATE Supervisors
                SET meetings_booked_last_month = meetings_booked_last_month + 1
                WHERE supervisor_id = @SupervisorId";

                using (var cmd = new SQLiteCommand(updateSupervisorQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", meeting.supervisor_id);
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding meeting: {ex.Message}");
            return false;
        }
    }

    public Meeting GetMeetingById(int meetingId) 
	{
        if (meetingId <= 0) 
        {
            Console.WriteLine("Invalid meeting ID.");
            return null;
        }

        try 
        {
            using(var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                SELECT meeting_id, student_id, supervisor_id, meeting_date, start_time, end_time, notes
                FROM Meetings
                WHERE meeting_id = @MeetingId";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@MeetingId", meetingId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToMeeting(reader);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving meeting by ID: {ex.Message}");
            return null;
        }
        return null;
    }

    public Meeting GetMeetingByVariable(int? student_id = null, int? supervisor_id = null, DateTime? meeting_date = null)
    {
        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                string query = @"SELECT *
                             FROM Meetings
                             WHERE 1 = 1";

                if (student_id.HasValue) query += " AND student_id = @StudentId";
                if (supervisor_id.HasValue) query += " AND supervisor_id = @SupervisorId";
                if (meeting_date.HasValue) query += " AND meeting_date = @MeetingDate";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    if (student_id.HasValue) cmd.Parameters.AddWithValue("@StudentId", student_id.Value);
                    if (supervisor_id.HasValue) cmd.Parameters.AddWithValue("@SupervisorId", supervisor_id.Value);
                    if (meeting_date.HasValue) cmd.Parameters.AddWithValue("@MeetingDate", meeting_date.Value.Date);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapReaderToMeeting(reader);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving meeting by variable: {ex.Message}");
        }

        return null;
    }

    public bool UpdateMeeting(Meeting updatedMeeting)
    {
        if (updatedMeeting == null || updatedMeeting.meeting_id <= 0)
            throw new ArgumentException("Invalid meeting provided.");

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                // 1. Fetch supervisor office hours for that date
                string officeHoursQuery = @"SELECT office_hours 
                                        FROM Supervisors 
                                        WHERE supervisor_id = @SupervisorId";
                string officeHoursStr;
                using (var cmd = new SQLiteCommand(officeHoursQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", updatedMeeting.supervisor_id);
                    officeHoursStr = cmd.ExecuteScalar()?.ToString();
                    if (string.IsNullOrEmpty(officeHoursStr))
                        throw new Exception("Supervisor office hours not found.");
                }

                // 2. Parse office hours string into usable time ranges
                // Example: "09:00-12:00,13:00-17:00"
                var allowedRanges = ParseOfficeHours(officeHoursStr);

                if (!IsWithinOfficeHours(updatedMeeting.start_time, updatedMeeting.end_time, allowedRanges))
                    throw new Exception("Meeting time is outside supervisor office hours.");

                // 3. Check for conflicts with other meetings
                string conflictQuery = @"
                SELECT COUNT(*) 
                FROM Meetings 
                WHERE meeting_id != @MeetingId
                  AND supervisor_id = @SupervisorId
                  AND ((start_time < @EndTime AND end_time > @StartTime))";
                using (var cmd = new SQLiteCommand(conflictQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MeetingId", updatedMeeting.meeting_id);
                    cmd.Parameters.AddWithValue("@SupervisorId", updatedMeeting.supervisor_id);
                    cmd.Parameters.AddWithValue("@StartTime", updatedMeeting.start_time);
                    cmd.Parameters.AddWithValue("@EndTime", updatedMeeting.end_time);

                    int conflicts = Convert.ToInt32(cmd.ExecuteScalar());
                    if (conflicts > 0)
                        throw new Exception("Meeting conflicts with an existing appointment.");
                }

                // Optional: Check student conflicts similarly
                // ...

                // 4. Update the meeting
                string updateQuery = @"
                UPDATE Meetings
                SET meeting_date = @MeetingDate,
                    start_time = @StartTime,
                    end_time = @EndTime,
                    notes = @Notes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE meeting_id = @MeetingId";

                using (var cmd = new SQLiteCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MeetingDate", updatedMeeting.meeting_date.Date);
                    cmd.Parameters.AddWithValue("@StartTime", updatedMeeting.start_time);
                    cmd.Parameters.AddWithValue("@EndTime", updatedMeeting.end_time);
                    cmd.Parameters.AddWithValue("@Notes", updatedMeeting.notes ?? string.Empty);
                    cmd.Parameters.AddWithValue("@MeetingId", updatedMeeting.meeting_id);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating meeting: {ex.Message}");
            return false;
        }
    }

    public void DeleteMeeting(int meetingId) 
	{
        if(meetingId <= 0)
            throw new ArgumentException("Invalid meeting ID.");
        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string deleteQuery = @"
                DELETE FROM Meetings
                WHERE meeting_id = @MeetingId";
                using (var cmd = new SQLiteCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MeetingId", meetingId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting meeting: {ex.Message}");
        }
    }

    public List<(DateTime start, DateTime end)> FetchAvailableSlots(int supervisorId, DateTime desiredDate)
    {
        if (desiredDate.Date < DateTime.Today)
            throw new ArgumentException("Cannot fetch slots for past dates.");
        if (supervisorId <= 0)
            throw new ArgumentException("Invalid supervisor ID.");

        var availableSlots = new List<(DateTime start, DateTime end)>();
        var takenSlots = new List<(DateTime start, DateTime end)>();

        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();

            // 1️⃣ Get meetings already booked for this supervisor on the desired date
            string meetingsQuery = @"
        SELECT start_time, end_time 
        FROM Meetings
        WHERE supervisor_id = @SupervisorId
          AND meeting_date = @MeetingDate";

            using (var cmd = new SQLiteCommand(meetingsQuery, conn))
            {
                cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                cmd.Parameters.AddWithValue("@MeetingDate", desiredDate.Date);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (TimeSpan.TryParse(reader["start_time"]?.ToString(), out TimeSpan start) &&
                            TimeSpan.TryParse(reader["end_time"]?.ToString(), out TimeSpan end))
                        {
                            var startDt = desiredDate.Date.Add(start);
                            var endDt = desiredDate.Date.Add(end);
                            takenSlots.Add((startDt, endDt));
                        }
                    }
                }
            }

            // 2️⃣ Get supervisor office hours (e.g. "Monday 09:00-11:00; Thursday 11:00-13:00")
            string officeHoursString;
            using (var cmd = new SQLiteCommand("SELECT office_hours FROM Supervisors WHERE supervisor_id = @SupervisorId", conn))
            {
                cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                officeHoursString = cmd.ExecuteScalar()?.ToString();
            }

            if (string.IsNullOrWhiteSpace(officeHoursString))
                return availableSlots;

            // 3️⃣ Match the desiredDate day with one of the supervisor’s office-hour days
            var sessions = officeHoursString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => s.Trim());

            foreach (var session in sessions)
            {
                var parts = session.Split(' ');
                if (parts.Length != 2) continue;

                string dayName = parts[0];
                if (!dayName.Equals(desiredDate.DayOfWeek.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue; // skip sessions for other days

                var times = parts[1].Split('-');
                if (times.Length != 2) continue;

                var start = TimeSpan.Parse(times[0]);
                var end = TimeSpan.Parse(times[1]);

                // 4️⃣ Generate 30-minute slots within this 2-hour window
                for (var t = start; t < end; t = t.Add(TimeSpan.FromMinutes(30)))
                {
                    var slotStart = desiredDate.Date.Add(t);
                    var slotEnd = desiredDate.Date.Add(t.Add(TimeSpan.FromMinutes(30)));

                    // Check if slot conflicts with booked meetings
                    bool isTaken = takenSlots.Any(b =>
                        (slotStart < b.end) && (slotEnd > b.start));

                    if (!isTaken)
                        availableSlots.Add((slotStart, slotEnd));
                }
            }
        }

        return availableSlots;
    }

    private Meeting MapReaderToMeeting(SQLiteDataReader reader)
    {
        return new Meeting
        {
            meeting_id = Convert.ToInt32(reader["meeting_id"]),
            student_id = Convert.ToInt32(reader["student_id"]),
            supervisor_id = Convert.ToInt32(reader["supervisor_id"]),
            meeting_date = Convert.ToDateTime(reader["meeting_date"]),
            start_time = TimeSpan.Parse(reader["start_time"].ToString()),
            end_time = TimeSpan.Parse(reader["end_time"].ToString()),
            notes = reader["notes"] == DBNull.Value ? null : reader["notes"].ToString(),
            created_at = Convert.ToDateTime(reader["created_at"]),
            updated_at = Convert.ToDateTime(reader["updated_at"])

        };
    }

    private bool IsWithinOfficeHours(TimeSpan startTime, TimeSpan endTime, List<(TimeSpan start, TimeSpan end)> allowedRanges)
    {
        var meetingStart = startTime;
        var meetingEnd = endTime;

        foreach (var range in allowedRanges)
        {
            if (meetingStart >= range.start && meetingEnd <= range.end)
                return true; // Meeting fits in this range
        }

        return false; // No suitable range found
    }

    private List<(TimeSpan start, TimeSpan end)> ParseOfficeHours(string officeHoursStr)
    {
        var ranges = new List<(TimeSpan start, TimeSpan end)>();

        if (string.IsNullOrWhiteSpace(officeHoursStr))
            return ranges;

        // Split multiple ranges separated by commas
        var parts = officeHoursStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var times = part.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (times.Length == 2 &&
                TimeSpan.TryParse(times[0], out var start) &&
                TimeSpan.TryParse(times[1], out var end))
            {
                ranges.Add((start, end));
            }
        }

        return ranges;
    }
    public List<Meeting> GetMeetingsBySupervisorId(int supervisor_id)
    {
        var meetings = new List<Meeting>();

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                string query = @"
                SELECT * 
                FROM Meetings 
                WHERE supervisor_id = @SupervisorId
                ORDER BY meeting_date ASC, start_time ASC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisor_id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) 
                        {
                            meetings.Add(MapReaderToMeeting(reader));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving meetings for supervisor: {ex.Message}");
        }

        return meetings;
    }






}
