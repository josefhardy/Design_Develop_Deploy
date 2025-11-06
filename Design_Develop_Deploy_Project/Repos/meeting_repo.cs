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

        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();

                const string insertSql = @"
                INSERT INTO Meetings
                (student_id, supervisor_id, meeting_date, start_time, end_time, notes, created_at, updated_at)
                VALUES (@StudentId, @SupervisorId, @MeetingDate, @StartTime, @EndTime, @Notes, @CreatedAt, @UpdatedAt)";

                using (var cmd = new SQLiteCommand(insertSql, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", meeting.student_id);
                    cmd.Parameters.AddWithValue("@SupervisorId", meeting.supervisor_id);
                    cmd.Parameters.AddWithValue("@MeetingDate", meeting.meeting_date.Date);
                    cmd.Parameters.AddWithValue("@StartTime", meeting.start_time);
                    cmd.Parameters.AddWithValue("@EndTime", meeting.end_time);
                    cmd.Parameters.AddWithValue("@Notes", (object?)meeting.notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

                    cmd.ExecuteNonQuery();
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding meeting: {ex.Message}");
            return false;
        }
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

    public List<Meeting> GetMeetingsBySupervisorAndDate(int supervisorId, DateTime date)
    {
        var meetings = new List<Meeting>();
        try
        {
            using (var conn = new SQLiteConnection(_connectionString))
            {
                conn.Open();
                const string query = @"
                SELECT *
                FROM Meetings
                WHERE supervisor_id = @SupervisorId
                  AND meeting_date = @MeetingDate
                ORDER BY start_time ASC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SupervisorId", supervisorId);
                    cmd.Parameters.AddWithValue("@MeetingDate", date.Date);

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
            Console.WriteLine($"Error retrieving meetings by supervisor/date: {ex.Message}");
        }

        return meetings;
    }







}
