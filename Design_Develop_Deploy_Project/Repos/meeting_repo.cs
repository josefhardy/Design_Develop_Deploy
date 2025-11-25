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
            throw new Exception($"Error adding meeting: {ex.Message}", ex);
        }
    }

    public List<Meeting> GetMeetingsByStudentId(int studentId)
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
                WHERE student_id = @StudentId
                AND meeting_date >= date('now')        -- 🔥 filter out past meetings
                ORDER BY meeting_date ASC, start_time ASC";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);

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
            throw new Exception($"Error retrieving meetings by student ID: {ex.Message}", ex);
        }

        return meetings;
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
            throw new Exception($"Error deleting meeting: {ex.Message}", ex);
        }
    }

    private Meeting MapReaderToMeeting(SQLiteDataReader reader)
    {
        return new Meeting
        {
            meeting_id = Convert.ToInt32(reader["meeting_id"]),
            student_id = Convert.ToInt32(reader["student_id"]),
            supervisor_id = Convert.ToInt32(reader["supervisor_id"]),

            // SQLite stores ISO date and time separately — parse correctly
            meeting_date = DateTime.Parse(reader["meeting_date"].ToString()),

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
                  AND meeting_date >= date('now')   -- 🔥 filter out past meetings
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
            throw new Exception($"Error retrieving meetings by supervisor ID: {ex.Message}", ex);
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
            throw new Exception($"Error retrieving meetings by supervisor and date: {ex.Message}", ex);
        }

        return meetings;
    }

}
